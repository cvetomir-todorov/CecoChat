# Cell limits

* Infrastructure
  - 10 000 connections per front-facing server (FFS)
  - 10 000 FFS instances 
* Time
  - 1 day = 24*60*60 seconds = 86 400 seconds
  - 1 hour = 60*60 seconds = 3 600 seconds
* Other
  - 100 000 000 max users at the same time
  - 128 bytes per message
  - 4 years of message storage

## Daily

* Users come and go throughout the day

### Users

* 640 000 000 active users daily

### Messages

* 64 messages per user daily
* 640 000 000 users x 64 messages = 40 960 000 000 messages daily

### Throughput

* 5 242 880 000 000 bytes = 5 120 000 000 KB = 5 000 000 MB daily
* 5 000 000 MB daily / 86 400 seconds  = ~58 MB/s daily

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

* 327 680 000 000 bytes = 320 000 000 KB = 312 500 MB peak
* 312 500 MB peak / 3 600 seconds = ~87 MB/s peak

## Storage

* 5 000 000 MB daily
* 1 825 000 000 MB yearly
* 7 300 000 000 MB for 4 years
