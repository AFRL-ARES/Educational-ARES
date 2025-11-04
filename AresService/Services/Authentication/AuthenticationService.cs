using System;
using System.Threading.Tasks;
using Ares.Messages;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace AresService.Services.Authentication;

public class AuthenticationService : Ares.Messages.Authentication.AuthenticationBase
{
  private readonly ILogger<AuthenticationService> _logger;
  private readonly JwtTokenGenerator _tokenGenerator;

  public AuthenticationService(JwtTokenGenerator tokenGenerator, ILogger<AuthenticationService> logger)
  {
    _tokenGenerator = tokenGenerator;
    _logger = logger;
  }

  //[AllowAnonymous]
  //public override async Task<AuthenticationResponse> Authenticate(AuthenticationRequest request, ServerCallContext context)
  //{
    //var token = await _tokenGenerator.GenerateTokenAsync(new AuthenticationCredentials(request.UserName, request.Password));

    //var authResponse = new AuthenticationResponse
    //{
    //  Success = false
    //};

    //if (token is null)
    //{
    //  authResponse.Errors.Add("Username or password is incorrect");
    //  return authResponse;
    //}

    //authResponse.Success = true;
    //authResponse.Token = new Token
    //{
    //  Expiration = token.Expiration != DateTime.MinValue ? token.Expiration.ToTimestamp() : null,
    //  TokenString = token.Token
    //};

    //_logger.LogInformation("User {} authenticated.", request.UserName);
    //return authResponse;
  //}
}
