CREATE TABLE IF NOT EXISTS public.elements
(
    name text NOT NULL,
    value text NOT NULL,
    version timestamp with time zone NOT NULL,
    CONSTRAINT elements_pkey
        PRIMARY KEY (name)
)
WITH
(
    OIDS = FALSE
)
TABLESPACE pg_default;

CREATE INDEX elements_name
    ON public.elements USING btree
        (name ASC NULLS LAST);

ALTER TABLE IF EXISTS public.elements
    OWNER to yugabyte;
GRANT ALL ON TABLE public.elements TO cecochat_dev;
GRANT ALL ON TABLE public.elements TO yugabyte;
