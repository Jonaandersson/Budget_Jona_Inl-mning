#nullable enable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Budget_Jona_Inlämning.Data;


/// <summary>
/// Provides a design-time factory for creating instances of <see cref="AppDbContext"/> for use with Entity Framework
/// Core tooling.
/// </summary>
/// <remarks>This class is used by Entity Framework Core tools, such as migrations, to instantiate the
/// application's database context at design time. It should be internal or private to prevent accidental use at
/// runtime. The factory configures the context to use a SQLite database with the connection string "Data
/// Source=budget.db". For more information, see
/// https://learn.microsoft.com/en-us/ef/core/cli/dbcontext-creation.</remarks>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<AppDbContext> optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite("Data Source=budget.db");

        return new AppDbContext(optionsBuilder.Options);
    }
}