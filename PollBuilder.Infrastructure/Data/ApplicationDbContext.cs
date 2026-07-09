using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PollBuilder.Domain.Entities;
using PollBuilder.Infrastructure.Identity;

namespace PollBuilder.Infrastructure.Data
{
    // Inherit from IdentityDbContext to automatically generate security tables
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // These translate directly to SQL Server tables
        public DbSet<Poll> Polls { get; set; }
        public DbSet<PollOption> PollOptions { get; set; }
        public DbSet<Vote> Votes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Must call the base method first for Identity to work properly!
            base.OnModelCreating(builder);

            // Configure exactly how the tables interact
            builder.Entity<Poll>()
                .HasMany(p => p.Options)
                .WithOne(o => o.Poll)
                .HasForeignKey(o => o.PollId)
                .OnDelete(DeleteBehavior.Cascade); // Deleting a poll wipes its options automatically

            builder.Entity<PollOption>()
                .HasMany(o => o.Votes)
                .WithOne(v => v.PollOption)
                .HasForeignKey(v => v.PollOptionId)
                .OnDelete(DeleteBehavior.Cascade); // Deleting an option wipes its votes automatically
        }
    }
}