docker build -f Connect.Dockerfile -t ceco.com/cecochat/connect:0.1 ../../source/
docker build -f History.Dockerfile -t ceco.com/cecochat/history:0.1 ../../source/
docker build -f IDGen.Dockerfile -t ceco.com/cecochat/idgen:0.1 ../../source/
docker build -f Materialize.Dockerfile -t ceco.com/cecochat/materialize:0.1 ../../source/
docker build -f Messaging.Dockerfile -t ceco.com/cecochat/messaging:0.1 ../../source/
docker build -f Profile.Dockerfile -t ceco.com/cecochat/profile:0.1 ../../source/