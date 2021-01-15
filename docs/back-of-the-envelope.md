# Cell limits

* Infrastructure
  - 10 000 connections per front-facing server (FFS)
  - 1 000 FFS instances 
* Time
  - 1 day = 24*60*60 seconds = 86 400 seconds
  - 1 hour = 60*60 seconds = 3 600 seconds
* Other
  - 10 000 000 max users at the same time
  - 128 bytes per message
  - 4 years of message storage

## Daily

* Users come and go throughout the day

### Users

* 64 000 000 active users daily

### Messages

* 64 messages per user daily
* 64 000 000 users = 4 096 000 000 messages daily

### Throughput

* 524 288 000 000 bytes = 512 000 000 KB = 500 000 MB daily
* 500 000 MB daily / 86 400 seconds  = ~5.8 MB/s daily

## Peak

* 1 hour
* 80% of cell is busy
* 50% of user daily messages are sent

### Users

* 8 000 000 active users

### Messages

* 32 messages for each user peak
* 8 000 000 * 32 = 256 000 000 messages peak

### Throughput

* 32 768 000 000 bytes = 32 000 000 KB = 31 250 MB
* 31 250 MB peak / 3 600 seconds = ~8.7 MB/s peak

## Storage

* 500 000 MB daily
* 182 500 000 MB yearly
* 730 000 000 MB for 4 years
