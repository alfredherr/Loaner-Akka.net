using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages
{
    public class MyAccountStatus
    {
        public MyAccountStatus()
        {
            AccountState = new AccountState();
            Message = "";
        }

        public MyAccountStatus(string message)
        {
            Message = message;
            AccountState = new AccountState();
        }

        public MyAccountStatus(string message, AccountState accountState)
        {
            Message = message;
            AccountState = accountState;
        }

        public string Message { get; }
        public AccountState AccountState { get; }
    }
}