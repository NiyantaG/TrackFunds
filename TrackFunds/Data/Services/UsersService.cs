using System.Data;
using System.Text.Json;
using TrackFunds.Data.Enums;
using TrackFunds.Data.Models;

namespace TrackFunds.Data.Services
{
    public class UsersService
    {
        // Saves all users to a JSON file
        public static void SaveAll(List<User> users)
        {
            // Get file paths for app data
            string appDataDirectoryPath = Utils.GetAppDirectoryPath();
            string appUsersFilePath = Utils.GetAppUsersFilePath();

            // Create directory if it doesn't exist
            if (!Directory.Exists(appDataDirectoryPath))
            {
                Directory.CreateDirectory(appDataDirectoryPath);
            }

            // Serialize users list to JSON and save to file
            var json = JsonSerializer.Serialize(users);
            File.WriteAllText(appUsersFilePath, json);
        }

        // Retrieves all users from the JSON file
        public static List<User> GetAll()
        {
            string appUsersFilePath = Utils.GetAppUsersFilePath();

            // Return empty list if file doesn't exist
            if (!File.Exists(appUsersFilePath))
            {
                return new List<User>();
            }

            // Read and deserialize JSON file
            var json = File.ReadAllText(appUsersFilePath);
            return JsonSerializer.Deserialize<List<User>>(json);
        }

        // Handles user login and registration
        public static User Login(string username, string password, MoneyPreference moneyPreference)
        {
            List<User> users = GetAll();
            var loginnullmessage = "Please fill up the details.";

            // Try to find existing user
            var user = users.FirstOrDefault(x => x.Username == username.Trim());

            if (user == null)
            {
                // If user doesn't exist, create new user (registration)
                users.Add(
                user = new User
                {
                    Username = username.Trim(),
                    Password = password,
                    MoneyPreference = moneyPreference,
                }
                );
                SaveAll(users);
            }
            else
            {
                // If user exists, validate password (login)
                var loginErrorMessage = "Invalid username or password.";
                bool passwordIsValid = password == user.Password;

                if (!passwordIsValid)
                {
                    throw new Exception(loginErrorMessage);
                }
            }
            return user;
        }

        // Gets currency symbol based on user's money preference
        public static string GetCurrency(Guid userId)
        {
            List<User> users = GetAll();
            var user = users.FirstOrDefault(x => x.Id == userId);

            // Return currency symbol based on preference
            return user.MoneyPreference switch
            {
                MoneyPreference.Dollar => "$",
                MoneyPreference.Rupees => "Rs",
                MoneyPreference.Pound => "£",
                _ => "Rs",  // Default to Rupees
            };
        }
    }
}