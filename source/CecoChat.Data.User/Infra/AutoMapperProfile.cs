using CecoChat.Contracts.User;
using Google.Protobuf.WellKnownTypes;

namespace CecoChat.Data.User.Infra;

public class AutoMapperProfile : AutoMapper.Profile
{
    public AutoMapperProfile()
    {
        CreateMap<ProfileEntity, ProfileFull>()
            .ForMember(
                profileContract => profileContract.Version,
                options => options.MapFrom(profileEntity => profileEntity.Version.ToTimestamp()));
        CreateMap<ProfileEntity, ProfilePublic>();
    }
}
