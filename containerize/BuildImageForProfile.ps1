dotnet publish ..\source\CecoChat.Server.Profile\CecoChat.Server.Profile.csproj -o .\Profile -c Debug
docker build -f .\Profile.Dockerfile -t ceco.com/cecochat/profile:0.1 .