namespace Invest.Models
{
    public class RecomendView
    {
        public int InvestSum { get; set; }
        public int Months { get; set; }
        public int SharesPercent { get; set; }
        public int BondsPercent { get; set; }
        public List<RecomendTableData> SharesList { get; set; } = new List<RecomendTableData>();
        public List<RecomendTableData> BondsList { get; set; } = new List<RecomendTableData>();
    }
}
