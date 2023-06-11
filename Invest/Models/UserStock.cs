namespace Invest.Models
{
    public class UserStock
    {
        public int UserId { get; set; }
        public User User { get; set; }
        public string SecId { get; set; } = string.Empty;
        public string SecName { get; set; } = string.Empty;
        public float PurchasePrice{ get; set; } // за единицу или в сумме?
        public int Quantity { get; set; }
        
        //public float SaleAmount { get; set; }

    }
}
