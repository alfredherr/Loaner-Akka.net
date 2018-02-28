using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Exceptions
{
    public class AccountNotUnderSupervision : Exception
    {
        public AccountNotUnderSupervision(string cmdAccountNumber) : base(cmdAccountNumber)
        {
        }
    }
}