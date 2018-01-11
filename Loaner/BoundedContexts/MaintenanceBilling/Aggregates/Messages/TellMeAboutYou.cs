namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages
{
    public class TellMeAboutYou
    {
        public TellMeAboutYou(string me)
        {
            Me = me;
        }

        public string Me { get; set; }
    }
}