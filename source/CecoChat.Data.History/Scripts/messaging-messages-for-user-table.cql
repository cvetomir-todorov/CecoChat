CREATE TABLE messaging.messages_for_user
(
  user_id bigint,
  message_id bigint,
  sender_id bigint,
  receiver_id bigint,
  type tinyint,
  status tinyint,
  data text,
  reactions map<bigint,text>,
  PRIMARY KEY (user_id, message_id)
)
WITH CLUSTERING ORDER BY (message_id DESC);