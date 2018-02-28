namespace SnapShotStore
{
    public class GenerateAccounts
    {
        public GenerateAccounts(string filename, int numAccountsToGenerate)
        {
            Filename = filename;
            NumAccountsToGenerate = numAccountsToGenerate;
        }

        public string Filename { get; }
        public int NumAccountsToGenerate { get; }
    }
}