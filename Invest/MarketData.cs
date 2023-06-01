namespace Invest
{
    public class MarketDataRoot1
    {
        public MarketData1 marketdata { get; set; }
    }
    public class MarketData1
    {
        public List<string> columns { get; set; }
        public List<List<string>> data { get; set; }
    }

    public class MarketDataRoot2
    {
        public MarketData2 marketdata { get; set; }
    }
    public class MarketData2
    {
        public List<string> columns { get; set; }
        public List<List<double?>> data { get; set; }
    }

    public class MarketData
    {
        public string SECID { get; set; }
        public double? LAST { get; set; }
        public double LASTCHANGEPRCNT { get; set; }
    }
}
