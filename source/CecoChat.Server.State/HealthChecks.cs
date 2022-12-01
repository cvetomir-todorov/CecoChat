using CecoChat.Health;

namespace CecoChat.Server.State;

public class StateDbInitHealthCheck : StatusHealthCheck
{ }

public class ReceiversConsumerHealthCheck : StatusHealthCheck
{ }

public class SendersConsumerHealthCheck : StatusHealthCheck
{ }
