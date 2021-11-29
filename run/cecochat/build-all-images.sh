docker build -f Bff.Dockerfile -t ceco.com/cecochat/bff:0.1 ../../source/
docker build -f Messaging.Dockerfile -t ceco.com/cecochat/messaging:0.1 ../../source/
docker build -f State.Dockerfile -t ceco.com/cecochat/state:0.1 ../../source/
docker build -f History.Dockerfile -t ceco.com/cecochat/history:0.1 ../../source/
docker build -f IDGen.Dockerfile -t ceco.com/cecochat/idgen:0.1 ../../source/
