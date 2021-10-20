namespace CecoChat.Client.Console.Interaction
{
    public sealed class StateContainer
    {
        public StateContainer(MessagingClient client, MessageStorage storage)
        {
            Client = client;
            Storage = storage;
            Context = new StateContext();

            Users = new UsersState(this);
            FindUser = new FindUserState(this);
            Chat = new ChatState(this);
            SendMessage = new SendMessageState(this);
            React = new ReactState(this);
            Final = new FinalState(this);
        }

        public MessagingClient Client { get; }
        public MessageStorage Storage { get; }
        public StateContext Context { get; }

        public State Users { get; }
        public State FindUser { get; }
        public State Chat { get; }
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