# Known issues

Defects found by reviewing the code against the architecture. Unlike `what-next.md`, which lists work
that is planned but not started, everything here is something the code currently gets wrong.

Items already tracked in `what-next.md` are not repeated here.

# High

## Cached profiles are matched to the wrong users

`CecoChat.User.Data/Entities/Profiles/ProfileCache.cs:72-95`,
`CecoChat.User.Data/Entities/Profiles/CachingProfileQueryRepo.cs:117-137`

`ProfileCache.GetMany` groups the requested keys by Redis hash slot to pipeline one request per slot,
then reassembles the results by concatenating the per-group responses:

```csharp
IEnumerable<IGrouping<int, RedisKey>> keyGroups = keys.GroupBy(key => _cache.Multiplexer.GetHashSlot(key));
...
foreach (Task<RedisValue[]> task in tasks)
{
    task.Result.CopyTo(values, index);
    index += task.Result.Length;
}
```

The result is a permutation of the input order, not the input order. `ProcessProfilesFromCache`
nevertheless indexes both arrays in lockstep and treats `cachedProfiles[i]` as the value for
`userIds[i]`.

`ProfileCache.CreateKey` produces `profiles:{userId}` with no Redis hash tag, so keys for different
users land in different slots and more than one group is essentially always formed.
`CachingConnectionQueryRepo` shows the author was aware of this - it deliberately uses the hash tag
form `connections:{{{userId}}}` to pin a user's keys to one slot - but the profile keys do not.

The visible effect appears as soon as any requested profile is a cache miss: the null check lands on
the wrong position, so `uncachedUserIds` collects the wrong IDs. Profiles that were cached get
re-fetched from the database and appended a second time, while profiles that were genuinely absent are
dropped from the result entirely. `GetPublicProfiles(IList<long>, long)` therefore returns duplicates
and omits requested users - visible on the all-chats screen, which resolves chat participants through
this path.

## Invalid dynamic configuration is applied anyway

`CecoChat.Config/ConfigSection.cs:109-116`

`NotifyConfigChange` logs the validation failure and then falls through to `Values = changedValues`
because there is no `return`. `Initialize` handles this correctly with `return false`, so validation
only ever gates startup - every hot reload of every section in every service skips it.

`PartitioningValidator` restricts `PartitionCount` to `[2, 10000]`, but a `0` published through the
Config service Swagger endpoints propagates to all subscribers and turns
`Math.Abs(hash) % partitionCount` in `CecoChat.Backplane/Partitioner.cs:23` into a
`DivideByZeroException` on every message, across the whole fleet.

## Lost updates on user chat state

`CecoChat.Chats.Service/Backplane/StateConsumer.cs:147,178`,
`CecoChat.Chats.Data/Entities/UserChats/UserChatsRepo.cs:156`

`design-chats.md` justifies the load-then-conditionally-write pattern instead of lightweight
transactions with: *"The operation doesn't suffer from concurrency issues since the Kafka subscribers
process each partition in a single thread and messages for each user go into the same partition."*
That does not hold. The row is keyed `(user_id, chat_id)` and two independent consumer groups write it:

* `state-receivers-consumer` writes row `(A, chatId(A,B))` from B->A messages, which live in partition(A)
* `state-senders-consumer` writes the same row from A->B messages, which live in partition(**B**)

Different consumer groups, different threads, possibly different service instances. Single-threaded
consumption per partition does not serialize them.

`UpdateUserChat` compounds it by issuing a full-row `INSERT` of all six columns, while
`UpdateSenderState` only computes `NewestMessage` - so it writes back the `other_user_delivered` and
`other_user_seen` values it read. Interleaved with the receiver path, delivered/seen silently regress.
The reverse interleaving regresses `newest_message`, which drives chat list ordering and the client
gap-detection backfill.

## A failing consumer handler drops the message permanently

`Common.Kafka/KafkaConsumer.cs:187-194`

When the message handler throws, the offset is not committed - but the surrounding loop calls
`Consume` again, fetches the *next* message, and commits *its* offset, moving the committed position
past the failed one. The message is never redelivered.

Neither `HistoryConsumer` nor `StateConsumer` catches anything, so a single transient Cassandra
timeout in `AddPlainTextMessage` permanently erases that message from chat history - the exact store
clients backfill from when they detect a counter gap (`research-reliable-messaging-consistency.md`).
The failure is logged as a generic "encountered a failing message handler" with no message identity.

# Medium

## `Math.Abs(int.MinValue)` throws in the partitioner

