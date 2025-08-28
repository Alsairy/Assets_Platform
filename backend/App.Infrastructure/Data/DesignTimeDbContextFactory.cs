using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace App.Infrastructure.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            IConfigurationRoot cfg = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.{env}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var cs = cfg.GetConnectionString("DefaultConnection")
                     ?? Environment.GetEnvironmentVariable("DB__CONNECTION_STRING")
                     ?? "Host=localhost;Port=5432;Database=assets;Username=postgres;Password=postgres";

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(cs);
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
