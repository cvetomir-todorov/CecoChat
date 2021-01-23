CREATE KEYSPACE messaging
  WITH REPLICATION =
  {
    'class' : 'NetworkTopologyStrategy',
    'Europe' : 3,
    'Asia' : 3,
    'America' : 3
  }
  AND DURABLE_WRITES = true;