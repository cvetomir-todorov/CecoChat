using System;
using System.Runtime.Serialization;
using StackExchange.Redis;

namespace CecoChat.Data.Configuration
{
    [Serializable]
    public class ConfigurationException : Exception
    {
        public ConfigurationException(string missingKey)
            : base($"Configuration key '{missingKey}' was not found or value is empty.")
        {}

        public ConfigurationException(string key, RedisValue badlyFormattedValue)
            : base($"Configuration key '{key}' has badly formatted value '{badlyFormattedValue}'.")
        {}

        protected ConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
}
