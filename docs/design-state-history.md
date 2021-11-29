# History

History servers use a standard Kafka consumer group with automatic partition assignment and balancing. Their role is to create the history data in the Cassandra database. Currently 2 types of queries are supported which reflects the Cassandra tables that is used.

* `Get user history` - returns a predefined max number of messages sent to the user with the specified user ID which are older than a specified date. In order to support this query for both sender and receiver the message is entered in the database twice. The table has a `user ID` column which is the partitioning key. It is separate from the `sender ID` and `receiver ID`. The `message ID` which is a snowflake is used as a clustering key. The timestamp from the request is converted to a snowflake in order to satisfy the query.
* `Get dialog history` - returns a predefined max number of messages between 2 users with the specified user IDs which are older than a specified date. The partition key here is a string of `userID1-userID2`. To avoid ambiguity `userID1` is always the smaller. The rest of the details are the same as the previous query.
