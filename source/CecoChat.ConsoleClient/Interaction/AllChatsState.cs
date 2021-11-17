using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.ConsoleClient.LocalStorage;

namespace CecoChat.ConsoleClient.Interaction
{
    public sealed class AllChatsState : State
    {
        private DateTime _lastKnownChatState;

        public AllChatsState(StateContainer states) : base(states)
        {
            _lastKnownChatState = Snowflake.Epoch;
        }

        public override async Task<State> Execute()
        {
            if (Context.ReloadData)
            {
                await GetChats();
            }

            Console.Clear();
            Console.WriteLine("Choose user to chat (press '0'...'9') | New (press 'n') | Refresh (press 'f') | Exit (press 'x'):");
            List<long> userIDs = Storage.GetUsers();
            int key = 0;
            foreach (long userID in userIDs)
            {
                Console.WriteLine("ID={0} (press '{1}')", userID, key++);
            }

            ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
            if (char.IsNumber(keyInfo.KeyChar))
            {
                return ProcessNumberKey(keyInfo, userIDs);
            }
            else if (keyInfo.KeyChar == 'n' || keyInfo.KeyChar == 'N')
            {
                Context.ReloadData = false;
                return States.FindUser;
            }
            else if (keyInfo.KeyChar == 'f' || keyInfo.KeyChar == 'F')
            {
                Context.ReloadData = true;
                return States.AllChats;
            }
            else if (keyInfo.KeyChar == 'x' || keyInfo.KeyChar == 'X')
            {
                return States.Final;
            }
            else
            {
                Context.ReloadData = false;
                return States.AllChats;
            }
        }

        private async Task GetChats()
        {
            DateTime currentState = DateTime.UtcNow;
            IList<Chat> chats = await Client.GetChats(_lastKnownChatState);

            foreach (Chat chat in chats)
            {
                Storage.AddOrUpdateChat(chat);
            }

            _lastKnownChatState = currentState;
        }

        private State ProcessNumberKey(ConsoleKeyInfo keyInfo, List<long> userIDs)
        {
            int index = keyInfo.KeyChar - '0';
            if (index < 0 || index >= userIDs.Count)
            {
                Context.ReloadData = false;
                return States.AllChats;
            }
            else
            {
                Context.UserID = userIDs[index];
                Context.ReloadData = true;
                return States.OneChat;
            }
        }
    }
}