using Common.Health;

namespace CecoChat.Chats.Service;

public class ChatsDbInitHealthCheck : StatusHealthCheck
{ }

public class HistoryConsumerHealthCheck : StatusHealthCheck
{ }

public class ReceiversConsumerHealthCheck : StatusHealthCheck
{ }

public class SendersConsumerHealthCheck : StatusHealthCheck
{ }
