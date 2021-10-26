CREATE TABLE state.user_chats
(
  user_id bigint,
  chats set<text>,
  PRIMARY KEY (user_id)
);