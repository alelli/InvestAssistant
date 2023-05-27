namespace Invest.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public Role Role { get; set; }

    }
     
    public enum Role
    {
        Admin = 0,
        User,
        Guest
    }

}
