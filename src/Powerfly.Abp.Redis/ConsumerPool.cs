﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Volo.Abp.DependencyInjection;

namespace Powerfly.Abp.Redis;

public class ConsumerPool : IConsumerPool, ISingletonDependency
{
    public IRedisSerializer Serializer { get; }

    protected AbpRedisOptions Options { get; }

    protected ConcurrentDictionary<string, Lazy<IConsumer<string, byte[]>>> Consumers { get; }

    protected TimeSpan TotalDisposeWaitDuration { get; set; } = TimeSpan.FromSeconds(10);

    public ILogger<ConsumerPool> Logger { get; set; }

    private bool _isDisposed;

    public ConsumerPool(IRedisSerializer serializer, IOptions<AbpRedisOptions> options)
    {
        Options = options.Value;

        Consumers = new ConcurrentDictionary<string, Lazy<IConsumer<string, byte[]>>>();
        Logger = new NullLogger<ConsumerPool>();
        Serializer = serializer;
    }

    public virtual IConsumer<string, byte[]> Get(string groupId, string? connectionName = null)
    {
        connectionName ??= RedisConnections.DefaultConnectionName;

        return Consumers.GetOrAdd(
            connectionName, connection => new Lazy<IConsumer<string, byte[]>>(() =>
            {
                var config = new ConsumerConfig(Options.Connections.GetOrDefault(connection))
                {
                    GroupName = groupId,
                };

                Options.ConfigureConsumer?.Invoke(config);

                var multiplexer = ConnectionMultiplexer.Connect(config.Configuration);
                var consumer = new Consumer<string, byte[]>(multiplexer.GetDatabase(), Serializer, config);

                return consumer;
            })
        ).Value;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (!Consumers.Any())
        {
            Logger.LogDebug($"Disposed consumer pool with no consumers in the pool.");
            return;
        }

        var poolDisposeStopwatch = Stopwatch.StartNew();

        Logger.LogInformation($"Disposing consumer pool ({Consumers.Count} consumers).");

        var remainingWaitDuration = TotalDisposeWaitDuration;

        foreach (var consumer in Consumers.Values)
        {
            var poolItemDisposeStopwatch = Stopwatch.StartNew();

            try
            {
                consumer.Value.Unsubscribe();
                consumer.Value.Close();
                consumer.Value.Dispose();
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
            $"Disposed Redis Consumer Pool ({Consumers.Count} consumers in {poolDisposeStopwatch.Elapsed.TotalMilliseconds:0.00} ms).");

        if (poolDisposeStopwatch.Elapsed.TotalSeconds > 5.0)
        {
            Logger.LogWarning(
                $"Disposing Redis Consumer Pool got time greather than expected: {poolDisposeStopwatch.Elapsed.TotalMilliseconds:0.00} ms.");
        }

        Consumers.Clear();
    }
}
