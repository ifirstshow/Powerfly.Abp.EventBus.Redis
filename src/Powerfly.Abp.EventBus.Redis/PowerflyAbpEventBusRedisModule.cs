using Microsoft.Extensions.DependencyInjection;
using Powerfly.Abp.Redis;
using Volo.Abp;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace Powerfly.Abp.EventBus.Redis
{
    [DependsOn(
        typeof(AbpEventBusModule),
        typeof(PowerflyAbpRedisModule))]
    public class PowerflyAbpEventBusRedisModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();

            Configure<PowerflyAbpRedisEventBusOptions>(configuration.GetSection("Redis:EventBus"));
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            context
                .ServiceProvider
                .GetRequiredService<RedisDistributedEventBus>()
                .Initialize();
        }
    }
}
