namespace Invest.Models
{
    public class SecurityHistory
    {
        public List<DateTime> Dates { get; set; } = new List<DateTime>();
        public List<double> Prices { get; set; } = new List<double>();
    }
}
