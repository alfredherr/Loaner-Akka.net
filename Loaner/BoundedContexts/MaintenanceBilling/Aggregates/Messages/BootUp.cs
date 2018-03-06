namespace Loaner
{
    public class BootUp
    {
        public BootUp(string startingUp)
        {
            Message = startingUp;
        }

        public string Message { get; set; }
    }
}