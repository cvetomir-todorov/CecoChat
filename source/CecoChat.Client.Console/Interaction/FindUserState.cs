using System.Threading.Tasks;

namespace CecoChat.Client.Console.Interaction
{
    public sealed class FindUserState : State
    {
        public FindUserState(StateContainer states) : base(states)
        {}

        public override Task<State> Execute()
        {
            System.Console.Clear();
            System.Console.Write("Enter user ID ('0' to exit): ");

            string userIDString = System.Console.ReadLine();
            if (string.IsNullOrWhiteSpace(userIDString) ||
                !long.TryParse(userIDString, out long userID) ||
                userID == 0)
            {
                States.Context.ReloadData = true;
                return Task.FromResult(States.Users);
            }
            else
            {
                States.Context.ReloadData = true;
                States.Context.UserID = userID;
                return Task.FromResult(States.Chat);
            }
        }
    }
}