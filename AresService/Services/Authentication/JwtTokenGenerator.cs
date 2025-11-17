using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AresService.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AresService.Services.Authentication;

public class JwtTokenGenerator
{
  private readonly RoleManager<IdentityRole> _roleManager;
  private readonly SignInManager<AresUser> _signInManager;
  private readonly IOptions<TokensConfig> _tokensOptions;
  private readonly UserManager<AresUser> _userManager;

  public JwtTokenGenerator(UserManager<AresUser> userManager,
    SignInManager<AresUser> signInManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<TokensConfig> tokensOptions)
  {
    _userManager = userManager;
    _signInManager = signInManager;
    _roleManager = roleManager;
    _tokensOptions = tokensOptions;
  }

  //public async Task<InternalToken?> GenerateTokenAsync(AuthenticationCredentials credentials)
  //{
  //  var user = await _userManager.FindByNameAsync(credentials.UserName);
  //  if(user is null)
  //    return null;

  //  var authCheck = await _signInManager.CheckPasswordSignInAsync(user, credentials.Password, false);

  //  if(!authCheck.Succeeded)
  //    return null;

  //  // TODO revisit these claims if needed
  //  var claims = new List<Claim>
  //  {
  //    new(JwtRegisteredClaimNames.Sub, user.UserName),
  //    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
  //    new(JwtRegisteredClaimNames.UniqueName, user.UserName)
  //  };

  //  var userRoles = await _userManager.GetRolesAsync(user);
  //  var userClaims = await _userManager.GetClaimsAsync(user);

  //  claims.AddRange(userClaims);

  //  foreach(var userRole in userRoles)
  //  {
  //    claims.Add(new Claim(ClaimTypes.Role, userRole));
  //    var role = await _roleManager.FindByNameAsync(userRole);
  //    if(role is null)
  //      continue;

  //    var roleClaims = await _roleManager.GetClaimsAsync(role);
  //    claims.AddRange(roleClaims);
  //  }

  //  var tokenConfig = _tokensOptions.Value;
  //  var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenConfig.Key ?? "DefaultKey"));
  //  var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

  //  // expiration can also be added here, but that might involve additional handling to reauthenticate to prevent timeouts when
  //  // doing experiments and such
  //  var token = new JwtSecurityToken(tokenConfig.Issuer ?? "localhost", tokenConfig.Audience ?? "localhost", claims, signingCredentials: signingCredentials);

  //  return new InternalToken(new JwtSecurityTokenHandler().WriteToken(token), token.ValidTo);
  //}
}
