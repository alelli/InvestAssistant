namespace Invest.Models
{
    public class Stock
    {
        public DateTime Tradedate { get; set; }
        public float Value { get; set; }
        public Stock()
        {
            Tradedate = DateTime.Now;
            Value = 0;
        }
        public Stock(DateTime date, float value)
        {
            Tradedate = date;
            Value = value;
        }
    }
    public class StockForecast
    {
        public float[] ForecastedPrices { get; set; }
    }
}
