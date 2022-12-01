using CecoChat.Health;

namespace CecoChat.Server.History;

public class ConfigDbInitHealthCheck : StatusHealthCheck
{ }

public class HistoryDbInitHealthCheck : StatusHealthCheck
{ }

public class HistoryConsumerHealthCheck : StatusHealthCheck
{ }
