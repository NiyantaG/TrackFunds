using System.Text.Json;
using TrackFunds.Data.Enums;
using TrackFunds.Data.Models;

namespace TrackFunds.Data.Services
{
    public class TransactionService
    {
        private static void SaveAll(List<Transaction> transactions, Guid userId)
        {
            string appDataDirectoryPath = Utils.GetAppDirectoryPath();
            string transactionsFilePath = Utils.GetUserTransactionsFilePath(userId);

            if (!Directory.Exists(appDataDirectoryPath))
            {
                Directory.CreateDirectory(appDataDirectoryPath);
            }

            var json = JsonSerializer.Serialize(transactions);
            File.WriteAllText(transactionsFilePath, json);
        }

        public static List<Transaction> GetAll(Guid userId)
        {
            string transactionsFilePath = Utils.GetUserTransactionsFilePath(userId);
            if (!File.Exists(transactionsFilePath))
            {
                return new List<Transaction>();
            }

            var json = File.ReadAllText(transactionsFilePath);

            return JsonSerializer.Deserialize<List<Transaction>>(json);
        }

        public static Tuple<List<Transaction>, User> Create(Guid userId, double amount, TransactionType type, string note, string tag, string? debtSource, DateTime? debtDueDate)
        {
            List<Transaction> transactions = GetAll(userId);
            List<User> users = UsersService.GetAll();
            var user = users.FirstOrDefault(x => x.Id == userId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }
            
            if (type == TransactionType.Debit)
            {
                if (user.BalanceAmount < amount)
                {
                    throw new Exception("Insufficient balance.");
                }
            }

            transactions.Add(new Transaction
            {
                Amount = amount,
                Type = type,
                Note = note,
                Tag = tag,
                UserId = userId,
                DebtSource = debtSource,
                DebtDueDate = debtDueDate ,
                DebtStatus = type == TransactionType.Debt ? DebtStatus.Pending : null
            });

            SaveAll(transactions, userId);

            if (type == TransactionType.Credit)
            {
                user.BalanceAmount += amount;
            }
            else if (type == TransactionType.Debit)
            {
                user.BalanceAmount -= amount;
            }
            UsersService.SaveAll(users);
            if (type == TransactionType.Credit)
            {
                var clearableDebts = GetTransactionType(userId, TransactionType.Debt).Where(x => x.DebtStatus == DebtStatus.Pending).Where(x => x.Amount < user.BalanceAmount);
                if (clearableDebts.Count() > 0)
                {
                    foreach (var debt in clearableDebts)
                    {
                        var result = ClearDebt(userId, debt.Id);
                        user = result.Item2;
                    }
                }
                transactions = GetAll(userId);
            }
            return (transactions, user).ToTuple();
        }

        public static List<Transaction> GetTransactionType (Guid userId, TransactionType type)
        {
            List<Transaction> transactions = GetAll(userId);
            return transactions.Where(x => x.Type == type).ToList();
        }

        public static Tuple<List<Transaction>, User> ClearDebt (Guid userId, Guid debtId)
        {
            try
            {
                List<Transaction> transactions = GetAll(userId);
                List<User> users = UsersService.GetAll();
                User user = users.FirstOrDefault(x => x.Id == userId);
                Transaction debt = transactions.FirstOrDefault(x => x.Id == debtId);
                if (debt == null)
                {
                    throw new Exception("Debt not found.");
                }
                if (debt.DebtStatus != DebtStatus.Pending)
                {
                    throw new Exception("Debt already cleared.");
                }
                if (user.BalanceAmount < debt.Amount)
                {
                    throw new Exception("Insufficient balance.");
                }
                var result = Create(userId, debt.Amount, TransactionType.Debit, "Debt payment", "Debt", debt.DebtSource, debt.DebtDueDate);
                transactions = result.Item1;
                Transaction currDebt = transactions.FirstOrDefault(x => x.Id == debtId);
                currDebt.DebtStatus = DebtStatus.Paid;
                user.BalanceAmount = result.Item2.BalanceAmount;
                UsersService.SaveAll(users);
                SaveAll(transactions, userId);
                return (transactions, user).ToTuple();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static double GetTotalPendingDebtAmount(Guid userId)
        {
            List<Transaction> transactions = GetAll(userId);
            return transactions.Where(x => x.Type == TransactionType.Debt && x.DebtStatus == DebtStatus.Pending).Sum(x => x.Amount);
        }

        public static double GetTotalIncome(Guid userId)
        {
            List<Transaction> transactions = GetAll(userId);
            return transactions.Where(x => x.Type == TransactionType.Credit).Sum(x => x.Amount);
        }

        public static double GetTotalExpense(Guid userId)
        {
            List<Transaction> transactions = GetAll(userId);
            return transactions.Where(x => x.Type == TransactionType.Debit).Sum(x => x.Amount);
        }

        public static int GetTotalTransactionsCount(Guid userId)
        { 
            List<Transaction> transactions = GetAll(userId);
            return transactions.Count;
        }

        public static double GetHighestTransactionAmount(Guid userId, TransactionType type)
        {
            List<Transaction> transactions = GetAll(userId);
            return transactions.Where(x => x.Type == type).Max(x => x.Amount);
        }
        public static double GetLowestTransactionAmount(Guid userId, TransactionType type)
        {
            List<Transaction> transactions = GetAll(userId);
            return transactions.Where(x => x.Type == type).Min(x => x.Amount);
        }
        
    }
}
