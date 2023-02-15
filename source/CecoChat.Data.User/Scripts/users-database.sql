CREATE DATABASE users
    WITH
    OWNER = yugabyte
        ENCODING = 'UTF8'
        LC_COLLATE = 'C'
        LC_CTYPE = 'en_US.UTF-8'
        CONNECTION LIMIT = -1
        IS_TEMPLATE = False;

GRANT TEMPORARY, CONNECT ON DATABASE users TO PUBLIC;
GRANT ALL ON DATABASE users TO yugabyte;
GRANT ALL ON DATABASE users TO cecochat_dev;
