using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AresService.Data;

public class AresIdentityContext : IdentityDbContext<AresUser>
{
  public AresIdentityContext(DbContextOptions<AresIdentityContext> options) : base(options)
  {
  }
}
