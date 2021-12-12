# Back of the envelope calculations

## System limits

* Targets
  - 100 000 000 max users at the same time
  - At least 4 years of message storage
  - 256 bytes per message
* Infrastructure
  - 64 000 connections per messaging server
  - 1 600 messaging servers
  - 102 400 000 max active users are supported
* Time
  - 1 hour = 60 minutes * 60 seconds = 3 600 seconds
  - 1 day = 24 hours * 60 minutes * 60 seconds = 86 400 seconds

## Daily

* Users come and go throughout the day
* 640 000 000 active users daily
* 128 messages per user daily

### Messages

* 640 000 000 users x 128 messages = 81 920 000 000 messages daily
* 81 920/ 000 000 daily / 86 400 seconds = ~950 000 messages/s daily
* 950 000 messages/s / 1 600 messaging servers = ~594 messages/s per messaging server daily

### Throughput

* 81 920 000 000 messages * 256 bytes = 20 971 520 000 000 bytes = 20 480 000 000 KB = 20 000 000 MB daily = ~19 532 GB daily = ~19.1 TB daily
* 20 000 000 MB daily / 86 400 seconds  = ~232 MB/s daily
* 232 MB/s / 1 600 messaging servers = ~0.15 MB/s per messaging server daily

## Peak

* 1 hour peak
* 80% of system is busy = 80 000 000 active users
* 50% of user daily messages are sent = 64 messages for each user peak

### Messages

* 80 000 000 users * 64 messages = 5 120 000 000 messages peak
* 5 120 000 000 peak / 3 600 seconds = ~1 423 000 messages/s peak
* 1 423 000 messages/s / 1 600 messaging servers = ~890 messages/s per messaging server peak

### Throughput

* 5 120 000 000 messages * 256 bytes = 1 310 720 000 000 bytes = 1 280 000 000 KB = 1 250 000 MB peak = ~1 221 GB peak = ~1.2 TB peak
* 1 250 000 MB peak / 3 600 seconds = ~348 MB/s peak
* 348 MB/s / 1 600 messaging servers = ~0.22 MB/s per messaging server peak

## Storage

* 20 000 000 MB daily (from daily throughput)
* 7 300 000 000 MB yearly
* 29 200 000 000 MB for 4 years = 28 515 625 GB for 4 years = ~27 850 TB for 4 years = ~28 PT for 4 years
* ~448 PT for 64 years
* These calculations do not take into account data compression
