using CecoChat.Contracts.IdGen;
using Grpc.Core;

namespace CecoChat.Server.IDGen.Endpoints;

public sealed class IdGenService : Contracts.IdGen.IdGen.IdGenBase
{
    private readonly IIdentityGenerator _generator;

    public IdGenService(
        IIdentityGenerator generator)
    {
        _generator = generator;
    }

    public override Task<GenerateOneResponse> GenerateOne(GenerateOneRequest request, ServerCallContext context)
    {
        long id = _generator.GenerateOne(request.OriginatorId);
        return Task.FromResult(new GenerateOneResponse { Id = id });
    }

    public override Task<GenerateManyResponse> GenerateMany(GenerateManyRequest request, ServerCallContext context)
    {
        IEnumerable<long> ids = _generator.GenerateMany(request.OriginatorId, request.Count);
        GenerateManyResponse response = new();
        response.Ids.AddRange(ids);
        return Task.FromResult(response);
    }
}
