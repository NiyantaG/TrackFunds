namespace TrackFunds.Data.Services
{
    public class Utils
    {
        // Get App Directory
        public static string GetAppDirectoryPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "TrackFunds"
            );
        }
        // Get App Users File Path
        public static string GetAppUsersFilePath()
        {
            return Path.Combine(GetAppDirectoryPath(), "users.json");
        }
        // Get App Transactions File Path
        public static string GetUserTransactionsFilePath(Guid userId)
        {
            return Path.Combine(GetAppDirectoryPath(), userId.ToString() + "transactions.json");
        }

    }
}
