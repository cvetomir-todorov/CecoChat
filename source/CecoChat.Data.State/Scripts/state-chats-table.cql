CREATE TABLE state.chats
(
  chat_id text,
  states map<bigint, frozen<user_chat_state>>,
  PRIMARY KEY (chat_id)
);