using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Collections.Concurrent;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.Threading;

namespace Powerfly.Abp.Redis;

public class RedisMessageConsumer : IRedisMessageConsumer, ITransientDependency, IDisposable
{
    public ILogger<RedisMessageConsumer> Logger { get; set; }

    protected IConsumerPool ConsumerPool { get; }

    protected IProducerPool ProducerPool { get; }

    protected IExceptionNotifier ExceptionNotifier { get; }

    protected AbpRedisOptions Options { get; }

    protected AbpAsyncTimer Timer { get; }

    protected ConcurrentBag<Func<Message<string, byte[]>, Task>> Callbacks { get; }

    protected IConsumer<string, byte[]>? Consumer { get; private set; }

    protected string ConnectionName { get; private set; } = default!;

    protected string GroupName { get; private set; } = default!;

    protected string TopicName { get; private set; } = default!;

    private CancellationTokenSource? cancellationTokenSource;

    public RedisMessageConsumer(
        IConsumerPool consumerPool,
        IExceptionNotifier exceptionNotifier,
        IOptions<AbpRedisOptions> options,
        IProducerPool producerPool,
        AbpAsyncTimer timer)
    {
        ConsumerPool = consumerPool;
        ExceptionNotifier = exceptionNotifier;
        ProducerPool = producerPool;
        Timer = timer;
        Options = options.Value;
        Logger = NullLogger<RedisMessageConsumer>.Instance;

        Callbacks = new ConcurrentBag<Func<Message<string, byte[]>, Task>>();

        Timer.Period = 5000; //5 sec.
        Timer.Elapsed = Timer_Elapsed;
        Timer.RunOnStart = true;
    }

    public virtual void Initialize(
        [NotNull] string topicName,
        [NotNull] string groupName,
        string? connectionName = null)
    {
        Check.NotNull(topicName, nameof(topicName));
        Check.NotNull(groupName, nameof(groupName));
        TopicName = topicName;
        ConnectionName = connectionName ?? RedisConnections.DefaultConnectionName;
        GroupName = groupName;
        Timer.Start();
    }

    public virtual void OnMessageReceived(Func<Message<string, byte[]>, Task> callback)
    {
        Callbacks.Add(callback);
    }

    protected virtual async Task Timer_Elapsed(AbpAsyncTimer timer)
    {
        if (Consumer == null)
        {
            await CreateTopicAsync();
            cancellationTokenSource = new CancellationTokenSource();
            Consume(cancellationTokenSource.Token);
        }
    }

    protected virtual async Task CreateTopicAsync()
    {
        if (!string.IsNullOrWhiteSpace(GroupName))
        {
            var config = new ConsumerConfig(Options.Connections.GetOrDefault(ConnectionName))
            {
                GroupName = GroupName,
            };
            Options.ConfigureConsumer?.Invoke(config);

            using var multiplexer = await ConnectionMultiplexer.ConnectAsync(config.Configuration);
            var database = multiplexer.GetDatabase();

            try
            {
                await database.StreamCreateConsumerGroupAsync(TopicName, GroupName);
            }
            catch (RedisServerException ex)
            {
                if (ex.Message.Contains("BUSYGROUP"))
                {
                    return;
                }

                throw;
            }
        }
    }

    protected virtual void Consume(CancellationToken token)
    {
        Consumer = ConsumerPool.Get(GroupName, ConnectionName);
        Consumer.Subscribe(TopicName);

        Task.Factory.StartNew(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                if (Consumer == null)
                {
                    break;
                }
                try
                {
                    var consumeResult = await Consumer.ConsumeAsync();

                    if (consumeResult.IsPartitionEOF)
                    {
                        continue;
                    }

                    await HandleIncomingMessage(consumeResult);
                }
                catch (RedisServerException ex)
                {
                    Logger.LogException(ex, LogLevel.Warning);
                    await ExceptionNotifier.NotifyAsync(ex, logLevel: LogLevel.Warning);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, LogLevel.Error);
                    await ExceptionNotifier.NotifyAsync(ex, logLevel: LogLevel.Error);
                }
                finally
                {
                    await Task.Delay(5);
                }
            }
        }, TaskCreationOptions.LongRunning);
    }

    protected virtual async Task HandleIncomingMessage(ConsumeResult<string, byte[]> consumeResult)
    {
        try
        {
            foreach (var callback in Callbacks)
            {
                await callback(consumeResult.Message);
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            await ExceptionNotifier.NotifyAsync(ex);
        }
        finally
        {
            if (Consumer != null)
            {
                await Consumer.CommitAsync(consumeResult);
            }
        }
    }

    public virtual void Dispose()
    {
        Timer.Stop();
        if (Consumer == null)
        {
            return;
        }

        try
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }

            Consumer.Unsubscribe();
            Consumer.Close();
            Consumer.Dispose();
            Consumer = null;
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
