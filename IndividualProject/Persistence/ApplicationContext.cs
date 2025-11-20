using Microsoft.EntityFrameworkCore;

namespace Week4.Persistence;

public class ApplicationContext : DbContext
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; } = null!;
}
