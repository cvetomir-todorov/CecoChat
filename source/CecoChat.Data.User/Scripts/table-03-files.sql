CREATE TABLE IF NOT EXISTS public."Files"
(
    "Bucket" text COLLATE pg_catalog."default" NOT NULL,
    "Path" text COLLATE pg_catalog."default" NOT NULL,
    "UserId" bigint NOT NULL,
    "Version" timestamp with time zone NOT NULL,
    CONSTRAINT "Files_pkey" PRIMARY KEY ("Bucket", "Path")
)
WITH
(
    OIDS = FALSE
)
TABLESPACE pg_default;

CREATE INDEX "Files_UserId_Version_index"
    ON public."Files" USING btree
        ("UserId" ASC NULLS LAST, "Version" ASC NULLS LAST);

ALTER TABLE IF EXISTS public."Files"
    ADD CONSTRAINT "Files_UserId_foreign"
        FOREIGN KEY ("UserId") REFERENCES public."Profiles"("UserId");

ALTER TABLE IF EXISTS public."Files"
    OWNER to yugabyte;
GRANT ALL ON TABLE public."Files" TO cecochat_dev;
GRANT ALL ON TABLE public."Files" TO yugabyte;
