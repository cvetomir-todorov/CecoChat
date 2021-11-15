using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CecoChat.Client.Console.LocalStorage;

namespace CecoChat.Client.Console.Interaction
{
    public sealed class OneChatState : State
    {
        public OneChatState(StateContainer states) : base(states)
        {}

        public override async Task<State> Execute()
        {
            if (Context.ReloadData)
            {
                await GetHistory(Context.UserID);
            }

            List<Message> messages = Storage.GetChatMessages(Context.UserID);
            messages.Sort((left, right) => left.MessageID.CompareTo(right.MessageID));

            System.Console.Clear();
            foreach (Message message in messages)
            {
                DisplayMessage(message);
            }
            System.Console.WriteLine("Write (press 'w') | React (press 'r') | Refresh (press 'f') | Local refresh (press 'l') | Return (press 'x')");

            ConsoleKeyInfo keyInfo = System.Console.ReadKey(intercept: true);
            if (keyInfo.KeyChar == 'w' || keyInfo.KeyChar == 'W')
            {
                Context.ReloadData = false;
                return States.SendMessage;
            }
            else if (keyInfo.KeyChar == 'r' || keyInfo.KeyChar == 'R')
            {
                Context.ReloadData = false;
                return States.React;
            }
            else if (keyInfo.KeyChar == 'f' || keyInfo.KeyChar == 'F')
            {
                Context.ReloadData = true;
                return States.OneChat;
            }
            else if (keyInfo.KeyChar == 'x' || keyInfo.KeyChar == 'X')
            {
                Context.ReloadData = true;
                return States.AllChats;
            }
            else
            {
                // includes local refresh
                Context.ReloadData = false;
                return States.OneChat;
            }
        }

        private void DisplayMessage(Message message)
        {
            string sender = message.SenderID == Client.UserID ? "You" : message.SenderID.ToString();
            string reactions = string.Empty;
            if (message.Reactions.Count > 0)
            {
                StringBuilder reactionsBuilder = new();
                foreach (KeyValuePair<long,string> pair in message.Reactions)
                {
                    reactionsBuilder.AppendFormat(" {0}={1}", pair.Key, pair.Value);
                }

                reactions = reactionsBuilder.ToString();
            }

            System.Console.WriteLine("[{0:F}] {1}: {2} (#{3}|ID: {4} |{5} reaction(s):{6})",
                message.MessageID.ToTimestamp(), sender, message.Data,
                message.SequenceNumber, message.MessageID,
                message.Reactions.Count, reactions);
        }
    }
}