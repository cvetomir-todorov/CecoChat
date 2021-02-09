dotnet publish ..\source\CecoChat.Profile.Server\CecoChat.Profile.Server.csproj -o .\Profile -c Debug
docker build -f .\Profile.Dockerfile -t ceco.com/cecochat/profile:0.1 .