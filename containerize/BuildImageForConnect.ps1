dotnet publish ..\source\CecoChat.Connect.Server\CecoChat.Connect.Server.csproj -o .\Connect -c Debug
docker build -f .\Connect.Dockerfile -t ceco.com/cecochat/connect:0.1 .