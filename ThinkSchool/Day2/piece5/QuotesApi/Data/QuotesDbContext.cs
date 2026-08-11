using Microsoft.EntityFrameworkCore;
using QuotesApi.Models;

namespace QuotesApi.Data;

public class QuotesDbContext : DbContext
{
    public QuotesDbContext(DbContextOptions<QuotesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(80);
            entity.Property(c => c.OwnerId).IsRequired();

            entity.OwnsMany(c => c.Items, item =>
            {
                item.WithOwner().HasForeignKey("CollectionId");
                item.Property<int>("Id");
                item.HasKey("Id");
                item.Property(i => i.QuoteId).IsRequired();
                item.Property(i => i.AddedAt).IsRequired();
            });
        });
    }
}