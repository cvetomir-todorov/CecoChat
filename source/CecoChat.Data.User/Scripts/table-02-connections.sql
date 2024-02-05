CREATE TABLE IF NOT EXISTS public.connections
(
    user1_id bigint NOT NULL,
    user2_id bigint NOT NULL,
    version timestamp with time zone NOT NULL,
    status text COLLATE pg_catalog."default" NOT NULL,
    target_id bigint NOT NULL,
    CONSTRAINT connections_pkey
        PRIMARY KEY (user1_id, user2_id),
    CONSTRAINT connections_user1_id_foreign
        FOREIGN KEY (user1_id) REFERENCES public.profiles (user_id),
    CONSTRAINT connections_user2_id_foreign
        FOREIGN KEY (user2_id) REFERENCES public.Profiles (user_id)
)
WITH
(
    OIDS = FALSE
)
TABLESPACE pg_default;

CREATE INDEX connections_user1_id_index
    ON public.connections USING btree
        (user1_id ASC NULLS LAST);

CREATE INDEX connections_user2_id_index
    ON public.connections USING btree
        (user2_id ASC NULLS LAST);

ALTER TABLE IF EXISTS public.connections
    OWNER to yugabyte;
GRANT ALL ON TABLE public.connections TO cecochat_dev;
GRANT ALL ON TABLE public.connections TO yugabyte;
