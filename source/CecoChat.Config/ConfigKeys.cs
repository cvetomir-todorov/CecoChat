namespace CecoChat.Config;

public static class ConfigKeys
{
    public static class History
    {
        public static readonly string Section = "history";
        public static readonly string MessageCount = "history.message-count";
    }

    public static class Partitioning
    {
        public static readonly string Section = "partitioning";
        public static readonly string Count = "partitioning.count";
        public static readonly string Partitions = "partitioning.partitions";
        public static readonly string Addresses = "partitioning.addresses";
    }

    public static class Snowflake
    {
        public static readonly string Section = "snowflake";
        public static readonly string GeneratorIds = "snowflake.generator-ids";
    }

    public static class User
    {
        public static readonly string Section = "user";
        public static readonly string ProfileCount = "user.profile-count";
    }
}
