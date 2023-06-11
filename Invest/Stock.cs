namespace Invest
{
    public class Stock
    {
        public DateTime Tradedate { get; set; }
        public float Value { get; set; }

        public Stock() // для CreateEnumerable<Stock>
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
    public class StockPrediction
    {
        public float[] ForecastedPrices { get; set; }
    }


    public class StringHistoryData
    {
        public StringHistory history { get; set; }
    }
    public class StringHistory
    {
        public List<string> columns { get; set; }
        public List<List<string>> data { get; set; }
    }

    public class DoubleHistoryData
    {
        public DoubleHistory history { get; set; }
    }
    public class DoubleHistory
    {
        public List<string> columns { get; set; }
        public List<List<double?>> data { get; set; }
    }

}
