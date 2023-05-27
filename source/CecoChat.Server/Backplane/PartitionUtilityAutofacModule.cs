using Autofac;
using CecoChat.Autofac;

namespace CecoChat.Server.Backplane;

public sealed class PartitionUtilityAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        string hashName = $"{nameof(PartitionUtility)}.{nameof(INonCryptoHash)}";

        builder.RegisterType<PartitionUtility>().As<IPartitionUtility>()
            .WithNamedParameter(typeof(INonCryptoHash), hashName)
            .SingleInstance();
        builder.RegisterType<FnvHash>().Named<INonCryptoHash>(hashName).SingleInstance();
    }
}
