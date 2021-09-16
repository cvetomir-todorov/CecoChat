using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Client.Shared;

namespace CecoChat.Client.Console.Interaction
{
    public sealed class DialogState : State
    {
        public DialogState(StateContainer states, MessagingClient client, MessageStorage storage) : base(states, client, storage)
        {}

        public override async Task<State> Execute(StateContext context)
        {
            if (context.ReloadData)
            {
                await GetDialogHistory(context.UserID);
            }
            
            List<Message> messages = Storage.GetDialogMessages(context.UserID);
            messages.Sort((left, right) => left.Data.MessageId.CompareTo(right.Data.MessageId));

            System.Console.Clear();
            foreach (Message message in messages)
            {
                DisplayMessage(message);
            }
            System.Console.WriteLine("Write (press 'w') | Refresh (press 'r') | Return (press 'x')");

            ConsoleKeyInfo keyInfo = System.Console.ReadKey(intercept: true);
            if (keyInfo.KeyChar == 'w' || keyInfo.KeyChar == 'W')
            {
                return States.SendMessage;
            }
            else if (keyInfo.KeyChar == 'r' || keyInfo.KeyChar == 'R')
            {
                context.ReloadData = true;
                return States.Dialog;
            }
            else if (keyInfo.KeyChar == 'x' || keyInfo.KeyChar == 'X')
            {
                context.ReloadData = true;
                return States.Users;
            }
            else
            {
                return States.Dialog;
            }
        }

        private void DisplayMessage(Message message)
        {
            string sender = message.Data.SenderId == Client.UserID ? "You" : message.Data.SenderId.ToString();
            string ackStatus = string.IsNullOrWhiteSpace(message.AckStatus) ? string.Empty : $" {message.AckStatus}";

            System.Console.WriteLine("[{0:F} #{3}{4}] {1}: {2}",
                message.Data.MessageId.ToTimestamp(), sender, message.Data.Text, message.SequenceNumber, ackStatus);
        }
    }
}