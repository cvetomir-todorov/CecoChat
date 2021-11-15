using CecoChat.ConsoleClient.Api;
using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction
{
    public sealed class StateContainer
    {
        public StateContainer(MessagingClient client, MessageStorage storage)
        {
            Client = client;
            Storage = storage;
            Context = new StateContext();

            AllChats = new AllChatsState(this);
            FindUser = new FindUserState(this);
            OneChat = new OneChatState(this);
            SendMessage = new SendMessageState(this);
            React = new ReactState(this);
            Final = new FinalState(this);
        }

        public MessagingClient Client { get; }
        public MessageStorage Storage { get; }
        public StateContext Context { get; }

        public State AllChats { get; }
        public State FindUser { get; }
        public State OneChat { get; }
        public State SendMessage { get; }
        public State React { get; }
        public State Final { get; }
    }

    public sealed class StateContext
    {
        public bool ReloadData { get; set; }
        public long UserID { get; set; }
    }
}