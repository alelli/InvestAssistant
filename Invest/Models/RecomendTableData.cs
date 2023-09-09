namespace Invest.Models
{
    public class RecomendTableData
    {
        public string SecId { get; set; }
        public float LastPrice { get; set; }
        public float ForecastedPrice { get; set; }
        public float Income { get; set; }
        public int Amount { get; set; }
        public float Buy { get; set; }
        public float Sale { get; set; }
        public float TotalIncome { get; set; }
    }
}
