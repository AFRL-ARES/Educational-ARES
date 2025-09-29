using Ares.Core;
﻿using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace AresService;

public class AresDbContext : CoreDatabaseContext
{
  public AresDbContext(DbContextOptions<AresDbContext> options) : base(options)
  {
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    DatabaseRuntimeEnvironment.DatabaseProvider = Database.ProviderName;
    var assembly = Assembly.GetAssembly(typeof(AresDbContext));
    if(assembly is null)
      return;

    modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    base.OnModelCreating(modelBuilder);
  }
}
