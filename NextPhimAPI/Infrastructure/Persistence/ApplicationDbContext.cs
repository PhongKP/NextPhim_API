using Microsoft.EntityFrameworkCore;
using NextPhimAPI.Domain.Entities;

namespace NextPhimAPI.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        
        public DbSet<Movie> Movies { get; set; }
    }
}
