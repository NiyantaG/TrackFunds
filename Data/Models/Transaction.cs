using TrackFunds.Data.Enums;

namespace TrackFunds.Data.Models
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public double Amount { get; set; }
        public TransactionType Type { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string? Note { get; set; }
        public string Tag { get; set; }
        public string? DebtSource { get; set; }
        public DateTime? DebtDueDate { get; set; }
        public DebtStatus? DebtStatus { get; set; }
        public Guid UserId { get; set; }
    }
}
