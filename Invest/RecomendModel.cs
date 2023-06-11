namespace Invest
{
    public class RecomendModel
    {
        public int InvestSum { get; set; }
        public int Months { get; set; }
        public int SharesPercent { get; set; }
        public int BondsPercent { get; set; }
        public List<RecomendData> SharesList { get; set; }
        public List<RecomendData> BondsList { get; set; }
    }
}
