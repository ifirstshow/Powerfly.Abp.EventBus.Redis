using System;

namespace Powerfly.Abp.Redis;

public interface IRedisSerializer
{
    byte[] Serialize(object obj);

    object Deserialize(byte[] value, Type type);

    T Deserialize<T>(byte[] value);
}
