using System.Collections.Generic;

namespace CecoChat.IDGen.Server.Generation
{
    public interface ISnowflakeOptions
    {
        List<short> GeneratorIDs { get; }
    }

    public sealed class SnowflakeOptions : ISnowflakeOptions
    {
        public List<short> GeneratorIDs { get; set; }
    }
}
