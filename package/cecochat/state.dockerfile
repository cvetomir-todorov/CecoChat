FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

WORKDIR /app
COPY . ./
RUN dotnet publish CecoChat.Server.State/CecoChat.Server.State.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
LABEL author="Cvetomir Todorov"

RUN apt update && \
    apt install -y curl && \
    apt install -y dnsutils && \
    apt clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/out .

ADD certificates/services.crt /usr/local/share/ca-certificates/services.crt
RUN chmod 644 /usr/local/share/ca-certificates/services.crt && \
    update-ca-certificates

ENTRYPOINT ["dotnet", "CecoChat.Server.State.dll"]
