using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AresService.DbDesignFactories;

public abstract class BaseDesignFactory<T> : IDesignTimeDbContextFactory<T> where T : DbContext
{
  public T CreateDbContext(string[] args)
  {
    var provider = "Sqlite";

    for(var i = 0; i < args.Length - 1; i++)
    {
      if(args[i].Equals("--provider", StringComparison.OrdinalIgnoreCase))
      {
        provider = args[i + 1];
        break;
      }
    }

    var optionsBuilder = new DbContextOptionsBuilder<T>();

    switch(provider)
    {
      case "Sqlite":
        optionsBuilder.UseSqlite(
            "Data Source=Data/app.db", b => b.MigrationsAssembly("AresService.Migrations.Sqlite"));
        break;

      case "Postgres":
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=ares;Username=postgres;Password=postgres", b => b.MigrationsAssembly("AresService.Migrations.Postgres"));
        break;

      case "SqlServer":
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=Ares;Trusted_Connection=True;TrustServerCertificate=True;", b => b.MigrationsAssembly("AresService.Migrations.SqlServer"));
        break;

      default:
        throw new InvalidOperationException($"Unknown provider: {provider}");
    }

    return (T)Activator.CreateInstance(typeof(T), optionsBuilder.Options)!;
  }
}
