FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app
COPY History/. ./
ENTRYPOINT ["dotnet", "CecoChat.History.Server.dll"]