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

### Users

* 640 000 000 active users daily

### Messages

* 64 messages per user daily
* 640 000 000 users x 64 messages = 40 960 000 000 messages daily

### Throughput

* 10 485 760 000 000 bytes = 10 240 000 000 KB = 10 000 000 MB daily
* 10 000 000 MB daily / 86 400 seconds  = ~116 MB/s daily
* 116 MB/s / 2000 messaging servers = 0.06 MB/s per messaging server daily

## Peak

* 1 hour
* 80% of cell is busy
* 50% of user daily messages are sent

### Users

* 80 000 000 active users

### Messages

* 32 messages for each user peak
* 80 000 000 users * 32 messages = 2 560 000 000 messages peak

### Throughput

* 655 360 000 000 bytes = 640 000 000 KB = 625 000 MB peak
* 625 000 MB peak / 3 600 seconds = ~174 MB/s peak
* 174 MB/s / 2000 messagin servers = 0.09 MB/s per messaging server peak

## Storage

* 5 000 000 MB daily
* 1 825 000 000 MB yearly
* 7 300 000 000 MB for 4 years
