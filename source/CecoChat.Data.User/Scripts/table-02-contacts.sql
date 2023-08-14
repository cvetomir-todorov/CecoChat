CREATE TABLE IF NOT EXISTS public."Contacts"
(
    "User1Id" bigint NOT NULL,
    "User2Id" bigint NOT NULL,
    "Version" uuid NOT NULL,
    "Status" text COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT "Contacts_pkey" PRIMARY KEY ("User1Id", "User2Id")
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;

CREATE INDEX "Contacts_User1Id_index"
    ON public."Contacts" USING btree
    ("User1Id" ASC NULLS LAST);

CREATE INDEX "Contacts_User2Id_index"
    ON public."Contacts" USING btree
        ("User2Id" ASC NULLS LAST);

ALTER TABLE IF EXISTS public."Contacts"
    ADD CONSTRAINT "Contacts_User1Id_foreign"
        FOREIGN KEY ("User1Id") REFERENCES public."Profiles"("UserId");

ALTER TABLE IF EXISTS public."Contacts"
    ADD CONSTRAINT "Contacts_User2Id_foreign"
        FOREIGN KEY ("User2Id") REFERENCES public."Profiles"("UserId");

ALTER TABLE IF EXISTS public."Contacts"
    OWNER to yugabyte;
GRANT ALL ON TABLE public."Contacts" TO cecochat_dev;
GRANT ALL ON TABLE public."Contacts" TO yugabyte;
