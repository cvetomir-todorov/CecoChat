using Autofac;
using Autofac.Extensions.DependencyInjection;
using CecoChat.IdGen.Client;
using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CecoChat.IdGen.Testing;

public sealed class IdGenClient : IDisposable
{
    private readonly IIdGenClient _instance;
    private readonly IContainer _autofacServices;

    public IdGenClient(string configFilePath)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile(configFilePath, optional: false)
            .Build();
        IConfiguration idGenClientConfig = configuration.GetSection("IdGenClient");

        ServiceCollection services = new();
        IdGenClientOptions idGenClientOptions = new();
        idGenClientConfig.Bind(idGenClientOptions);
        services.AddIdGenClient(idGenClientOptions);
        services.AddSerilog(dispose: true);

        ContainerBuilder autofacBuilder = new();
        autofacBuilder.RegisterModule(new IdGenClientAutofacModule(idGenClientConfig));
        autofacBuilder.Populate(services);
        autofacBuilder.RegisterType<MonotonicClock>().As<IClock>().SingleInstance();
        _autofacServices = autofacBuilder.Build();

        IServiceProvider serviceProvider = new AutofacServiceProvider(_autofacServices);
        _instance = serviceProvider.GetRequiredService<IIdGenClient>();
    }

    public void Dispose()
    {
        _autofacServices.Dispose();
    }

    public IIdGenClient Instance => _instance;
}
