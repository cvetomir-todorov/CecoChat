FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /app
COPY . ./
RUN dotnet publish CecoChat.Server.State/CecoChat.Server.State.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
LABEL author="Cvetomir Todorov"

WORKDIR /app
COPY --from=build /app/out .

ADD certificates/ceco-com.crt /usr/local/share/ca-certificates/ceco-com.crt
RUN chmod 644 /usr/local/share/ca-certificates/ceco-com.crt
RUN update-ca-certificates

ENTRYPOINT ["dotnet", "CecoChat.Server.State.dll"]
