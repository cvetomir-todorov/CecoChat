using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CecoChat.Client.Console.Interaction
{
    public sealed class DialogState : State
    {
        public DialogState(StateContainer states) : base(states)
        {}

        public override async Task<State> Execute()
        {
            if (States.Context.ReloadData)
            {
                await GetDialogHistory(States.Context.UserID);
            }
            
            List<Message> messages = States.Storage.GetDialogMessages(States.Context.UserID);
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
                States.Context.ReloadData = false;
                return States.SendMessage;
            }
            else if (keyInfo.KeyChar == 'r' || keyInfo.KeyChar == 'R')
            {
                States.Context.ReloadData = true;
                return States.Dialog;
            }
            else if (keyInfo.KeyChar == 'x' || keyInfo.KeyChar == 'X')
            {
                States.Context.ReloadData = true;
                return States.Users;
            }
            else
            {
                States.Context.ReloadData = false;
                return States.Dialog;
            }
        }

        private void DisplayMessage(Message message)
        {
            string sender = message.Data.SenderId == States.Client.UserID ? "You" : message.Data.SenderId.ToString();
            string ackStatus = string.IsNullOrWhiteSpace(message.AckStatus) ? string.Empty : $" {message.AckStatus}";

            System.Console.WriteLine("[{0:F} #{3}{4}] {1}: {2}",
                message.Data.MessageId.ToTimestamp(), sender, message.Data.Text, message.SequenceNumber, ackStatus);
        }
    }
}