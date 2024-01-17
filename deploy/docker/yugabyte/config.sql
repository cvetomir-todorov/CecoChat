DO
$body$
    DECLARE
        version timestamp with time zone;
    BEGIN
        version := current_timestamp;

        -- partitioning
        INSERT INTO "Elements" VALUES ('partitioning.count', '12', version);
        INSERT INTO "Elements" VALUES ('partitioning.partitions', '0=0-5;1=6-11', version);
        INSERT INTO "Elements" VALUES ('partitioning.addresses', '0=https://localhost:31000;1=https://localhost:31001', version);
        -- history
        INSERT INTO "Elements" VALUES ('history.message-count', '32', version);
        -- snowflake
        INSERT INTO "Elements" VALUES ('snowflake.generator-ids', '0=0,1,2,3', version);
        -- user
        INSERT INTO "Elements" VALUES ('user.profile-count', '32', version);
    END
$body$
