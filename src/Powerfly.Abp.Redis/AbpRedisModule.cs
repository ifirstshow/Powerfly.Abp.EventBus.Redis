using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Json;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace Powerfly.Abp.Redis;

[DependsOn(
    typeof(AbpJsonModule),
    typeof(AbpThreadingModule)
)]
public class AbpRedisModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<AbpRedisOptions>(configuration.GetSection("Redis"));
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        context.ServiceProvider
            .GetRequiredService<IConsumerPool>()
            .Dispose();

        context.ServiceProvider
            .GetRequiredService<IProducerPool>()
            .Dispose();
    }
}
