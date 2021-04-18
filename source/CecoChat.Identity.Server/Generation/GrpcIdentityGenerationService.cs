using System.Threading.Tasks;
using CecoChat.Contracts.Identity;
using Grpc.Core;

namespace CecoChat.Identity.Server.Generation
{
    public sealed class GrpcIdentityGenerationService : Contracts.Identity.Identity.IdentityBase
    {
        private readonly IIdentityGenerator _generator;

        public GrpcIdentityGenerationService(
            IIdentityGenerator generator)
        {
            _generator = generator;
        }

        public override Task<GenerateIdentityResponse> GenerateIdentity(GenerateIdentityRequest request, ServerCallContext context)
        {
            long id = _generator.Generate(request.OriginatorId);
            return Task.FromResult(new GenerateIdentityResponse {Id = id});
        }
    }
}
