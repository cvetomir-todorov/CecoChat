using Common;
using Google.Protobuf.WellKnownTypes;

namespace CecoChat.Server.Bff;

public class AutoMapperProfile : AutoMapper.Profile
{
    public AutoMapperProfile()
    {
        CreateMap<CecoChat.Bff.Contracts.Auth.RegisterRequest, User.Contracts.Registration>();
        CreateMap<CecoChat.Bff.Contracts.Profiles.EditProfileRequest, User.Contracts.ProfileUpdate>()
            .ForMember(profileContract => profileContract.Version,
                options => options.MapFrom(request => request.Version.ToTimestamp()));
        CreateMap<User.Contracts.ProfileFull, CecoChat.Bff.Contracts.Auth.ProfileFull>()
            .ForMember(
                profileBff => profileBff.Version,
                options => options.MapFrom(profileContract => profileContract.Version.ToDateTime()));
        CreateMap<User.Contracts.ProfilePublic, CecoChat.Bff.Contracts.Profiles.ProfilePublic>();

        CreateMap<User.Contracts.Connection, CecoChat.Bff.Contracts.Connections.Connection>()
            .ForMember(
                connectionBff => connectionBff.Version,
                options => options.MapFrom(connectionContract => connectionContract.Version.ToDateTime()))
            .ForMember(
                connectionBff => connectionBff.Status,
                options => options.MapFrom((connectionContract, _) =>
                {
                    switch (connectionContract.Status)
                    {
                        case User.Contracts.ConnectionStatus.NotConnected:
                            return CecoChat.Bff.Contracts.Connections.ConnectionStatus.NotConnected;
                        case User.Contracts.ConnectionStatus.Pending:
                            return CecoChat.Bff.Contracts.Connections.ConnectionStatus.Pending;
                        case User.Contracts.ConnectionStatus.Connected:
                            return CecoChat.Bff.Contracts.Connections.ConnectionStatus.Connected;
                        default:
                            throw new EnumValueNotSupportedException(connectionContract.Status);
                    }
                }));

        CreateMap<User.Contracts.FileRef, CecoChat.Bff.Contracts.Files.FileRef>()
            .ForMember(
                fileBff => fileBff.Version,
                options => options.MapFrom(fileContract => fileContract.Version.ToDateTime()));
    }
}
