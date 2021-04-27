FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app

COPY . ./
RUN dotnet publish CecoChat.Profile.Server/CecoChat.Profile.Server.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

ADD certificates/ceco-com.crt /usr/local/share/ca-certificates/ceco-com.crt
RUN chmod 644 /usr/local/share/ca-certificates/ceco-com.crt
RUN update-ca-certificates

ENTRYPOINT ["dotnet", "CecoChat.Profile.Server.dll"]
