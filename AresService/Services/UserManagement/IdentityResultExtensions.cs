using System.Linq;
using Ares.Messages;
using Microsoft.AspNetCore.Identity;

namespace AresService.Services.UserManagement;

public static class IdentityResultExtensions
{
  public static ManagementResponse ToManagementResponse(this IdentityResult result)
  {
    if (!result.Succeeded)
    {
      var response = new ManagementResponse
      {
        Success = false
      };

      response.Errors.AddRange(result.Errors.Select(error => error.Description));
      return response;
    }

    return new ManagementResponse { Success = true };
  }
}
