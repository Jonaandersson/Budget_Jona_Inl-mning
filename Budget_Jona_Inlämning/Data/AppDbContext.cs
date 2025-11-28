using Budget_Jona_Inlämning.Data;
using Budget_Jona_Inlämning.Models;
using Microsoft.EntityFrameworkCore;
#nullable enable

namespace Budget_Jona_Inlämning.Data;

/// <summary>
/// Represents the Entity Framework Core database context for the application, providing access to categories,
/// transactions, and income/loss records.
/// </summary>
/// <remarks>Use this context to query and save instances of the application's domain entities. The context
/// configures entity mappings and conversions, including handling decimal values as doubles for compatibility with
/// SQLite. This class is intended to be registered with dependency injection and used within a unit of work pattern.
/// AppDbContext is sealed and should not be inherited.</remarks>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => this.Set<Category>();
    public DbSet<Transaction> Transactions => this.Set<Transaction>();
    public DbSet<IncomeLoss> IncomeLosses => this.Set<IncomeLoss>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>()
            .HasIndex(c => c.Name)
            .IsUnique(false);

        modelBuilder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasConversion<double>();

        modelBuilder.Entity<IncomeLoss>()
            .Property(i => i.AmountLost)
            .HasConversion<double>();

        modelBuilder.Entity<IncomeLoss>()
            .Property(i => i.RefundPercentage)
            .HasConversion<double>();
    }
}