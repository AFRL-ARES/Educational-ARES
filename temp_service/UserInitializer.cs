using System;
using System.Linq;
using System.Threading.Tasks;
using Ares.Datamodel;
using AresService.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AresService;

public class UserInitializer
{
  private readonly RoleManager<IdentityRole> _roleManager;
  private readonly UserManager<AresUser> _userManager;

  public UserInitializer(UserManager<AresUser> userManager, RoleManager<IdentityRole> roleManager)
  {
    _userManager = userManager;
    _roleManager = roleManager;
  }

  /// <summary>
  /// Makes a default admin account which can then be used to create other users
  /// </summary>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException">Failed to create the admin user or the admin role</exception>
  public async Task Init()
  {
    var username = "admin";
    var password = "123456";

    if(await _userManager.Users.AnyAsync(user => user.UserName == username))
      return;

    var user = new AresUser
    {
      UserName = username,
      Email = "test@testmail.com"
    };

    var userCreation = await _userManager.CreateAsync(user, password);
    if(!userCreation.Succeeded)
      throw new InvalidOperationException($"Failed to create user: {user.UserName}, Reason: {userCreation.Errors.Select(error => error.Description).Aggregate((test, test2) => $"{test}\n {test2}")}");

    await _userManager.AddToRoleAsync(user, AresUserType.AresAdmin.ToString());
    await _userManager.AddToRoleAsync(user, AresUserType.AresUser.ToString());
  }
}
