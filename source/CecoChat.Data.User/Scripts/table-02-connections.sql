CREATE TABLE IF NOT EXISTS public."Connections"
(
    "User1Id" bigint NOT NULL,
    "User2Id" bigint NOT NULL,
    "Version" timestamp with time zone NOT NULL,
    "Status" text COLLATE pg_catalog."default" NOT NULL,
    "TargetId" bigint NOT NULL,
    CONSTRAINT "Connections_pkey" PRIMARY KEY ("User1Id", "User2Id")
)
WITH
(
    OIDS = FALSE
)
TABLESPACE pg_default;

CREATE INDEX "Connections_User1Id_index"
    ON public."Connections" USING btree
    ("User1Id" ASC NULLS LAST);

CREATE INDEX "Connections_User2Id_index"
    ON public."Connections" USING btree
        ("User2Id" ASC NULLS LAST);

ALTER TABLE IF EXISTS public."Connections"
    ADD CONSTRAINT "Connections_User1Id_foreign"
        FOREIGN KEY ("User1Id") REFERENCES public."Profiles"("UserId");

ALTER TABLE IF EXISTS public."Connections"
    ADD CONSTRAINT "Connections_User2Id_foreign"
        FOREIGN KEY ("User2Id") REFERENCES public."Profiles"("UserId");

ALTER TABLE IF EXISTS public."Connections"
    OWNER to yugabyte;
GRANT ALL ON TABLE public."Connections" TO cecochat_dev;
GRANT ALL ON TABLE public."Connections" TO yugabyte;
