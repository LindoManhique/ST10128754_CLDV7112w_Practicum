using AgriStream.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AgriStream.Data
{
    public class AgriStreamDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public AgriStreamDbContext(
            DbContextOptions<AgriStreamDbContext> options)
            : base(options)
        {
        }

        public DbSet<LegacyApplicationUser> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LegacyApplicationUser>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
    }
}