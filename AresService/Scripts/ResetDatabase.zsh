#!/bin/zsh

## This script is not the official way of generating/applying migrations and should
## only be used for development/testing


dotnet ef database drop --context AresDbContext -f --no-build --project ../AresService.csproj
dotnet ef migrations remove --context AresIdentityContext --no-build --project ../AresService.csproj
dotnet ef migrations remove --context AresDbContext --no-build --project ../AresService.csproj
dotnet ef migrations add DatabaseInit --context AresDbContext --project ../AresService.csproj
dotnet ef migrations add DatabaseInit --context AresIdentityContext --project ../AresService.csproj
dotnet ef database update --context AresDbContext --project ../AresService.csproj
dotnet ef database update --context AresIdentityContext --project ../AresService.csproj