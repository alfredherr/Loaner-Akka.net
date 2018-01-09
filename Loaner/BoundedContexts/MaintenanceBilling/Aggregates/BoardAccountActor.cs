namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates
{
    using Akka.Actor;
    using Akka.Dispatch.SysMsg;
    using Akka.Event;
    using Akka.Monitoring;
    using Akka.Routing;
    using Commands;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Messages;

    /**
      * We are sumulating the boarding of accounts from scratch. 
     */

    public class BoardAccountActor : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private static int _accounSpunUp;
        private readonly Dictionary<PortfolioName, Dictionary<AccountNumber, Balance>> _accountsInPortfolio = new Dictionary<PortfolioName, Dictionary<AccountNumber, Balance>>();
        private readonly Dictionary<AccountNumber, List<MaintenanceFee>> _obligationsInFile = new Dictionary<AccountNumber, List<MaintenanceFee>>();

        public BoardAccountActor()
        {
            Receive<SimulateBoardingOfAccounts>(client => StartUpHandler(client, client.ClientAccountsFilePath,
                client.ObligationsFilePath));
            Receive<SpinUpAccountActor>(msg => SpinUpAccountActor(msg));

            /* Example of custom error handling, also using messages */
            Receive<FailedToLoadAccounts>(m => Self.Tell(typeof(Stop)));
            Receive<FailedToLoadObligations>(m => Self.Tell(typeof(Stop)));
            //Receive<SaveSnapshotSuccess>(m => _log.Info($"snapshot taken {m.Metadata.PersistenceId}") );
            ReceiveAny(msg => _log.Error($"Unhandled message in {Self.Path.Name}. Message:{msg.ToString()}"));
        }

        private void StartUpHandler(SimulateBoardingOfAccounts client
                                    ,string accountsFilePath
                                    ,string obligationsFilePath)
        {
            Monitor();
            var supervisor = Context.Parent;
            var counter = 0;

            _log.Info($"Procesing boarding command... ");

            GetAccountsForClient(accountsFilePath,obligationsFilePath);
            
            var props = new RoundRobinPool(72).Props(Props.Create<BoardAccountActor>());
            var router = Context.ActorOf(props, $"Client{client.ClientName}Router");

            foreach (var portfolioDic in _accountsInPortfolio)
            {
                string portfolio = portfolioDic.Key.Instance;
                var accounts  = portfolioDic.Value;
                var porfolioActor  = supervisor.Ask<IActorRef>(new SuperviseThisPortfolio(portfolio),TimeSpan.FromSeconds(3)).Result;
                _log.Info($"The portfolio name is: {porfolioActor.Path.Name}");

                foreach (var account in accounts)
                {
                    if (_obligationsInFile.ContainsKey(account.Key) )
                    {
                        //Pluck out all the obligations for this account, LINQ anyone?
                        var obligations = _obligationsInFile[account.Key];
                        if (++counter % 1000 == 0)
                        {
                            _log.Info(
                                $"({counter}) Telling router {router.Path.Name} to spin up account {account.Key.Instance} with initial balance of {account.Value}... ");
                        }
                        router.Tell(new SpinUpAccountActor(portfolio, account.Key.Instance, obligations, porfolioActor));
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
            accountActor.Tell(new CreateAccount(command.AccountNumber));

            command.Obligations.ForEach(x => accountActor.Tell(new AddObligationToAccount(command.AccountNumber, x)));
             
            accountActor.Tell(new AskToBeSupervised(command.Portfolio,command.Supervisor));
            _accounSpunUp++;
            if (_accounSpunUp  % 1000 == 0)
            {
                Console.WriteLine($"Boarding: {DateTime.Now}\t{_accounSpunUp} accounts processed.");

            }
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
                            Double.TryParse( line[3],out openningBalance);
                            
                            MaintenanceFee o = new MaintenanceFee(obligationNumber,openningBalance);
                            
                            if (_obligationsInFile.ContainsKey(accountNumber))
                            {
                                List<MaintenanceFee> obligations = _obligationsInFile[accountNumber] ;
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
                }
                _log.Info($"Successfully processing file {obligationsFilePath}");
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
                            var line = row.Split('\t');
                            var portfolioName = new PortfolioName( line[0] );
                            var accountNumber = new AccountNumber( line[1] );
                            //string accountInfo   = line[2];
                            double balance;
                            Double.TryParse(line[3],out balance);
                            var accountbalance = new Balance(balance);
                            if (_accountsInPortfolio.ContainsKey(portfolioName))
                            {
                                var existingAccounts = _accountsInPortfolio[portfolioName];
                                existingAccounts.Add(accountNumber, accountbalance);
                                _accountsInPortfolio[portfolioName] = existingAccounts;
                            }
                            else
                            {
                                var accounts = new Dictionary<AccountNumber,Balance>();
                                accounts.Add(accountNumber, accountbalance);
                                _accountsInPortfolio.Add(portfolioName, accounts);
                            }
                        }
                    }
                }
                _log.Info($"Successfully processing file {clientsFilePath}");
            }
            catch (Exception e)
            {
                Sender.Tell(new FailedToLoadAccounts($"{e.Message} {e.StackTrace}"));
            }

            GetObligationsForClient(obligationsFilePath);

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

    internal class PortfolioName
    {
        public PortfolioName(string name)
        {
            Instance = name;
        }
        public string Instance { get; }
        public override int GetHashCode()
        {
            return Instance.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            PortfolioName test = obj as PortfolioName;
            return test.Instance == this.Instance;
        }
    }

    internal class AccountNumber
    {
        public AccountNumber(string accountNumber)
        {
            Instance = accountNumber;
        }
        public string Instance { get; }
        public override int GetHashCode()
        {
            return Instance.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            AccountNumber test = obj as AccountNumber;
            return test.Instance == this.Instance;
        }
    }

    internal class Balance
    {
        public Balance(double amount)
        {
            Instance = amount;
        }
        public double Instance { get; }
        public override int GetHashCode()
        {
            return Instance.GetHashCode();
        }
    
        public override bool Equals(object obj)
        {
            Balance test = obj as Balance;
            return test.Instance == this.Instance;
        }
    }


    internal class SpinUpAccountActor
    {
        public SpinUpAccountActor(
            string portfolio ,
            string accountNumber,
            List<MaintenanceFee> oligations,
            IActorRef supervisor)
        {
            Portfolio = portfolio;
            AccountNumber = accountNumber;
            Obligations = oligations;
            Supervisor = supervisor;
        }
        public string Portfolio { get; }
        public string AccountNumber { get; }
        public List<MaintenanceFee> Obligations { get; }
        public IActorRef Supervisor { get; }
    }
}