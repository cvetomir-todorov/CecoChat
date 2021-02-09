FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app
COPY Profile/. ./
ENTRYPOINT ["dotnet", "CecoChat.Profile.Server.dll"]