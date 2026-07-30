using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace Tanakh.Infrastructure.Data
{
    // Used only by `dotnet ef` at design time (migrations add/script/update).
    // Deliberately bypasses the ASP.NET Core host so migrations run against
    // ConnectionStrings__MigrationsDb (the direct/unpooled, higher-privilege
    // connection - see D-01/D-12) rather than the app's pooled runtime
    // connection wired up in Program.cs.
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MigrationsDb")
                ?? throw new InvalidOperationException(
                    "The ConnectionStrings__MigrationsDb environment variable must be set to run migrations.");

            DbContextOptionsBuilder<AppDbContext> optionsBuilder = new();
            optionsBuilder.UseNpgsql(connectionString);
            optionsBuilder.UseSnakeCaseNamingConvention();

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
