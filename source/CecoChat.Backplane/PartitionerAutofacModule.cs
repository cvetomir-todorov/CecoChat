using Autofac;
using Common;
using Common.Autofac;

namespace CecoChat.Backplane;

public sealed class PartitionerAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        string hashName = $"{nameof(Partitioner)}.{nameof(INonCryptoHash)}";

        builder.RegisterType<Partitioner>().As<IPartitioner>()
            .WithNamedParameter(typeof(INonCryptoHash), hashName)
            .SingleInstance();
        builder.RegisterType<FnvHash>().Named<INonCryptoHash>(hashName).SingleInstance();
    }
}
