dotnet publish ..\source\CecoChat.Messaging.Server\CecoChat.Messaging.Server.csproj -o .\Messaging -c Debug
docker build -f .\Messaging.Dockerfile -t ceco.com/cecochat/messaging:0.1 .