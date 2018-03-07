using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Event;
using Akka.Monitoring;
using Akka.Persistence;
using Akka.Routing;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates
{
    /**
      * We are sumulating the boarding of accounts from scratch. 
     */

    public class BoardAccountActor : ReceiveActor
    {
        private readonly Dictionary<PortfolioName, Dictionary<AccountNumber, AccountBoardingModel>> _accountsInPortfolio
            =
            new Dictionary<PortfolioName, Dictionary<AccountNumber, AccountBoardingModel>>();

        private readonly ILoggingAdapter _log = Context.GetLogger();

        private readonly Dictionary<AccountNumber, List<MaintenanceFee>> _obligationsInFile =
            new Dictionary<AccountNumber, List<MaintenanceFee>>();

        public BoardAccountActor()
        {
            Receive<SimulateBoardingOfAccounts>(client => BoardingPrerequisites(client));
            Receive<BoardClient>(client => StartUpHandler(client));
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

        private void BoardingPrerequisites(SimulateBoardingOfAccounts cmd)
        {
            var props = new RoundRobinPool(Environment.ProcessorCount * 3).Props(Props.Create<BoardAccountActor>());
            var router = Context.ActorOf(props, $"Client{cmd.ClientName}Router");

            Self.Tell(new BoardClient(cmd, cmd.ClientAccountsFilePath, cmd.ObligationsFilePath, router));
        }

        private void StartUpHandler(BoardClient cmd)
        {
            var obligationsFilePath = cmd.ObligationsFilePath;
            var accountsFilePath = cmd.AccountsFilePath;
            var router = cmd.BoardingRouter;

            Monitor();
            var supervisor = Context.Parent;
            var counter = 0;

            _log.Info($"Processing boarding command... ");

            GetAccountsForClient(accountsFilePath, obligationsFilePath);

            Console.WriteLine(
                $"There are {Environment.ProcessorCount} logical processors. Running {Environment.ProcessorCount * 4} boarding actor routees");


            foreach (var portfolioDic in _accountsInPortfolio)
            {
                var portfolio = portfolioDic.Key.Instance;
                var accounts = portfolioDic.Value;
                var porfolioActor = supervisor
                    .Ask<IActorRef>(new SuperviseThisPortfolio(portfolio), TimeSpan.FromSeconds(3)).Result;
                _log.Info($"The portfolio name is: {porfolioActor.Path.Name}");

                foreach (var account in accounts)
                    if (_obligationsInFile.ContainsKey(account.Key))
                    {
                        //Pluck out all the obligations for this account, LINQ anyone?
                        var obligations = _obligationsInFile[account.Key];
                        if (++counter % 1000 == 0)
                            _log.Info(
                                $"({counter}) Telling router {router.Path.Name} to spin up account {account.Key.Instance} with initial balance of {account.Value}... ");
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

        private void SpinUpAccountActor(SpinUpAccountActor command)
        {
            Monitor();
            try
            {
                var props = Props.Create<AccountActor>();
                var accountActor = Context.ActorOf(props, command.AccountNumber);
                accountActor.Tell(new CreateAccount(command.AccountNumber, command.BoardingModel));

                command.Obligations.ForEach(
                    x => accountActor.Tell(new AddObligationToAccount(command.AccountNumber, x)));

                accountActor.Tell(new AskToBeSupervised(command.Portfolio, command.Supervisor));

                if (Int64.Parse(command.AccountNumber) % 1000 == 0)
                    Console.WriteLine($"Boarding: {DateTime.Now}\taccount {command.AccountNumber} processed.");
            }
            catch (Exception e)
            {
                _log.Error($"[SpinUpAccountActor]: {e.Message} {e.StackTrace}");
                throw;
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
                        if (row.Length > 11)
                        {
                            var line = row.Split('\t');
                            var obligationNumber = line[0];
                            var accountNumber = new AccountNumber(line[1]);
                            var typeOfObligation = line[2];
                            double openningBalance;
                            double.TryParse(line[3], out openningBalance);

                            var o = new MaintenanceFee(obligationNumber, openningBalance, ObligationStatus.Active);

                            if (_obligationsInFile.ContainsKey(accountNumber))
                            {
                                var obligations = _obligationsInFile[accountNumber];
                                obligations.Add(o);
                                _obligationsInFile[accountNumber] = obligations;
                            }
                            else
                            {
                                var obligations = new List<MaintenanceFee>();
                                obligations.Add(o);
                                _obligationsInFile[accountNumber] = obligations;
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
                _log.Error($"[GetObligationsForClient]: {e.Message} {e.StackTrace}");
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
                        if (row.Length > 11)
                        {
                            var line = row.ToUpper().Split('\t');

                            if (line.Length < 7)
                                throw new Exception(
                                    $"Row: {row} in {clientsFilePath} does not have 7 tab-separated columns. It has {line.Length}.");

                            var portfolioName = new PortfolioName(line[0]);
                            var accountNumber = new AccountNumber(line[1]);
                            var userName = line[2];
                            double balance;
                            double.TryParse(line[3], out balance);
                            var inventroy = line[4];

                            var daysDelinquent = double.Parse(line[5]);

                            var lastPaymentAmount = 100.0;
                            var lastPaymentDate = DateTime.Now.AddDays(-10);
                            if (daysDelinquent > 0.0)
                            {
                                lastPaymentAmount = 55.0;
                                lastPaymentDate = DateTime.Now.AddDays(-1 * daysDelinquent);
                            }

                            var delinquentAmount = double.Parse(line[6]);


                            var accountBoarded = new AccountBoardingModel
                            (
                                line[0],
                                accountNumber,
                                balance,
                                inventroy,
                                userName,
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
                _log.Error($"[GetAccountsForClient]: {e.Message} {e.StackTrace}");
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