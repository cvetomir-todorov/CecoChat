using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CecoChat.Data.Config.Snowflake;

public interface ISnowflakeConfig : IDisposable
{
    Task Initialize();

    IReadOnlyCollection<short> GetGeneratorIDs(string server);
}