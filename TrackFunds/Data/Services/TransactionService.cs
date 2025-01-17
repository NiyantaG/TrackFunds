using System.Text.Json;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.JSInterop;
using TrackFunds.Data.Enums;
using TrackFunds.Data.Models;

namespace TrackFunds.Data.Services
{
    public class TransactionService
    {
        // Saves all transactions to a JSON file for a specific user
        private static void SaveAll(List<Transaction> transactions, Guid userId)
        {
            string appDataDirectoryPath = Utils.GetAppDirectoryPath();
            string transactionsFilePath = Utils.GetUserTransactionsFilePath(userId);

            // Create directory if it doesn't exist
            if (!Directory.Exists(appDataDirectoryPath))
            {
                Directory.CreateDirectory(appDataDirectoryPath);
            }

            // Serialize and save transactions to file
            var json = JsonSerializer.Serialize(transactions);
            File.WriteAllText(transactionsFilePath, json);
        }

        // Retrieves all transactions for a specific user
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

        // Creates a new transaction and updates user balance
        public static Tuple<List<Transaction>, User> Create(Guid userId, double amount, TransactionType type,
            string note, string tag, string? debtSource, DateTime? debtDueDate)
        {
            List<Transaction> transactions = GetAll(userId);
            List<User> users = UsersService.GetAll();
            var user = users.FirstOrDefault(x => x.Id == userId);

            // Validate user exists
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            // Check sufficient balance for debit transactions
            if (type == TransactionType.Debit)
            {
                if (user.BalanceAmount < amount)
                {
                    throw new Exception("Insufficient balance.");
                }
            }

            // Create and add new transaction
            transactions.Add(new Transaction
            {
                Amount = amount,
                Type = type,
                Note = note,
                Tag = tag,
                UserId = userId,
                DebtSource = debtSource,
                DebtDueDate = debtDueDate,
                DebtStatus = type == TransactionType.Debt ? DebtStatus.Pending : null
            });

            SaveAll(transactions, userId);

            // Update user balance based on transaction type
            if (type == TransactionType.Credit)
            {
                user.BalanceAmount += amount;
                // Check and clear any pending debts that can be paid
                var clearableDebts = GetTransactionType(userId, TransactionType.Debt)
                    .Where(x => x.DebtStatus == DebtStatus.Pending && x.Amount < user.BalanceAmount);

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
            else if (type == TransactionType.Debit)
            {
                user.BalanceAmount -= amount;
            }

            UsersService.SaveAll(users);
            return (transactions, user).ToTuple();
        }

        // Gets transactions of a specific type
        public static List<Transaction> GetTransactionType(Guid userId, TransactionType type)
        {
            List<Transaction> transactions = GetAll(userId);
            return transactions.Where(x => x.Type == type).ToList();
        }

        // Clears a pending debt by creating a debit transaction
        public static Tuple<List<Transaction>, User> ClearDebt(Guid userId, Guid debtId)
        {
            try
            {
                List<Transaction> transactions = GetAll(userId);
                List<User> users = UsersService.GetAll();
                User user = users.FirstOrDefault(x => x.Id == userId);
                Transaction debt = transactions.FirstOrDefault(x => x.Id == debtId);

                // Validate debt exists and can be cleared
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

                // Create debit transaction for debt payment
                var result = Create(userId, debt.Amount, TransactionType.Debit,
                    "Debt payment", "Debt", debt.DebtSource, debt.DebtDueDate);

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

        // Calculates total pending debt amount
        public static double GetTotalPendingDebtAmount(Guid userId)
        {
            List<Transaction> transactions = GetAll(userId);
            return transactions.Where(x => x.Type == TransactionType.Debt &&
                   x.DebtStatus == DebtStatus.Pending).Sum(x => x.Amount);
        }

        // Gets total income (credit transactions)
        public static double GetTotalIncome(Guid userId)
        {
            List<Transaction> transactions = GetAll(userId);
            return transactions.Where(x => x.Type == TransactionType.Credit).Sum(x => x.Amount);
        }

        // Gets total expenses (debit transactions)
        public static double GetTotalExpense(Guid userId)
        {
            List<Transaction> transactions = GetAll(userId);
            return transactions.Where(x => x.Type == TransactionType.Debit).Sum(x => x.Amount);
        }

        // Gets total number of transactions
        public static int GetTotalTransactionsCount(Guid userId)
        {
            List<Transaction> transactions = GetAll(userId);
            return transactions.Count;
        }

        // Gets highest transaction amount of a specific type
        public static double GetHighestTransactionAmount(Guid userId, TransactionType type)
        {
            List<Transaction> transactions = GetAll(userId);
            var filteredTransactions = transactions.Where(x => x.Type == type).ToList();
            return filteredTransactions.Any() ? filteredTransactions.Max(x => x.Amount) : 0.0;
        }

        // Gets lowest transaction amount of a specific type
        public static double GetLowestTransactionAmount(Guid userId, TransactionType type)
        {
            List<Transaction> transactions = GetAll(userId);
            var filteredTransactions = transactions.Where(x => x.Type == type).ToList();
            return filteredTransactions.Any() ? filteredTransactions.Min(x => x.Amount) : 0.0;
        }

        // Gets transactions within a date range
        public static List<Transaction> GetFilteredTransactions(Guid userId, DateTime startDate, DateTime endDate)
        {
            List<Transaction> transactions = GetAll(userId);

            if (startDate == endDate)
            {
                return transactions.Where(x => x.Date == startDate).ToList();
            }
            return transactions.Where(x => x.Date >= startDate && x.Date <= endDate).ToList();
        }

        // Searches transactions by tag
        public static List<Transaction> Search(Guid userId, string searchQuery)
        {
            List<Transaction> transactions = GetAll(userId);
            if (string.IsNullOrEmpty(searchQuery))
            {
                return transactions;
            }

            try
            {
                return transactions
                    .Where(x => x.Tag != null &&
                           x.Tag.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("No transactions found.", ex);
            }
        }

        // Gets list of pending debts
        public static List<Transaction> GetPendingDebtList(Guid userId, TransactionType type)
        {
            List<Transaction> transactions = GetAll(userId);
            var pendingDebts = GetTransactionType(userId, TransactionType.Debt)
                .Where(x => x.DebtStatus == DebtStatus.Pending)
                .ToList();

            return pendingDebts.Count != 0 ? pendingDebts : [];
        }
    }
}