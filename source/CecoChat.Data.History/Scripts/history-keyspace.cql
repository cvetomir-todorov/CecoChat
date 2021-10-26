CREATE KEYSPACE history
  WITH REPLICATION =
  {
    'class' : 'NetworkTopologyStrategy',
    'Europe' : 2,
    'Asia' : 2,
    'America' : 2
  }
  AND DURABLE_WRITES = true;