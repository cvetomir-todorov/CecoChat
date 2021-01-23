CREATE TABLE messaging.messages_for_user
(
  receiver_id bigint,
  when timestamp,
  data blob,
  PRIMARY KEY (receiver_id, when)
)
WITH CLUSTERING ORDER BY (when DESC);