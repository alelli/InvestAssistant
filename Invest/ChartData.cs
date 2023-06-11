namespace Invest
{
    public class ChartData
    {
        public string SecId { get; set; }
        public string SecName{ get; set; }
        public float LastPrice { get; set; }
        public float LastChange{ get; set; }
        public int Amount { get; set; }
        public List<DateTime> Dates { get; set; }
        public List<double> Prices { get; set; }
        public List<double> ForecastedPrices { get; set; }
    }
}
