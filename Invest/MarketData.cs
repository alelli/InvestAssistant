namespace Invest
{
    public class MarketDataRoot1 // заменить на stringStaticData
    {
        public MarketData1 marketdata { get; set; }
    }
    public class MarketData1
    {
        public List<string> columns { get; set; }
        public List<List<string>> data { get; set; }
    }

    public class MarketData
    {
        public DoubleMarketData marketdata { get; set; }
    }
    public class DoubleMarketData
    {
        public List<string> columns { get; set; }
        public List<List<double?>> data { get; set; }
    }

    public class StringStaticData // можно заменить на Root+MarketData+Securities
    {
        public Securities securities { get; set; }
    }
    public class Securities
    {
        public List<string> columns { get; set; }
        public List<List<string>> data { get; set; }
    }
}
