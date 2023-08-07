using CecoChat.Contracts;
using CecoChat.Contracts.User;

namespace CecoChat.Data.User;

public class AutoMapperProfile : AutoMapper.Profile
{
    public AutoMapperProfile()
    {
        CreateMap<ProfileEntity, ProfileFull>()
            .ForMember(
                profileContract => profileContract.Version,
                options => options.MapFrom(profileEntity => profileEntity.Version.ToUuid()));
        CreateMap<ProfileEntity, ProfilePublic>();
    }
}
