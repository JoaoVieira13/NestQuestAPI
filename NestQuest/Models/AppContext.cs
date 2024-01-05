using Microsoft.EntityFrameworkCore;
using NestQuest.Models;

namespace NestQuest.Models
{
    public class NestQuesteContext : DbContext
    {
        public NestQuesteContext(DbContextOptions<NestQuesteContext> options)
        : base(options)
        {
        }

        public DbSet<Offer> Offers { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Token> Tokens { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<Hiring> Hirings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Offer>()
                .HasMany(o => o.Comments)
                .WithOne()
                .HasForeignKey(c => c.OfferId);

            modelBuilder.Entity<Offer>()
                .HasMany(o => o.Hirings)
                .WithOne()
                .HasForeignKey(c => c.OfferId);

            modelBuilder.Entity<User>()
                .HasMany(o => o.Hirings)
                .WithOne()
                .HasForeignKey(c => c.UserId);

            base.OnModelCreating(modelBuilder);
        }
    }

}
