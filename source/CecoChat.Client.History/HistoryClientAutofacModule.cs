using Autofac;
using CecoChat.Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Client.History
{
    public sealed class HistoryClientAutofacModule : Module
    {
        public IConfiguration HistoryClientConfiguration { get; init; }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HistoryClient>().As<IHistoryClient>().SingleInstance();
            builder.RegisterOptions<HistoryOptions>(HistoryClientConfiguration);
        }
    }
}