namespace Invest
{
    public class MarketDataRoot
    {
        public Marketdata marketdata { get; set; }
    }
    public class Marketdata
    {
        public List<string> columns { get; set; }
        public List<List<double>> data { get; set; }
    }

    public class Info
    {
        public Charsetinfo charsetinfo { get; set; }
        public List<MarketData> marketdata { get; set; }
    }

    public class MarketData
    {
        public string SECID { get; set; }
        public double? LAST { get; set; }
        public double LASTCHANGEPRCNT { get; set; }
    }
}
