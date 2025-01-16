using System.Data;
using System.Text.Json;
using TrackFunds.Data.Enums;
using TrackFunds.Data.Models;

namespace TrackFunds.Data.Services
{
    public class UsersService
    {

        public static void SaveAll(List<User> users)
        {
            string appDataDirectoryPath = Utils.GetAppDirectoryPath();
            string appUsersFilePath = Utils.GetAppUsersFilePath();

            if (!Directory.Exists(appDataDirectoryPath))
            {
                Directory.CreateDirectory(appDataDirectoryPath);
            }

            var json = JsonSerializer.Serialize(users);
            File.WriteAllText(appUsersFilePath, json);
        }

        public static List<User> GetAll()
        {
            string appUsersFilePath = Utils.GetAppUsersFilePath();
            if (!File.Exists(appUsersFilePath))
            {
                return new List<User>();
            }
            var json = File.ReadAllText(appUsersFilePath);

            return JsonSerializer.Deserialize<List<User>>(json);
        }

        public static User Login(string username, string password, MoneyPreference moneyPreference)
        {
            List<User> users = GetAll();
            var loginnullmessage = "Please fill up the details.";
            var user = users.FirstOrDefault(x => x.Username == username.Trim());

            if (user == null )
            {
             
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
            else {
                var loginErrorMessage = "Invalid username or password.";
                bool passwordIsValid = password == user.Password;

                if (!passwordIsValid)
                {
                    throw new Exception(loginErrorMessage);
                }
            }
            return user;
        }


        public static string GetCurrency(Guid userId)
        {
            List<User> users = GetAll();
            var user = users.FirstOrDefault(x => x.Id == userId);
            return user.MoneyPreference switch
            {
                MoneyPreference.Dollar => "$",
                MoneyPreference.Rupees => "Rs",
                MoneyPreference.Pound => "£",
                _ => "Rs",
            };
        }
    }
}
