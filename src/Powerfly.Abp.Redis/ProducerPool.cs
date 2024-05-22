using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Volo.Abp.DependencyInjection;

namespace Powerfly.Abp.Redis;

public class ProducerPool : IProducerPool, ISingletonDependency
{
    protected IRedisSerializer Serializer { get; }

    protected AbpRedisOptions Options { get; }

    protected ConcurrentDictionary<string, Lazy<IProducer<string, byte[]>>> Producers { get; }

    protected TimeSpan TotalDisposeWaitDuration { get; set; } = TimeSpan.FromSeconds(10);

    protected TimeSpan DefaultTransactionsWaitDuration { get; set; } = TimeSpan.FromSeconds(30);

    public ILogger<ProducerPool> Logger { get; set; }

    private bool _isDisposed;

    public ProducerPool(IRedisSerializer serializer, IOptions<AbpRedisOptions> options)
    {
        Serializer = serializer;
        Options = options.Value;

        Producers = new ConcurrentDictionary<string, Lazy<IProducer<string, byte[]>>>();
        Logger = new NullLogger<ProducerPool>();
    }

    public virtual IProducer<string, byte[]> Get(string? connectionName = null)
    {
        connectionName ??= RedisConnections.DefaultConnectionName;

        return Producers.GetOrAdd(
            connectionName, connection => new Lazy<IProducer<string, byte[]>>(() =>
            {
                var config = new ProducerConfig(Options.Connections.GetOrDefault(connection))
                {
                };

                Options.ConfigureProducer?.Invoke(config);

                var multiplexer = ConnectionMultiplexer.Connect(config.Configuration);
                var producer = new Producer<string, byte[]>(multiplexer.GetDatabase(), Serializer, config);

                return producer;

            })).Value;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (!Producers.Any())
        {
            Logger.LogDebug($"Disposed producer pool with no producers in the pool.");
            return;
        }

        var poolDisposeStopwatch = Stopwatch.StartNew();

        Logger.LogInformation($"Disposing producer pool ({Producers.Count} producers).");

        var remainingWaitDuration = TotalDisposeWaitDuration;

        foreach (var producer in Producers.Values)
        {
            var poolItemDisposeStopwatch = Stopwatch.StartNew();

            try
            {
                producer.Value.Dispose();
            }
            catch
            {
            }

            poolItemDisposeStopwatch.Stop();

            remainingWaitDuration = remainingWaitDuration > poolItemDisposeStopwatch.Elapsed
                ? remainingWaitDuration.Subtract(poolItemDisposeStopwatch.Elapsed)
                : TimeSpan.Zero;
        }

        poolDisposeStopwatch.Stop();

        Logger.LogInformation(
            $"Disposed Redis Producer Pool ({Producers.Count} producers in {poolDisposeStopwatch.Elapsed.TotalMilliseconds:0.00} ms).");

        if (poolDisposeStopwatch.Elapsed.TotalSeconds > 5.0)
        {
            Logger.LogWarning(
                $"Disposing Redis Producer Pool got time greather than expected: {poolDisposeStopwatch.Elapsed.TotalMilliseconds:0.00} ms.");
        }

        Producers.Clear();
    }
}
