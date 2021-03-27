# Cell limits

* Infrastructure
  - 50 000 connections per messaging server
  - 2 000 messaging servers
* Time
  - 1 hour = 60 minutes * 60 seconds = 3 600 seconds
  - 1 day = 24 hours * 60 minutes * 60 seconds = 86 400 seconds
* Other
  - 100 000 000 max users at the same time
  - 256 bytes per message
  - 4 years of message storage

## Daily

* Users come and go throughout the day
* 640 000 000 active users daily
* 64 messages per user daily

### Messages

* 640 000 000 users x 64 messages = 40 960 000 000 messages daily
* 40 960 000 000 daily / 86 400 seconds = ~475 000 messages/s daily
* 475 000 messages/s / 2000 messaging servers = ~237 messages/s per messaging server daily

### Throughput

* 40 960 000 000 messages * 256 bytes = 10 485 760 000 000 bytes = 10 240 000 000 KB = 10 000 000 MB daily
* 10 000 000 MB daily / 86 400 seconds  = ~116 MB/s daily
* 116 MB/s / 2000 messaging servers = 0.06 MB/s per messaging server daily

## Peak

* 1 hour peak
* 80% of cell is busy = 80 000 000 active users
* 50% of user daily messages are sent = 32 messages for each user peak

### Messages

* 80 000 000 users * 32 messages = 2 560 000 000 messages peak
* 2 560 000 000 peak / 3600 seconds = ~712 000 messages/s peak
* 712 000 messages/s / 2000 messaging servers = ~356 messages/s per messaging server peak

### Throughput

* 2 560 000 000 messages * 256 bytes = 655 360 000 000 bytes = 640 000 000 KB = 625 000 MB peak
* 625 000 MB peak / 3 600 seconds = ~174 MB/s peak
* 174 MB/s / 2000 messaging servers = 0.09 MB/s per messaging server peak

## Storage

* 10 000 000 MB daily
* 3 650 000 000 MB yearly
* 14 600 000 000 MB for 4 years
