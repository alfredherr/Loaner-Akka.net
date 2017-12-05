namespace Demo.BoundedContexts.MaintenanceBilling.Aggregates.Messages
{
    public class AboutMe
    {
        public AboutMe(string me)
        {
            Me = me;
        }

        public string Me { get; set; }
    }
}