# Calculations

Below are some calculations about the system.

# System limits

* Targets
  - 10 000 000 max users at the same time
  - At least 4 years of message storage
  - 256 bytes per message
* Infrastructure
  - 64 000 connections per messaging server
  - 160 messaging servers
  - 10 240 000 max active users are supported
* Time
  - 1 hour = 60 minutes * 60 seconds = 3 600 seconds
  - 1 day = 24 hours * 60 minutes * 60 seconds = 86 400 seconds

# Daily

* Users come and go throughout the day
* 64 000 000 active users daily
* 128 messages per user daily

### Messages

* 64 000 000 users x 128 messages = 8 192 000 000 messages daily
* 8 192 000 000 messages daily / 86 400 seconds = ~95 000 messages/s daily
* 95 000 messages/s / 160 messaging servers = ~600 messages/s per messaging server daily

### Throughput

* 8 192 000 000 messages * 256 bytes = 2 097 152 000 000 bytes = 2 048 000 000 KB = 2 000 000 MB daily = ~1 953 GB daily = ~1.91 TB daily
* 2 000 000 MB daily / 86 400 seconds  = ~23 MB/s daily
* 23 MB/s / 160 messaging servers = ~0.15 MB/s per messaging server daily

# Peak

* 1 hour peak
* 80% of system is busy = 8 000 000 active users
* 50% of user daily messages are sent = 64 messages for each user peak

### Messages

* 8 000 000 users * 64 messages = 512 000 000 messages peak
* 512 000 000 peak / 3 600 seconds = ~142 200 messages/s peak
* 142 000 messages/s / 160 messaging servers = ~900 messages/s per messaging server peak

### Throughput

* 512 000 000 messages * 256 bytes = 131 072 000 000 bytes = 128 000 000 KB = 125 000 MB peak = ~122 GB peak = ~0.12 TB peak
* 125 000 MB peak / 3 600 seconds = ~35 MB/s peak
* 35 MB/s / 160 messaging servers = ~0.22 MB/s per messaging server peak

# Storage

* 2 000 000 MB daily (from daily throughput)
* 730 000 000 MB yearly
* 2 920 000 000 MB for 4 years = 2 851 562 GB for 4 years = ~2 785 TB for 4 years = ~2.72 PT for 4 years
* ~44 PT for 64 years
* These calculations do not take into account data compression