`CecoChat.Backplane/Partitioner.cs:23`

`FnvHash.Compute` runs `unchecked` and can return any `int`, including `int.MinValue`, for which
`Math.Abs` throws `OverflowException`. Because the hash is deterministic per user ID this is not a
transient one-in-four-billion event: an affected user would be permanently unable to send or receive,
and the BFF could not route them either. `(hash & 0x7FFFFFFF) % partitionCount` avoids it without
meaningfully changing the distribution.

## Snowflake generator IDs are captured once and never refreshed

`CecoChat.IdGen.Service/Endpoints/SnowflakeGenerator.cs`

The constructor calls `GetGeneratorIds` and builds the `IdGenerator` list, and the type is registered
`SingleInstance`. There is no subscriber for snowflake config changes anywhere - only
`PartitionsChangedEventArgs` has subscribers. Editing the snowflake section hot-reloads
`ISnowflakeConfig` but has no effect on the running generators.

`SnowflakeValidator` correctly rejects overlapping generator IDs in the desired state, but nothing
covers the transition. Reassign generator ID 5 from serverA to serverB and restart B: B starts using 5
while A, which never noticed the change, is still using it. Duplicate Snowflake IDs land in
`chat_messages`, whose primary key is `(chat_id, message_id)`, so one message silently overwrites the
other.

## `System.Random` shared across concurrent requests

`CecoChat.IdGen.Service/Endpoints/SnowflakeGenerator.cs:19,27,69`

`SnowflakeGenerator` is a singleton and `_random.Next()` is called from every concurrent
`GenerateOne`/`GenerateMany`. `System.Random` is not thread-safe; concurrent use corrupts its internal
state and can wedge it into returning `0` permanently, collapsing all traffic onto generator index 0
and defeating the multi-generator design. IDs stay unique, so it fails silently. `Random.Shared` fixes
it.

## Client container entries are never removed

`CecoChat.Messaging.Service/Clients/ClientContainer.cs:50-66`

`RemoveClient` decrements the per-user count but never calls `TryRemove`, so the dictionary
accumulates one entry per user who has *ever* connected to that instance. Besides the leak,
`EnumerateUsers` is walked on every partition rebalance and scans all-time users rather than connected
ones. `GetOrAdd(userId, new ClientContext())` also allocates on every connect even when the entry
already exists - the factory overload avoids it.

## Connection cache entries can end up without a TTL

`CecoChat.User.Data/Entities/Connections/CachingConnectionQueryRepo.cs:79-80`

`GetConnections` populates the cache with two separate round trips - `SetAddAsync` followed by
`KeyExpireAsync`. They are not atomic. If the process dies, the Redis connection drops, or the second
call fails between the two, the set exists with no expiry.

`design-users.md` accepts that connection changes do not invalidate the cache because writes are
infrequent, which makes the TTL the only invalidation mechanism. A key that loses its TTL therefore
serves that user a stale connection list permanently, until someone deletes the key by hand. Use a
transaction, or `SetAdd` plus `KeyExpire` in one batch.

## A shutdown race can crash the process

`Common/Threading/DedicatedThreadTaskScheduler.cs:29,40-44`

`Dispose` calls `_cts.Cancel()` and then immediately `_cts.Dispose()`, while the dedicated thread may
still be blocked in `_taskQueue.Take(_cts.Token)`. Disposing a `CancellationTokenSource` with an
active wait registration surfaces as `ObjectDisposedException` from `Take`, and the loop only catches
`OperationCanceledException`. The exception goes unhandled on a thread that is not a thread pool
thread, which terminates the process.

The scheduler also never calls `CompleteAdding`, never disposes `_taskQueue`, and never joins the
thread, so queued work is silently abandoned. `BackplaneComponents` owns one of these per messaging
instance and disposes it during shutdown.

## Partition reassignment can overlap with itself

`Common/Events/EventSource.cs:60`

`Publish` is fire and forget - `Task.Run(async () => await ProcessEvent(eventData))` discards the
task. Two partitioning config changes in quick succession start two overlapping `ProcessEvent` runs,
so `BackplaneComponentsInit.Handle` can run concurrently with itself. It reads
`_backplaneComponents.CurrentPartitionCount` and `CurrentPartitions` to decide which clients to
disconnect, and the other invocation may already have overwritten both. The outcome is clients that
should have been disconnected staying put, or the Kafka assignment no longer matching
`CurrentPartitions`.

