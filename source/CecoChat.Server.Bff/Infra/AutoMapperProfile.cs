namespace CecoChat.Server.Bff.Infra;

public class AutoMapperProfile : AutoMapper.Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Contracts.User.ProfileFull, Contracts.Bff.ProfileFull>();
        CreateMap<Contracts.User.ProfilePublic, Contracts.Bff.ProfilePublic>();
    }
}
