CREATE TABLE messaging.messages_for_dialog
(
  dialog_id text,
  when timestamp,
  data blob,
  PRIMARY KEY (dialog_id, when)
)
WITH CLUSTERING ORDER BY (when DESC);