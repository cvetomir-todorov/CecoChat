using System;
using System.Threading.Tasks;
using Refit;

namespace CecoChat.Contracts.Bff
{
    public interface IBffClient : IDisposable
    {
        [Post("/api/connect")]
        Task<ConnectResponse> Connect([Body] ConnectRequest request);

        [Get("/api/state/chats")]
        Task<GetChatsResponse> GetStateChats([Query] GetChatsRequest request, [Authorize("Bearer")] string accessToken);

        [Get("/api/history/messages")]
        Task<GetHistoryResponse> GetHistoryMessages([Query] GetHistoryRequest request, [Authorize("Bearer")] string accessToken);
    }
}