CREATE TABLE messaging.messages_for_dialog
(
  dialog_id text,
  message_id text,
  sender_id bigint,
  receiver_id bigint,
  when timestamp,
  message_type tinyint,
  data map<text,text>,
  PRIMARY KEY (dialog_id, when)
)
WITH CLUSTERING ORDER BY (when DESC);