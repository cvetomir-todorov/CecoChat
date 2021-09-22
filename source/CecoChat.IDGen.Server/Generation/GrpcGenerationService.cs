using System.Collections.Generic;
using System.Threading.Tasks;
using CecoChat.Contracts.Identity;
using Grpc.Core;

namespace CecoChat.IDGen.Server.Generation
{
    public sealed class GrpcGenerationService : Contracts.Identity.Identity.IdentityBase
    {
        private readonly IIdentityGenerator _generator;

        public GrpcGenerationService(
            IIdentityGenerator generator)
        {
            _generator = generator;
        }

        public override Task<GenerateOneResponse> GenerateOne(GenerateOneRequest request, ServerCallContext context)
        {
            long id = _generator.GenerateOne(request.OriginatorId);
            return Task.FromResult(new GenerateOneResponse {Id = id});
        }

        public override Task<GenerateManyResponse> GenerateMany(GenerateManyRequest request, ServerCallContext context)
        {
            IEnumerable<long> ids = _generator.GenerateMany(request.OriginatorId, request.Count);
            GenerateManyResponse response = new();
            response.Ids.AddRange(ids);
            return Task.FromResult(response);
        }
    }
}
