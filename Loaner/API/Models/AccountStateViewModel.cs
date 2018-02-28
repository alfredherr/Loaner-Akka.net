using System;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Newtonsoft.Json;

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
            AccountState = accountState;
            Message = $"State as of: {DateTime.Now}";
        }


        [JsonProperty(Order = 1)]
        public string Message { get; set; }

        [JsonProperty(Order = 2)]
        public AccountState AccountState { get; set; }
    }
}