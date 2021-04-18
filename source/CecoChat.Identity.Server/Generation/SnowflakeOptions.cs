using System;
using System.Collections.Generic;

namespace CecoChat.Identity.Server.Generation
{
    public interface ISnowflakeOptions
    {
        List<short> GeneratorIDs { get; }

        DateTime Epoch { get; }
    }

    public sealed class SnowflakeOptions : ISnowflakeOptions
    {
        public List<short> GeneratorIDs { get; set; }

        public DateTime Epoch { get; set; }
    }
}
