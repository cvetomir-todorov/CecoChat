using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Client.Shared;

namespace CecoChat.Client.Console.Interaction
{
    public sealed class UsersState : State
    {
        public UsersState(StateContainer states, MessagingClient client, MessageStorage storage) : base(states, client, storage)
        {}

        public override async Task<State> Execute(StateContext context)
        {
            if (context.ReloadData)
            {
                await GetUserHistory();
            }

            System.Console.Clear();
            System.Console.WriteLine("Choose user to chat (press '0'...'9') | Refresh (press 'r') | Exit (press 'x'):");
            List<long> userIDs = Storage.GetUsers();
            int key = 0;
            foreach (long userID in userIDs)
            {
                System.Console.WriteLine("ID={0} (press '{1}')", userID, key++);
            }

            ConsoleKeyInfo keyInfo = System.Console.ReadKey(intercept: true);
            if (char.IsNumber(keyInfo.KeyChar))
            {
                return ProcessNumberKey(keyInfo, userIDs, context);
            }
            else if (keyInfo.KeyChar == 'r' || keyInfo.KeyChar == 'R')
            {
                context.ReloadData = true;
                return States.Users;
            }
            else if (keyInfo.KeyChar == 'x' || keyInfo.KeyChar == 'X')
            {
                return States.Final;
            }
            else
            {
                return States.Users;
            }
        }

        private State ProcessNumberKey(ConsoleKeyInfo keyInfo, List<long> userIDs, StateContext context)
        {
            int index = keyInfo.KeyChar - '0';
            if (index < 0 || index >= userIDs.Count)
            {
                context.ReloadData = false;
                return States.Users;
            }
            else
            {
                context.UserID = userIDs[index];
                context.ReloadData = true;
                return States.Dialog;
            }
        }
    }
}