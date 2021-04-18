CREATE TABLE messaging.messages_for_user
(
  user_id bigint,
  message_id uuid,
  message_id_snowflake bigint,
  sender_id bigint,
  receiver_id bigint,
  when timestamp,
  message_type tinyint,
  data map<text,text>,
  PRIMARY KEY (user_id, when)
)
WITH CLUSTERING ORDER BY (when DESC);