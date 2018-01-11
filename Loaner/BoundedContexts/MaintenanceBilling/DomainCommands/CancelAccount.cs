using System;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainCommands
{
    public class CancelAccount : IDomainCommand
    {
        public CancelAccount()
        {
            _RequestedOn = DateTime.Now;
            _UniqueGuid = Guid.NewGuid();
        }

        public CancelAccount(Account account) : this()
        {
            Account = account;
        }

        public Account Account { get; }
        private Guid _UniqueGuid { get; }
        private DateTime _RequestedOn { get; }

        public DateTime RequestedOn()
        {
            return _RequestedOn;
        }

        public Guid UniqueGuid()
        {
            return _UniqueGuid;
        }
    }
}