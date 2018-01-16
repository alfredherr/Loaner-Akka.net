using System;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;

namespace Loaner.API.Models
{
    public class AccountStateViewModel
    {
        public AccountStateViewModel()
        {
            AccountState = new AccountState();
        }

        public AccountStateViewModel(string message) : this()
        {
            Message = message;
        }

        public AccountStateViewModel(AccountState accountState)
        {
            this.AccountState = accountState;
            Message = $"State as of: {DateTime.Now}";
        }

        public AccountState AccountState { get; set; }

        public string Message { get; set; }
    }
}