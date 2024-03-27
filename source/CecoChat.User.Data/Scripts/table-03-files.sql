CREATE TABLE IF NOT EXISTS public.files
(
    bucket text COLLATE pg_catalog."default" NOT NULL,
    path text COLLATE pg_catalog."default" NOT NULL,
    user_id bigint NOT NULL,
    version timestamp with time zone NOT NULL,
    allowed_users bigint[] NOT NULL,
    CONSTRAINT files_pkey
        PRIMARY KEY (bucket, path),
    CONSTRAINT files_user_id_foreign
        FOREIGN KEY (user_id) REFERENCES public.profiles (user_id)
)
WITH
(
    OIDS = FALSE
)
TABLESPACE pg_default;

CREATE INDEX files_user_id_version_index
    ON public.files USING btree
        (user_id ASC NULLS LAST, version ASC NULLS LAST);

ALTER TABLE IF EXISTS public.files
    OWNER to yugabyte;
GRANT ALL ON TABLE public.files TO cecochat_dev;
GRANT ALL ON TABLE public.files TO yugabyte;
