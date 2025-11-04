using System;
using System.Threading.Tasks;
using Ares.Datamodel;
using Microsoft.AspNetCore.Identity;

namespace AresService;

public static class RolesExtensions
{
  public static async Task InitializeAsync(this RoleManager<IdentityRole> roleManager)
  {
    foreach (var roleName in Enum.GetNames(typeof(AresUserType)))
      if (!await roleManager.RoleExistsAsync(roleName))
        await roleManager.CreateAsync(new IdentityRole(roleName));
  }
}
