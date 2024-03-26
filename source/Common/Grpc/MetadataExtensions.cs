using Grpc.Core;

namespace CecoChat.Grpc;

public static class MetadataExtensions
{
    public static void AddAuthorization(this Metadata headers, string accessToken)
    {
        headers.Add("Authorization", $"Bearer {accessToken}");
    }
}
