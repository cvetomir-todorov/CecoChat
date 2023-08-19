using CecoChat.Contracts;

namespace CecoChat.Server.Bff.Infra;

public class AutoMapperProfile : AutoMapper.Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Contracts.Bff.Auth.RegisterRequest, Contracts.User.Registration>();
        CreateMap<Contracts.Bff.Profiles.EditProfileRequest, Contracts.User.ProfileUpdate>()
            .ForMember(profileContract => profileContract.Version,
                options => options.MapFrom(request => request.Version.ToUuid()));
        CreateMap<Contracts.User.ProfileFull, Contracts.Bff.Auth.ProfileFull>()
            .ForMember(
                profileBff => profileBff.Version,
                options => options.MapFrom(profileContract => profileContract.Version.ToGuid()));
        CreateMap<Contracts.User.ProfilePublic, Contracts.Bff.Profiles.ProfilePublic>();

        CreateMap<Contracts.User.Connection, Contracts.Bff.Connections.Connection>()
            .ForMember(
                connectionBff => connectionBff.Version,
                options => options.MapFrom(connectionContract => connectionContract.Version.ToGuid()))
            .ForMember(
                connectionBff => connectionBff.Status,
                options => options.MapFrom((connectionContract, _) =>
                {
                    switch (connectionContract.Status)
                    {
                        case Contracts.User.ConnectionStatus.Pending:
                            return Contracts.Bff.Connections.ConnectionStatus.Pending;
                        case Contracts.User.ConnectionStatus.Connected:
                            return Contracts.Bff.Connections.ConnectionStatus.Connected;
                        default:
                            throw new EnumValueNotSupportedException(connectionContract.Status);
                    }
                }));
    }
}
