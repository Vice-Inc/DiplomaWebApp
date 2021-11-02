using DiplomaWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace DiplomaWebApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> contextOptions) : base(contextOptions)
        {
            Database.EnsureCreated();
        }

        public DbSet<FileModel> MusicFiles { get; set; }
    }
}
