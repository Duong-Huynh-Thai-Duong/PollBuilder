using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PollBuilder.Domain.Entities;
using PollBuilder.Infrastructure.Identity;

namespace PollBuilder.Infrastructure.Data
{
    // Inheriting from IdentityDbContext automatically generates the AspNetUsers tables
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // The core tables for your Poll Builder
        public DbSet<Poll> Polls { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Option> Options { get; set; }
        public DbSet<Vote> Votes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // IMPORTANT: Must be called first for Identity tables to map correctly!
            base.OnModelCreating(builder);

            // 1. Poll -> Questions (1-to-Many)
            builder.Entity<Poll>()
                .HasMany(p => p.Questions)
                .WithOne(q => q.Poll)
                .HasForeignKey(q => q.PollId)
                .OnDelete(DeleteBehavior.Cascade); // Deleting a poll wipes its questions

            // 2. Question -> Options (1-to-Many)
            builder.Entity<Question>()
                .HasMany(q => q.Options)
                .WithOne(o => o.Question)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade); // Deleting a question wipes its options

            // 3. Question -> Votes (1-to-Many)
            builder.Entity<Question>()
                .HasMany(q => q.Votes)
                .WithOne(v => v.Question)
                .HasForeignKey(v => v.QuestionId)
                .OnDelete(DeleteBehavior.Cascade); // Deleting a question wipes its votes

            // 4. Option -> Votes (1-to-Many)
            builder.Entity<Option>()
                .HasMany(o => o.Votes)
                .WithOne(v => v.Option)
                .HasForeignKey(v => v.OptionId)
                // We use NoAction here to prevent SQL Server from throwing a "Multiple Cascade Paths" error.
                // Since the Vote is already deleted when the Question is deleted, we are perfectly safe!
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}