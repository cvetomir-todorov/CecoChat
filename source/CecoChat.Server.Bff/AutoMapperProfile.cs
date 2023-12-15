using Google.Protobuf.WellKnownTypes;

namespace CecoChat.Server.Bff;

public class AutoMapperProfile : AutoMapper.Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Contracts.Bff.Auth.RegisterRequest, Contracts.User.Registration>();
        CreateMap<Contracts.Bff.Profiles.EditProfileRequest, Contracts.User.ProfileUpdate>()
            .ForMember(profileContract => profileContract.Version,
                options => options.MapFrom(request => request.Version.ToTimestamp()));
        CreateMap<Contracts.User.ProfileFull, Contracts.Bff.Auth.ProfileFull>()
            .ForMember(
                profileBff => profileBff.Version,
                options => options.MapFrom(profileContract => profileContract.Version.ToDateTime()));
        CreateMap<Contracts.User.ProfilePublic, Contracts.Bff.Profiles.ProfilePublic>();

        CreateMap<Contracts.User.Connection, Contracts.Bff.Connections.Connection>()
            .ForMember(
                connectionBff => connectionBff.Version,
                options => options.MapFrom(connectionContract => connectionContract.Version.ToDateTime()))
            .ForMember(
                connectionBff => connectionBff.Status,
                options => options.MapFrom((connectionContract, _) =>
                {
                    switch (connectionContract.Status)
                    {
                        case Contracts.User.ConnectionStatus.NotConnected:
                            return Contracts.Bff.Connections.ConnectionStatus.NotConnected;
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
