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
        public float[] LowerBoundPrices { get; set; }
        public float[] UpperBoundPrices { get; set; }
    }

    public class Charsetinfo
    {
        public string name { get; set; }
    }

    public class History
    {
        public string TRADEDATE { get; set; }
        public double OPEN { get; set; }
    }

    public class Root3
    {
        public Charsetinfo charsetinfo { get; set; }
        public List<History> history { get; set; }
    }
    public class Root
    {
        public Securities securities { get; set; }
    }
    public class Root1
    {
        public History1 history { get; set; }
    }
    public class Root2
    {
        public History2 history { get; set; }
    }

    [Serializable]
    public class Securities
    {
        public List<string> columns { get; set; }
        public List<List<string>> data { get; set; }
    }

    public class History1 //DATE
    {
        public List<string> columns { get; set; }
        public List<List<string>> data { get; set; }
    }

    public class History2 //PRICE
    {
        public List<string> columns { get; set; }
        public List<List<double>> data { get; set; }
    }

}
