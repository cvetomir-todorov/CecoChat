CREATE TABLE chats.chat_messages
(
  chat_id text,
  message_id bigint,
  sender_id bigint,
  receiver_id bigint,
  type tinyint,
  text text,
  file chats.file_data,
  reactions map<bigint,text>,
  PRIMARY KEY (chat_id, message_id)
)
WITH CLUSTERING ORDER BY (message_id DESC);
