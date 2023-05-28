namespace Invest.Models
{
    public class UserStock
    {
        public int UserId { get; set; }
        public User User { get; set; }
        public string SecId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public float PurchasePrice{ get; set; } // за единицу или в сумме?
        
        //public float SaleAmount { get; set; }

    }
    //public enum Status
    //{

    //}
}
