using CecoChat.Health;

namespace CecoChat.Server.Messaging;

public class DynamicConfigInitHealthCheck : StatusHealthCheck
{ }

public class ConfigChangesConsumerHealthCheck : StatusHealthCheck
{ }

public class ReceiversConsumerHealthCheck : StatusHealthCheck
{ }
