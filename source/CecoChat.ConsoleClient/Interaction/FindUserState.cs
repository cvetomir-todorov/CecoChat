using System;
using System.Threading.Tasks;

namespace CecoChat.ConsoleClient.Interaction
{
    public sealed class FindUserState : State
    {
        public FindUserState(StateContainer states) : base(states)
        {}

        public override Task<State> Execute()
        {
            Console.Clear();
            Console.Write("Enter user ID ('0' to exit): ");

            string userIDString = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(userIDString) ||
                !long.TryParse(userIDString, out long userID) ||
                userID == 0)
            {
                Context.ReloadData = true;
                return Task.FromResult(States.AllChats);
            }
            else
            {
                Context.ReloadData = true;
                Context.UserID = userID;
                return Task.FromResult(States.OneChat);
            }
        }
    }
}