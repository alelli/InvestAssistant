namespace Invest.JsonClasses
{
    public class StringData
    {
        public List<string> columns { get; set; }
        public List<List<string>> data { get; set; }
    }
    public class StaticData
    {
        public StringData securities { get; set; }
    }
    public class StringHistoryData
    {
        public StringData history { get; set; }
    }

    public class DoubleData
    {
        public List<string> columns { get; set; }
        public List<List<double?>> data { get; set; }
    }
    public class DynamicData
    {
        public DoubleData marketdata { get; set; }
    }
    public class DoubleHistoryData
    {
        public DoubleData history { get; set; }
    }
}
