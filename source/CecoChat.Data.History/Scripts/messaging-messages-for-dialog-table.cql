CREATE TABLE messaging.messages_for_dialog
(
  dialog_id text,
  message_id bigint,
  sender_id bigint,
  receiver_id bigint,
  message_type tinyint,
  data map<text,text>,
  PRIMARY KEY (dialog_id, message_id)
)
WITH CLUSTERING ORDER BY (message_id DESC);