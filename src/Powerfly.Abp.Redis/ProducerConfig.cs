using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Powerfly.Abp.Redis
{
    public class ProducerConfig : ClientConfig
    {
        public int MaxLength { get; set; } = 1000;

        public ProducerConfig(ClientConfig config)
        {
            Configuration = config.Configuration;
            InstanceName = config.InstanceName;
        }
    }
}
