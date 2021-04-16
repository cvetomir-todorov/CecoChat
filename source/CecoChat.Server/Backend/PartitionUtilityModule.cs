﻿using Autofac;
using CecoChat.Autofac;

namespace CecoChat.Server.Backend
{
    public sealed class PartitionUtilityModule : Module
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
}