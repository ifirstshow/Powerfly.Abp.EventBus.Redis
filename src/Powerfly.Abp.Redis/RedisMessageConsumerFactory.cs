using System;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace Powerfly.Abp.Redis;

public class RedisMessageConsumerFactory : IRedisMessageConsumerFactory, ISingletonDependency, IDisposable
{
    protected IServiceScope ServiceScope { get; }

    public RedisMessageConsumerFactory(IServiceScopeFactory serviceScopeFactory)
    {
        ServiceScope = serviceScopeFactory.CreateScope();
    }

    public IRedisMessageConsumer Create(
        string topicName,
        string groupName,
        string? connectionName = null)
    {
        var consumer = ServiceScope.ServiceProvider.GetRequiredService<RedisMessageConsumer>();
        consumer.Initialize(topicName, groupName, connectionName);
        return consumer;
    }

    public void Dispose()
    {
        ServiceScope?.Dispose();
    }
}
