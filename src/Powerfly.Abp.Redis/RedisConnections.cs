using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using StackExchange.Redis;
using Volo.Abp;

namespace Powerfly.Abp.Redis;

[Serializable]
public class RedisConnections : Dictionary<string, ClientConfig>
{
    public const string DefaultConnectionName = "Default";

    [NotNull]
    public ClientConfig Default {
        get => this[DefaultConnectionName];
        set => this[DefaultConnectionName] = Check.NotNull(value, nameof(value));
    }

    public RedisConnections()
    {
        Default = new ClientConfig();
    }

    public ClientConfig GetOrDefault(string? connectionName)
    {
        connectionName ??= DefaultConnectionName;
        
        if (TryGetValue(connectionName, out var connectionFactory))
        {
            return connectionFactory;
        }

        return Default;
    }
}
