using Refit;

namespace CecoChat.Contracts.Admin;

public interface IAdminClient : IDisposable
{
    [Get("/api/config")]
    Task<GetConfigResponse> GetConfig(
        [Query] GetConfigRequest request);

    [Put("/api/config")]
    Task<IApiResponse> UpdateConfigElements(
        [Body] UpdateConfigElementsRequest request);
}
