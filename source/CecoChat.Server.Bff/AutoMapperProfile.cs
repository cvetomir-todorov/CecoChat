using Common;
using Google.Protobuf.WellKnownTypes;

namespace CecoChat.Server.Bff;

public class AutoMapperProfile : AutoMapper.Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Contracts.Bff.Auth.RegisterRequest, User.Contracts.Registration>();
        CreateMap<Contracts.Bff.Profiles.EditProfileRequest, User.Contracts.ProfileUpdate>()
            .ForMember(profileContract => profileContract.Version,
                options => options.MapFrom(request => request.Version.ToTimestamp()));
        CreateMap<User.Contracts.ProfileFull, Contracts.Bff.Auth.ProfileFull>()
            .ForMember(
                profileBff => profileBff.Version,
                options => options.MapFrom(profileContract => profileContract.Version.ToDateTime()));
        CreateMap<User.Contracts.ProfilePublic, Contracts.Bff.Profiles.ProfilePublic>();

        CreateMap<User.Contracts.Connection, Contracts.Bff.Connections.Connection>()
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
                            return Contracts.Bff.Connections.ConnectionStatus.NotConnected;
                        case User.Contracts.ConnectionStatus.Pending:
                            return Contracts.Bff.Connections.ConnectionStatus.Pending;
                        case User.Contracts.ConnectionStatus.Connected:
                            return Contracts.Bff.Connections.ConnectionStatus.Connected;
                        default:
                            throw new EnumValueNotSupportedException(connectionContract.Status);
                    }
                }));

        CreateMap<User.Contracts.FileRef, Contracts.Bff.Files.FileRef>()
            .ForMember(
                fileBff => fileBff.Version,
                options => options.MapFrom(fileContract => fileContract.Version.ToDateTime()));
    }
}
