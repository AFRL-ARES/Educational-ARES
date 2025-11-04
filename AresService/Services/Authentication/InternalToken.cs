using System;

namespace AresService.Services.Authentication;

public record InternalToken(string Token, DateTime Expiration);
