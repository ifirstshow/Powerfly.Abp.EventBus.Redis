using Nito.AsyncEx;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;

namespace Powerfly.Abp.Redis
{
    public class Consumer<TKey, TValue> : IConsumer<TKey, TValue>
    {
        private bool _isDisposed;
        private readonly Channel<ConsumeResult<TKey, TValue>> receivedChannel;

        public Consumer(IDatabase database, IRedisSerializer serializer, ConsumerConfig config)
        {
            Database = database;
            Serializer = serializer;
            Config = config;
            ConsumerName = config.ConsumerName ?? Guid.NewGuid().ToString();

            TopicsDictionary = new ConcurrentDictionary<string, CancellationTokenSource>();

            receivedChannel = Channel.CreateUnbounded<ConsumeResult<TKey, TValue>>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        }

        public string ConsumerName { get; }
        public IDatabase Database { get; }
        public IRedisSerializer Serializer { get; }
        public ConsumerConfig Config { get; }
        public ConcurrentDictionary<string, CancellationTokenSource> TopicsDictionary { get; private set; }

        public async Task<ConsumeResult<TKey, TValue>> ConsumeAsync()
        {
            if (await receivedChannel.Reader.WaitToReadAsync())
            {
                if (receivedChannel.Reader.TryRead(out var result))
                {
                    return result;
                }
            }
            
            return ConsumeResult<TKey, TValue>.EOF;
        }

        public async Task CommitAsync(ConsumeResult<TKey, TValue> consumeResult)
        {
            if (consumeResult.Message.Headers.TryGetValue("redis-id", out var bytes))
            {
                var id = Encoding.UTF8.GetString(bytes);
                await Database.StreamAcknowledgeAsync(consumeResult.TopicName, Config.GroupName, id);
            }
        }

        public void Subscribe(string topicName)
        {
            var source = new CancellationTokenSource();
            if (TopicsDictionary.TryAdd(topicName, source))
            {
                ReclaimPendingMessagesLoop(topicName, source.Token);
                PollNewMessagesLoop(topicName, source.Token);
            }
        }

        private Task ReclaimPendingMessagesLoop(string topicName, CancellationToken token)
        {
            return Task.Factory.StartNew(async () =>
            {
                try
                {
                    do
                    {
                        var pendingMessages = await Database.StreamPendingMessagesAsync(
                            topicName,
                            Config.GroupName,
                            10,
                            Config.ConsumerName);

                        if (!pendingMessages.IsNullOrEmpty())
                        {
                            var ids = pendingMessages.Where(t => t.IdleTimeInMilliseconds >= Config.ProcessingTimeout.TotalMilliseconds)
                                .Where(t => t.MessageId != default)
                                .Select(t => t.MessageId)
                                .ToArray();

                            if (ids.Length > 0)
                            {
                                var streamEntries = await Database.StreamClaimAsync(topicName, Config.GroupName, ConsumerName, Config.QueueDepth, ids);
                                var messages = ToConsumeResults(streamEntries, topicName);

                                if (messages != null)
                                {
                                    await WriteAsync(messages);
                                }
                            }
                        }

                        if(Config.RedeliverInterval == TimeSpan.Zero ||  Config.ProcessingTimeout == TimeSpan.Zero)
                        {
                            return;
                        }

                        await Task.Delay(Config.RedeliverInterval);
                    }
                    while (!token.IsCancellationRequested);
                    
                }
                catch (OperationCanceledException)
                {
                }
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private Task PollNewMessagesLoop(string topicName, CancellationToken token)
        {
            return Task.Factory.StartNew(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        StreamEntry[] streamEntries;
                        if (string.IsNullOrWhiteSpace(Config.GroupName))
                        {
                            streamEntries = await Database.StreamReadAsync(
                                topicName,
                                StreamPosition.NewMessages,
                                Config.QueueDepth,
                                CommandFlags.None);
                        }
                        else
                        {
                            streamEntries = await Database.StreamReadGroupAsync(
                                topicName,
                                Config.GroupName,
                                ConsumerName,
                                StreamPosition.NewMessages,
                                Config.QueueDepth,
                                false,
                                CommandFlags.None);
                        }

                        var messages = ToConsumeResults(streamEntries, topicName);

                        await WriteAsync(messages);

                        if (Config.FetchInterval > TimeSpan.Zero)
                        {
                            await Task.Delay(Config.FetchInterval);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private IEnumerable<ConsumeResult<TKey, TValue>> ToConsumeResults(StreamEntry[] streamEntries, string topicName)
        {
            if (streamEntries.IsNullOrEmpty())
            {
                yield break;
            }

            foreach (var streamEntry in streamEntries)
            {
                if (streamEntry.IsNull)
                {
                    continue;
                }

                if (streamEntry.Values.IsNullOrEmpty())
                {
                    continue;
                }

                foreach (var item in streamEntry.Values)
                {
                    var messageType = item.Name.ToString();
                    if (messageType != "d")
                    {
                        throw new ArgumentException($"Redis stream entry with id {streamEntry.Id} data issues");
                    }

                    var str = item.Value.ToString();
                    if (str == null)
                    {
                        throw new ArgumentException($"Redis stream entry with message null data issues");
                    }

                    var message = Serializer.Deserialize<Message<TKey, TValue>>(Encoding.UTF8.GetBytes(str));
                    message.Headers.Add("redis-id", Encoding.UTF8.GetBytes(streamEntry.Id.ToString()));

                    var result = new ConsumeResult<TKey, TValue>(topicName, message);
                    yield return result;
                }
            }
        }

        private async Task WriteAsync(IEnumerable<ConsumeResult<TKey, TValue>> messages)
        {
            if (messages != null)
            {
                foreach (var message in messages)
                {
                    await receivedChannel.Writer.WriteAsync(message);
                }
            }
        }

        public void Unsubscribe()
        {
            while(TopicsDictionary.Count > 0)
            {
                var keys = TopicsDictionary.Keys.ToArray();
                foreach ( var key in keys)
                {
                    if (TopicsDictionary.TryRemove(key, out var source))
                    {
                        source.Cancel();
                        source.Dispose();
                    }
                }
            }
        }

        public void Close()
        {
            Database.Multiplexer.Close();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            Unsubscribe();
            Database.Multiplexer.Dispose();
        }
    }
}
