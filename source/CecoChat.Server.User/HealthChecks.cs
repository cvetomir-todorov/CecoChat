using CecoChat.Health;

namespace CecoChat.Server.User;

public class DynamicConfigInitHealthCheck : StatusHealthCheck
{ }

public class UserDbInitHealthCheck : StatusHealthCheck
{ }

public class ConfigChangesConsumerHealthCheck : StatusHealthCheck
{ }
