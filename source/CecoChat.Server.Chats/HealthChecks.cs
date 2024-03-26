using Common.Health;

namespace CecoChat.Server.Chats;

public class ChatsDbInitHealthCheck : StatusHealthCheck
{ }

public class HistoryConsumerHealthCheck : StatusHealthCheck
{ }

public class ReceiversConsumerHealthCheck : StatusHealthCheck
{ }

public class SendersConsumerHealthCheck : StatusHealthCheck
{ }
