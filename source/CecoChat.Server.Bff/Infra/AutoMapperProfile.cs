using CecoChat.Contracts;

namespace CecoChat.Server.Bff.Infra;

public class AutoMapperProfile : AutoMapper.Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Contracts.Bff.RegisterRequest, Contracts.User.ProfileCreate>();
        CreateMap<Contracts.Bff.ChangePasswordRequest, Contracts.User.ProfileChangePassword>()
            .ForMember(
                profileContract => profileContract.Version,
                options => options.MapFrom(request => request.Version.ToUuid()));
        CreateMap<Contracts.Bff.EditProfileRequest, Contracts.User.ProfileUpdate>()
            .ForMember(profileContract => profileContract.Version,
                options => options.MapFrom(request => request.Version.ToUuid()));
        CreateMap<Contracts.User.ProfileFull, Contracts.Bff.ProfileFull>()
            .ForMember(
                profileBff => profileBff.Version,
                options => options.MapFrom(profileContract => profileContract.Version.ToGuid()));
        CreateMap<Contracts.User.ProfilePublic, Contracts.Bff.ProfilePublic>();

        CreateMap<Contracts.User.Contact, Contracts.Bff.Contact>()
            .ForMember(
                contactBff => contactBff.Version,
                options => options.MapFrom(contactContract => contactContract.Version.ToGuid()))
            .ForMember(
                contactBff => contactBff.Status,
                options => options.MapFrom((contactContract, _) =>
                {
                    switch (contactContract.Status)
                    {
                        case Contracts.User.ContactStatus.PendingRequest:
                            return Contracts.Bff.ContactStatus.PendingRequest;
                        case Contracts.User.ContactStatus.CancelledRequest:
                            return Contracts.Bff.ContactStatus.CancelledRequest;
                        case Contracts.User.ContactStatus.Established:
                            return Contracts.Bff.ContactStatus.Established;
                        case Contracts.User.ContactStatus.Removed:
                            return Contracts.Bff.ContactStatus.Removed;
                        default:
                            throw new EnumValueNotSupportedException(contactContract.Status);
                    }
                }));
    }
}
