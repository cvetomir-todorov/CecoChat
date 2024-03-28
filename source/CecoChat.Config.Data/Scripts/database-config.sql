CREATE DATABASE config
    WITH
    OWNER = yugabyte
        ENCODING = 'UTF8'
        LC_COLLATE = 'C'
        LC_CTYPE = 'en_US.UTF-8'
        CONNECTION LIMIT = -1
        IS_TEMPLATE = False;

GRANT TEMPORARY, CONNECT ON DATABASE config TO PUBLIC;
GRANT ALL ON DATABASE config TO yugabyte;
GRANT ALL ON DATABASE config TO cecochat_dev;
