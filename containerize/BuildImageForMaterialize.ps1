dotnet publish ..\source\CecoChat.Materialize.Server\CecoChat.Materialize.Server.csproj -o .\Materialize -c Debug
docker build -f .\Materialize.Dockerfile -t ceco.com/cecochat/materialize:0.1 .