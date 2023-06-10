namespace Invest
{
    public class RecomendModel
    {
        public int Sum { get; set; }
        public int Months { get; set; }
        public int SharesPercent { get; set; }
        public int BondsPercent { get; set; }
        public Dictionary<string, float> Prediction { get; set; }
    }
}
