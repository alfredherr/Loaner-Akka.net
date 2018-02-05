namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public enum AccountStatus
    {
        Active=0,
        Inactive,
        Cancelled,
        Created,
        Boarded,
        Upgraded,
        Removed,
        Closed
    }
}