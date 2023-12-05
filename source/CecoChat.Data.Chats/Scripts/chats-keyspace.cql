CREATE KEYSPACE chats
  WITH REPLICATION =
  {
    'class' : 'NetworkTopologyStrategy',
    'Europe' : 2
  }
  AND DURABLE_WRITES = true;
