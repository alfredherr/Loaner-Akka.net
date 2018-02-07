using Akka.Persistence;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates
{
    using Akka.Actor;
    using Akka.Dispatch.SysMsg;
    using Akka.Event;
    using Akka.Monitoring;
    using Akka.Routing;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Messages;

    /**
      * We are sumulating the boarding of accounts from scratch. 
     */

    public class AccountBoardingModel
    {
       
        public AccountBoardingModel(string portfolioName, AccountNumber accountNumber, double openingBalance, string inventory, string userName, DateTime lastPaymentDate, double lastPaymentAmount)
        {
            PortfolioName = portfolioName;
            AccountNumber = accountNumber;
            OpeningBalance = openingBalance;
            Inventory = inventory;
            UserName = userName;
            LastPaymentDate = lastPaymentDate;
            LastPaymentAmount = lastPaymentAmount;
        }
        public double LastPaymentAmount { get; }
        public DateTime LastPaymentDate { get; }
        public string PortfolioName { get;  }
        public AccountNumber AccountNumber { get; }
        public double OpeningBalance { get;  }
        public string Inventory { get;  }
        public string UserName { get;  }
              
    }
    public class BoardAccountActor : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private static int _accounSpunUp;

        private readonly Dictionary<PortfolioName, Dictionary<AccountNumber, AccountBoardingModel>> _accountsInPortfolio =
            new Dictionary<PortfolioName, Dictionary<AccountNumber, AccountBoardingModel>>();

        private readonly Dictionary<AccountNumber, List<MaintenanceFee>> _obligationsInFile =
            new Dictionary<AccountNumber, List<MaintenanceFee>>();

        public BoardAccountActor()
        {
            Receive<SimulateBoardingOfAccounts>(client => StartUpHandler(client, client.ClientAccountsFilePath,
                client.ObligationsFilePath));
            Receive<SpinUpAccountActor>(msg => SpinUpAccountActor(msg));

            /* Example of custom error handling, also using messages */
            Receive<FailedToLoadAccounts>(m =>
            {
                _log.Error($"Failed to load account {m}");
                Self.Tell(typeof(Stop));
            });
            Receive<FailedToLoadObligations>(m =>
            {
                _log.Error($"Failed to load obligations {m}");
                Self.Tell(typeof(Stop));
            });
            Receive<SaveSnapshotSuccess>(m => { });
            Receive<DeleteMessagesSuccess>(m => { });
            Receive<DeleteSnapshotsSuccess>(msg => { });
            ReceiveAny(msg => _log.Error($"Unhandled message in {Self.Path.Name}. Message:{msg.ToString()}"));
        }

        private void StartUpHandler(SimulateBoardingOfAccounts client
            , string accountsFilePath
            , string obligationsFilePath)
        {
            Monitor();
            var supervisor = Context.Parent;
            var counter = 0;

            _log.Info($"Processing boarding command... ");

            GetAccountsForClient(accountsFilePath, obligationsFilePath);

            Console.WriteLine($"There are {Environment.ProcessorCount} logical processors. Running {Environment.ProcessorCount * 4} boarding actor routees");

            var props = new RoundRobinPool(Environment.ProcessorCount * 3).Props(Props.Create<BoardAccountActor>());
            var router = Context.ActorOf(props, $"Client{client.ClientName}Router");

            foreach (KeyValuePair<PortfolioName, Dictionary<AccountNumber, AccountBoardingModel>> portfolioDic in _accountsInPortfolio)
            {
                string portfolio = portfolioDic.Key.Instance;
                Dictionary<AccountNumber, AccountBoardingModel> accounts = portfolioDic.Value;
                var porfolioActor = supervisor
                    .Ask<IActorRef>(new SuperviseThisPortfolio(portfolio), TimeSpan.FromSeconds(3)).Result;
                _log.Info($"The portfolio name is: {porfolioActor.Path.Name}");

                foreach (KeyValuePair<AccountNumber, AccountBoardingModel> account in accounts)
                {
                    if (_obligationsInFile.ContainsKey(account.Key))
                    {
                        //Pluck out all the obligations for this account, LINQ anyone?
                        var obligations = _obligationsInFile[account.Key];
                        if (++counter % 1000 == 0)
                        {
                            _log.Info(
                                $"({counter}) Telling router {router.Path.Name} to spin up account {account.Key.Instance} with initial balance of {account.Value}... ");
                        }
                        router.Tell(
                            new SpinUpAccountActor(portfolio, 
                                account.Key.Instance, 
                                obligations,
                                porfolioActor,
                                account.Value));
                    }
                    else
                    {
                        _log.Error($"WTF {account.Key.Instance} doesn't exist in my obligations list");
                        throw new Exception($"WTF {account.Key.Instance} doesn't exist in my obligations list");
                    }
                }
            }
        }

        private void SpinUpAccountActor(SpinUpAccountActor command)
        {
            Monitor();
            var props = Props.Create<AccountActor>();
            var accountActor = Context.ActorOf(props, command.AccountNumber);
            accountActor.Tell(new CreateAccount(command.AccountNumber, command.BoardingModel));

            command.Obligations.ForEach(x => accountActor.Tell(new AddObligationToAccount(command.AccountNumber, x)));

            accountActor.Tell(new AskToBeSupervised(command.Portfolio, command.Supervisor));
            
            if (_accounSpunUp % 1000 == 0)
            {
                Console.WriteLine($"Boarding: {DateTime.Now}\t{_accounSpunUp} accounts processed.");
            }

            _accounSpunUp++;
        }

        /* Auxiliary methods */
        public void GetObligationsForClient(string obligationsFilePath)
        {
            try
            {
                _log.Info($"Gonna try to open file {obligationsFilePath}");
                if (File.Exists(obligationsFilePath))
                {
                    var readText = File.ReadAllLines(obligationsFilePath, Encoding.UTF8);
                    _log.Info($"There are {readText.Length} obligations in {obligationsFilePath}");
                    foreach (var row in readText)
                    {
                        if (row.Length > 11)
                        {
                            var line = row.Split('\t');
                            string obligationNumber = line[0];
                            var accountNumber = new AccountNumber(line[1]);
                            string typeOfObligation = line[2];
                            double openningBalance;
                            Double.TryParse(line[3], out openningBalance);

                            MaintenanceFee o = new MaintenanceFee(obligationNumber, openningBalance);

                            if (_obligationsInFile.ContainsKey(accountNumber))
                            {
                                List<MaintenanceFee> obligations = _obligationsInFile[accountNumber];
                                obligations.Add(o);
                                _obligationsInFile[accountNumber] = obligations;
                            }
                            else
                            {
                                List<MaintenanceFee> obligations = new List<MaintenanceFee>();
                                obligations.Add(o);
                                _obligationsInFile[accountNumber] = obligations;
                            }
                        }
                    }

                    _log.Info($"Successfully processing file {obligationsFilePath}");
                } 
                else
                {
                    throw new FileNotFoundException(obligationsFilePath);
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
                Sender.Tell(new FailedToLoadObligations(e.Message));
            }
        }

        private void GetAccountsForClient(string clientsFilePath, string obligationsFilePath)
        {
            try
            {
                _log.Info($"Gonna try to open file {clientsFilePath}");
                if (File.Exists(clientsFilePath))
                {
                    var readText = File.ReadAllLines(clientsFilePath, Encoding.UTF8);
                    _log.Info($"There are {readText.Length} accounts in {clientsFilePath}");

                    foreach (var row in readText)
                    {
                        if (row.Length > 11)
                        {
                            string[] line = row.ToUpper().Split('\t');

                            if (line.Length < 7)
                                throw new Exception($"Row: {row} in {clientsFilePath} does not have 7 tab-separated columns. It has {line.Length}.");

                            var portfolioName = new PortfolioName(line[0]);
                            var accountNumber = new AccountNumber(line[1]);
                            string userName = line[2];
                            double balance;
                            Double.TryParse(line[3], out balance);
                            string inventroy = line[4];

                            var daysDelinquent = Double.Parse(line[5]);

                            double lastPaymentAmount = 100.0;
                            DateTime lastPaymentDate = DateTime.Now.AddDays(-10);
                            if (daysDelinquent > 0.0)
                            {
                                lastPaymentAmount = 55.0;
                                lastPaymentDate = DateTime.Now.AddDays(daysDelinquent);
                            }

                            var delinquentAmount = Double.Parse(line[6]);


                            var accountBoarded = new AccountBoardingModel
                            (
                                portfolioName: line[0],
                                accountNumber: accountNumber,
                                openingBalance: balance,
                                inventory: inventroy,
                                userName: userName,
                                lastPaymentAmount: lastPaymentAmount,
                                lastPaymentDate: lastPaymentDate
                            );

                            if (_accountsInPortfolio.ContainsKey(portfolioName))
                            {
                                var existingAccounts = _accountsInPortfolio[portfolioName];
                                existingAccounts.Add(accountNumber, accountBoarded);
                                _accountsInPortfolio[portfolioName] = existingAccounts;
                            }
                            else
                            {
                                var accounts = new Dictionary<AccountNumber, AccountBoardingModel>();
                                accounts.Add(accountNumber, accountBoarded);
                                _accountsInPortfolio.Add(portfolioName, accounts);
                            }
                        }
                    }
                    _log.Info($"Successfully processing file {clientsFilePath}");

                    GetObligationsForClient(obligationsFilePath);
                }
                else
                {
                    throw new FileNotFoundException(clientsFilePath);
                }
            }
            catch (Exception e)
            {
                _log.Error($"{e}");
                Sender.Tell(new FailedToLoadAccounts($"{e.Message} {e.StackTrace}"));
            }            
        }

        private void Monitor()
        {
            Context.IncrementMessagesReceived();
        }

        protected override void PostStop()
        {
            Context.IncrementActorStopped();
        }

        protected override void PreStart()
        {
            Context.IncrementActorCreated();
        }
    }
}