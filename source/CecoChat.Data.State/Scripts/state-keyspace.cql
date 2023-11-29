CREATE KEYSPACE state
  WITH REPLICATION =
  {
    'class' : 'NetworkTopologyStrategy',
    'Europe' : 2
  }
  AND DURABLE_WRITES = true;
