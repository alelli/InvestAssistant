namespace Invest
{
    public class StockInfo
    {
        public string SecId { get; set; } = string.Empty;
        public string SecName { get; set; } = string.Empty;
        public float LastPrice { get; set; }
        public float LastChange{ get; set; }
        public int Amount { get; set; }

    }
}
