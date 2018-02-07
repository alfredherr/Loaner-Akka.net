using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Exceptions
{
    public class AccountNotUnderSupervision : Exception
    {
        public AccountNotUnderSupervision(string cmdAccountNumber)
        {
            throw new NotImplementedException();
        }
    }
}