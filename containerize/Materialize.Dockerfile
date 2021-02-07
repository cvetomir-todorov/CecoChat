FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app
COPY Materialize/. ./
ENTRYPOINT ["dotnet", "CecoChat.Materialize.Server.dll"]