The same overlap reaches `KafkaConsumer.Assign`, where `_assignedPartitions`
(`Common.Kafka/KafkaConsumer.cs:23,84-98`) is read and written with no synchronization, so the cached
value can end up disagreeing with what was actually assigned. Since live partition reassignment is the
documented operational lever, this path deserves to be serialized.

# Small

* `Common.Kafka/KafkaConsumer.cs:40` - `Dispose` calls `_consumer.Close()` but never `Dispose()`,
  leaking the native librdkafka handle.
* `CecoChat.Messaging.Service/Backplane/ReceiversConsumer.cs:95` and
  `SendersProducer.cs` (`NotifyDelivery`) attach `ContinueWith(..., OnlyOnFaulted)` to `NotifyInGroup`,
  which already catches and logs everything internally - the continuation can never run. Dead error
  handling that reads like a safety net.
* `CecoChat.Config/Backplane/ConfigChangesConsumer.cs:82` - the reload is fire-and-forget and the
  offset commits immediately, so a reload that fails (for example the config DB being unreachable) is
  never retried and that instance runs stale config indefinitely with only a log line.
* `CecoChat.Messaging.Service/ContractMapper.cs:111` - `GetReactionTargetUserId` throws
  `InvalidOperationException` rather than a `HubException` on a client-controlled condition
  (`SenderId`/`ReceiverId` come straight from the request), so a malformed React surfaces as an
  unhandled server error instead of a client error.
* `CecoChat.User.Service/Endpoints/Auth/AuthService.cs:73` - `Authenticate` returns `Missing` before
  doing any hashing. `SessionController` collapses `Missing` and `InvalidPassword` into a single 401,
  so the response shape does not leak, but the timing does: an unknown user answers immediately while
  a known one costs roughly a million PBKDF2-SHA512 iterations. The same asymmetry makes valid
  usernames a cheap CPU exhaustion target.
* `CecoChat.User.Data/Entities/Profiles/ProfileCache.cs:130-143` - a Redis failure inside
  `ProcessChannel` is caught by the outer handler in `StartProcessing`, which logs and lets that
  processor task exit for good. After enough transient failures every processor is gone, profile
  caching silently stops, and the service still reports healthy. The loop also uses the blocking
  `StringSet` rather than `StringSetAsync`.
* `CecoChat.User.Data/Entities/Profiles/CachingProfileQueryRepo.cs:160` - the profile search cache key
  `profile-search:{searchPattern}` does not include `profileCount`, which comes from the dynamic
  `UserConfig`. Raising that limit keeps returning the previously cached, shorter result list until
  the entry expires.
* `CecoChat.Bff.Service/Endpoints/Files/UploadFileController.cs:73-75` - the object is written to
  MinIO before `AssociateFile` runs, and nothing removes it when the association fails or the User
  service is unavailable. Since files are immutable and buckets are per day, the orphans have no
  cleanup path.
* `CecoChat.Bff.Service/Endpoints/Files/DownloadFileController.cs` - responses carry no
  `X-Content-Type-Options: nosniff` and no `Content-Disposition`, so allowed types such as `.pdf` and
  `.txt` render inline on the BFF origin. Related to, but not covered by, the file signature
  verification item in `what-next.md`.
* `deploy/minikube/messaging/templates/messaging-services.yml` - the per-pod Services are hardcoded to
  ordinals `-0` and `-1` while `StatefulSet.Replicas` is a chart value. Raising the replica count
  leaves the new pod without a Service, unreachable even though the partitioning config may route
  users to it. The same applies to the `idgen` chart.
* `design-users.md:50` states a 512KB upload limit and `CLAUDE.md` repeats it, but
  `CecoChat.Bff.Service/appsettings.json` sets `MaxUploadedFileBytes` to 10485760 (10MB).
* The `UserIds` count limit is enforced as `Length < userConfig.ProfileCount` in
  `CecoChat.Bff.Service/Endpoints/Profiles/ProfileValidation.cs` but as `Count <= ProfileCount` in
  `CecoChat.User.Service/Endpoints/Profiles/ProfileQueryValidation.cs` - off by one between the two
  layers.
* `Common.AspNet/MultipartUtility.cs` - `IsMultipartContentType` uses `Contains("multipart/")` rather
  than a prefix check, so a content type that merely mentions the token passes the first gate.
* `CecoChat.Messaging.Service/Backplane/SendersProducer.cs:58` - `PartitionCount` is a plain
  auto-property written by the config-change thread and read by request threads with no memory
  barrier.
