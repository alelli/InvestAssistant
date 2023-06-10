namespace Invest.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions options) : base(options) { }
        public DbSet<User> Users => Set<User>();
        public DbSet<UserStock> UserStocks => Set<UserStock>();
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserStock>().HasKey(u => new { u.UserId, u.SecId });

            //modelBuilder.Entity<UserStock>().
            //    //.HasOne(s => s.UserId)
            //    .WithMany(u => u.Users)
            //    .HasForeignKey(u => u.UserId)
            //    .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
