CREATE TABLE messaging.messages
(
  user_id bigint,
  when timestamp,
  data blob,
  PRIMARY KEY (user_id, when)
)
WITH CLUSTERING ORDER BY (when DESC);