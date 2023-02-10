using CecoChat.Contracts.User;

namespace CecoChat.Data.User;

public class AutoMapperProfile : AutoMapper.Profile
{
    public AutoMapperProfile()
    {
        CreateMap<ProfileEntity, ProfileFull>();
        CreateMap<ProfileEntity, ProfilePublic>();
    }
}
