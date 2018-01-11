using System.Collections.Generic;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.Events
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