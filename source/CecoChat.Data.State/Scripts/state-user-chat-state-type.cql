CREATE TYPE state.user_chat_state
(
  message_delivered_id bigint,
  message_seen_id bigint,
  newest_message_id bigint,
  new_messages_count smallint
);