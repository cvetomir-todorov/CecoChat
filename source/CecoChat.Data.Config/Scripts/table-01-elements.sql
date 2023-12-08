CREATE TABLE IF NOT EXISTS public."Elements"
(
    "Name" text NOT NULL,
    "Value" text NOT NULL,
    "Version" timestamp with time zone NOT NULL,
    PRIMARY KEY ("Name")
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."Elements"
    OWNER to yugabyte;
GRANT ALL ON TABLE public."Elements" TO cecochat_dev;
GRANT ALL ON TABLE public."Elements" TO yugabyte;
