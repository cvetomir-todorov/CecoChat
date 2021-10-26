CREATE TABLE history.messages_for_chat
(
  chat_id text,
  message_id bigint,
  sender_id bigint,
  receiver_id bigint,
  type tinyint,
  status tinyint,
  data text,
  reactions map<bigint,text>,
  PRIMARY KEY (chat_id, message_id)
)
WITH CLUSTERING ORDER BY (message_id DESC);