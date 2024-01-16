using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Powerfly.Abp.EventBus.Tests.EventData;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace Powerfly.Abp.EventBus.Tests.Client;

public class HelloWorldService : ITransientDependency
{
    public ILogger<HelloWorldService> Logger { get; set; }
    public IDistributedEventBus DistributedEventBus { get; }

    public HelloWorldService(IDistributedEventBus distributedEventBus)
    {
        Logger = NullLogger<HelloWorldService>.Instance;
        DistributedEventBus = distributedEventBus;
    }

    public async Task SayHelloAsync()
    {
        Logger.LogInformation("Hello World!");
        for (int i = 0; i < 1000; i++)
        {
            await DistributedEventBus.PublishAsync<SayHelloEto>(new SayHelloEto { Count = i + 1 });
        }
    }
}
