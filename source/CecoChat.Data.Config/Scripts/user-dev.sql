DO
$body$
    DECLARE
        user_count INTEGER;
    BEGIN
        SELECT COUNT(rolname)
        INTO user_count
        FROM pg_catalog.pg_roles
        WHERE rolname = 'cecochat_dev';

        IF user_count = 1 THEN
            RAISE NOTICE 'User cecochat_dev already exists';
        END IF;

        IF user_count = 0 THEN
            CREATE USER cecochat_dev WITH PASSWORD 'secret';
            RAISE NOTICE 'User cecochat_dev created';
        END IF;
    END
$body$
