using Budget_Jona_Inlämning.Data;
using Budget_Jona_Inlämning.Models;
using Microsoft.EntityFrameworkCore;
#nullable enable

namespace Budget_Jona_Inlämning.Data;

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

        // SQLite doesn't support decimal natively; use double conversions
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