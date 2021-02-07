dotnet publish ..\source\CecoChat.History.Server\CecoChat.History.Server.csproj -o .\History -c Debug
docker build -f .\History.Dockerfile -t ceco.com/cecochat/history:0.1 .