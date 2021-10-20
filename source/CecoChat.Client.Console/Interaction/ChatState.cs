using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CecoChat.Client.Console.Interaction
{
    public sealed class ChatState : State
    {
        public ChatState(StateContainer states) : base(states)
        {}

        public override async Task<State> Execute()
        {
            if (States.Context.ReloadData)
            {
                await GetDialogHistory(States.Context.UserID);
            }

            List<Message> messages = States.Storage.GetDialogMessages(States.Context.UserID);
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
                States.Context.ReloadData = false;
                return States.SendMessage;
            }
            else if (keyInfo.KeyChar == 'r' || keyInfo.KeyChar == 'R')
            {
                States.Context.ReloadData = false;
                return States.React;
            }
            else if (keyInfo.KeyChar == 'f' || keyInfo.KeyChar == 'F')
            {
                States.Context.ReloadData = true;
                return States.Chat;
            }
            else if (keyInfo.KeyChar == 'l' || keyInfo.KeyChar == 'L')
            {
                States.Context.ReloadData = false;
                return States.Chat;
            }
            else if (keyInfo.KeyChar == 'x' || keyInfo.KeyChar == 'X')
            {
                States.Context.ReloadData = true;
                return States.Users;
            }
            else
            {
                States.Context.ReloadData = false;
                return States.Chat;
            }
        }

        private void DisplayMessage(Message message)
        {
            string sender = message.SenderID == States.Client.UserID ? "You" : message.SenderID.ToString();
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

            System.Console.WriteLine("[{0:F}] {1}: {2} (#{3}|{4}|ID: {5} |{6} reaction(s):{7})",
                message.MessageID.ToTimestamp(), sender, message.Data,
                message.SequenceNumber, message.Status, message.MessageID,
                message.Reactions.Count, reactions);
        }
    }
}