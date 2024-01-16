using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Powerfly.Abp.Redis
{
    public class ClientConfig
    {
        public string Configuration { get; set; } = default!;
        public string? InstanceName { get; set; }
    }
}
