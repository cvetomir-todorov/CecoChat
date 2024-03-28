FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app
COPY . ./
RUN dotnet publish CecoChat.Config.Service/CecoChat.Config.Service.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
LABEL author="Cvetomir Todorov"

RUN apt update && \
    apt install -y curl && \
    apt install -y dnsutils && \
    apt install -y net-tools &&\
    apt install -y iputils-ping && \
    apt clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/out .

ADD certificates/services.crt /usr/local/share/ca-certificates/services.crt
RUN chmod 644 /usr/local/share/ca-certificates/services.crt && \
    update-ca-certificates

ENTRYPOINT ["dotnet", "CecoChat.Config.Service.dll"]
