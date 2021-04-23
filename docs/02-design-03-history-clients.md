# History

Materialize servers use a standard Kafka consumer group with automatic partition assignment and balancing. Their role is to create the history data in the Cassandra database. Currently 2 types of queries are supported which reflects the Cassandra tables that is used.

* `Get user history` - returns a predefined max number of messages sent to the user with the specified user ID which are older than a specified date. In order to support this query for both sender and receiver the message is entered in the database twice. The table has a `user ID` column which is the partitioning key. It is separate from the `sender ID` and `receiver ID`. The `message ID` which is a snowflake is used as a clustering key. The timestamp from the request is converted to a snowflake in order to satisfy the query.
* `Get dialog history` - returns a predefined max number of messages between 2 users with the specified user IDs which are older than a specified date. The partition key here is a string of `userID1-userID2`. To avoid ambiguity `userID1` is always the smaller. The rest of the details are the same as the previous query.

# Clients

![Clients](images/cecochat-04-clients.png)

Because of the messaging servers state each client needs to be connected to the correct messaging server. This problem could be solved via a load balancer which extracts the user ID from the client's access token. This is an operation which would require an additional decryption and application load-balancing for every message. Instead the connect server is used to find out which the messaging server is. This happens only once and the clients use that address to connect directly to their messaging server. There are operational issues with this approach but the additional application load-balancing and decryption is avoided. To make things consistent the connect server returns the history server address as well, but here it could be the HTTP load-balancer address.

A client's way of being consistent with the latest messages is to start listening for new ones from the messaging server first. After than the client can query for the user history using the current date until it decides it has caught up by checking the date of the oldest returned message. Additionally, each client can explore a dialog with a certain user in the same manner using the second query supported by the history database. In order to handle duplicate messages which could be already present in the client's local database each message has a unique ID used for deduplication.
