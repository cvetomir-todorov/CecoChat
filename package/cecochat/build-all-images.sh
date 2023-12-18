docker build -f bff.dockerfile -t ceco.com/cecochat/bff:0.1 ../../source/
docker build -f messaging.dockerfile -t ceco.com/cecochat/messaging:0.1 ../../source/
docker build -f chats.dockerfile -t ceco.com/cecochat/chats:0.1 ../../source/
docker build -f idgen.dockerfile -t ceco.com/cecochat/idgen:0.1 ../../source/
docker build -f user.dockerfile -t ceco.com/cecochat/user:0.1 ../../source/
docker build -f config.dockerfile -t ceco.com/cecochat/config:0.1 ../../source/
