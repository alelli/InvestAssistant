namespace Invest
{
    public class ChartData
    {
        public string Secid { get; set; }
        public List<DateTime> Dates { get; set; }
        public List<double> Prices { get; set; }
        public List<double> ForecastedPrices { get; set; }
        public float[] Errors { get; set; }
    }
}
