CREATE TABLE chats.user_chats (
    user_id bigint,
    other_user_id bigint,
    chat_id text,
    newest_message bigint,
    other_user_delivered bigint,
    other_user_seen bigint,
    PRIMARY KEY (user_id, chat_id)
);
