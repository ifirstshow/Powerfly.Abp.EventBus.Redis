# Powerfly.Abp.EventBus.Redis

Similar to Volo.Abp.EventBus.Kafka, but it's use redis stream.

Thanks to [BenLocal/Rebus.Redis.Transport](https://github.com/BenLocal/Rebus.Redis.Transport), I used some of his code in my project.

> This document explains **how to configure the [redis](https://redis.io/)** as the distributed event bus provider. See the [distributed event bus document](https://docs.abp.io/en/abp/latest/Distributed-Event-Bus) to learn how to use the distributed event bus system

## Installation

Use the ABP CLI to add [Powerfly.Abp.EventBus.Redis](https://www.nuget.org/packages/Powerfly.Abp.EventBus.Redis) NuGet package to your project:

* Install the [ABP CLI](https://docs.abp.io/en/abp/latest/CLI) if you haven't installed before.
* Open a command line (terminal) in the directory of the `.csproj` file you want to add the `Powerfly.Abp.EventBus.Redis` package.
* Run `abp add-package Powerfly.Abp.EventBus.Redis` command.

If you want to do it manually, install the [Powerfly.Abp.EventBus.Redis](https://www.nuget.org/packages/Powerfly.Abp.EventBus.Redis) NuGet package to your project and add `[DependsOn(typeof(PowerflyAbpEventBusRedisModule))]` to the [ABP module](https://docs.abp.io/en/abp/latest/Module-Development-Basics) class inside your project.

## Configuration

You can configure using the standard [configuration system](https://docs.abp.io/en/abp/latest/Configuration), like using the `appsettings.json` file, or using the [options](https://docs.abp.io/en/abp/latest/Options) classes.

### `appsettings.json` file configuration

This is the simplest way to configure the Redis settings. It is also very strong since you can use any other configuration source (like environment variables) that is [supported by the AspNet Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/).

**Example: The minimal configuration to connect to a local redis server with default configurations**

````json
{
  "Redis": {
    "Connections": {
      "Default": {
        "Configuration": "localhost:6379"
      }
    },
    "EventBus": {
      "GroupName": "MyGroupName",
      "TopicName": "MyTopicName"
    }
  }
}
````

* `MyGroupName` is the name of this application, which is used as the **GroupName** on the Redis.
* `MyTopicName` is the **topic name**.

See [the Redis document](https://stackexchange.github.io/StackExchange.Redis/Configuration) to understand these options better.

#### Connections

If you need to connect to another server than the localhost, you need to configure the connection properties.

**Example: Specify the host name (as an IP address)**

````json
{
  "Redis": {
    "Connections": {
      "Default": {
        "Configuration": "123.123.123.123:6379"
      }
    },
    "EventBus": {
      "GroupName": "MyGroupName",
      "TopicName": "MyTopicName"
    }
  }
}
````

Defining multiple connections is allowed. In this case, you can specify the connection that is used for the event bus.

**Example: Declare two connections and use one of them for the event bus**

````json
{
  "Redis": {
    "Connections": {
      "Default": {
        "Configuration": "123.123.123.123:6379"
      },
      "SecondConnection": {
        "Configuration": "321.321.321.321:6379"
      }
    },
    "EventBus": {
      "GroupName": "MyGroupName",
      "TopicName": "MyTopicName",
      "ConnectionName": "SecondConnection"
    }
  }
}
````

This allows you to use multiple Redis cluster in your application, but select one of them for the event bus.

### The Options Classes

`PowerflyAbpRedisOptions` and `PowerflyAbpRedisEventBusOptions` classes can be used to configure the connection strings and event bus options for the Redis.

You can configure this options inside the `ConfigureServices` of your [module](https://docs.abp.io/en/abp/latest/Module-Development-Basics).

**Example: Configure the connection**

````csharp
Configure<PowerflyAbpRedisOptions>(options =>
{
    options.Connections.Default.Configuration = "123.123.123.123:6379,defaultDatabase=1,password=qwed8s7w5a9t63";
});
````

**Example: Configure the consumer config**

````csharp
Configure<PowerflyAbpRedisOptions>(options =>
{
    options.ConfigureConsumer = config =>
    {
        config.GroupName = "MyGroupName";
        config.ProcessingTimeout = TimeSpan.FromSeconds(60);
    };
});
````

**Example: Configure the producer config**

````csharp
Configure<PowerflyAbpRedisOptions>(options =>
{
    options.ConfigureProducer = config =>
    {
        config.MaxLength = 1000;
    };
});
````

Using these options classes can be combined with the `appsettings.json` way. Configuring an option property in the code overrides the value in the configuration file.
