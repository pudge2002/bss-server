namespace WebApplication2.Data
{
    using Microsoft.EntityFrameworkCore;
    using WebApplication2.Models;

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Camera> Cameras { get; set; }
    }
}
