using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CecoChat.Client.Console.Interaction
{
    public sealed class UsersState : State
    {
        public UsersState(StateContainer states) : base(states)
        {}

        public override async Task<State> Execute()
        {
            if (States.Context.ReloadData)
            {
                await GetUserHistory();
            }

            System.Console.Clear();
            System.Console.WriteLine("Choose user to chat (press '0'...'9') | New (press 'n') | Refresh (press 'r') | Exit (press 'x'):");
            List<long> userIDs = States.Storage.GetUsers();
            int key = 0;
            foreach (long userID in userIDs)
            {
                System.Console.WriteLine("ID={0} (press '{1}')", userID, key++);
            }

            ConsoleKeyInfo keyInfo = System.Console.ReadKey(intercept: true);
            if (char.IsNumber(keyInfo.KeyChar))
            {
                return ProcessNumberKey(keyInfo, userIDs);
            }
            else if (keyInfo.KeyChar == 'n' || keyInfo.KeyChar == 'N')
            {
                States.Context.ReloadData = false;
                return States.FindUser;
            }
            else if (keyInfo.KeyChar == 'r' || keyInfo.KeyChar == 'R')
            {
                States.Context.ReloadData = true;
                return States.Users;
            }
            else if (keyInfo.KeyChar == 'x' || keyInfo.KeyChar == 'X')
            {
                return States.Final;
            }
            else
            {
                States.Context.ReloadData = false;
                return States.Users;
            }
        }

        private State ProcessNumberKey(ConsoleKeyInfo keyInfo, List<long> userIDs)
        {
            int index = keyInfo.KeyChar - '0';
            if (index < 0 || index >= userIDs.Count)
            {
                States.Context.ReloadData = false;
                return States.Users;
            }
            else
            {
                States.Context.UserID = userIDs[index];
                States.Context.ReloadData = true;
                return States.Dialog;
            }
        }
    }
}