
using TrackFunds.Data.Enums;

namespace TrackFunds.Data.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; }
        public string Password { get; set; }
        public MoneyPreference MoneyPreference { get; set; } = MoneyPreference.Rupees;
        public double BalanceAmount { get; set; } = 0;
    }
}
