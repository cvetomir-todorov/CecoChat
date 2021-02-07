FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app
COPY Connect/. ./
ENTRYPOINT ["dotnet", "CecoChat.Connect.Server.dll"]