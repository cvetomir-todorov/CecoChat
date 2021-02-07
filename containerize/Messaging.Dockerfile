FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app
COPY Messaging/. ./
ENTRYPOINT ["dotnet", "CecoChat.Messaging.Server.dll"]