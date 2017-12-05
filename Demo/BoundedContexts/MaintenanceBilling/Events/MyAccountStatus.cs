using System.Collections.Generic;
using Demo.BoundedContexts.MaintenanceBilling.Aggregates;
using Demo.BoundedContexts.MaintenanceBilling.Aggregates.StateModels;

namespace Demo.BoundedContexts.MaintenanceBilling.Events
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