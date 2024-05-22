using System.Net;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using CecoChat.Config.Client;
using CecoChat.Config.Contracts;
using CecoChat.IdGen.Client;
using CecoChat.IdGen.Testing.Infra;
using CecoChat.Server;
using Common;
using Common.AspNet.Init;
using Common.Autofac;
using Common.Kafka;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Serilog;
using IdGenService = CecoChat.IdGen.Service.Program;

namespace CecoChat.IdGen.Testing.Tests;

public abstract class BaseTest
{
    protected WebApplication App { get; private set; }
    protected IIdGenClient Client { get; private set; }

    [OneTimeSetUp]
    public async Task BeforeAllTests()
    {
        App = CreateIdGenService();
        await RunIdGenService(App);

        Client = CreateIdGenClient();
    }

    private static WebApplication CreateIdGenService()
    {
        WebApplicationBuilder builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(IdGenService).Assembly.GetName().Name,
            EnvironmentName = "Test"
        });
        builder.Configuration.AddJsonFile("idgen-service-settings.json", optional: false);
        builder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, port: 32002, listenOptions =>
            {
                listenOptions.UseHttps("services.pfx", password: "cecochat");
            });
        });
        builder.Host.UseSerilog(dispose: true);

        CommonOptions options = new(builder.Configuration);

        IdGenService.AddServices(builder, options);
        IdGenService.AddHealth(builder, options);
        IdGenService.AddTelemetry(builder, options);
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        builder.Host.ConfigureContainer<ContainerBuilder>((host, autofacBuilder) =>
        {
            IdGenService.ConfigureContainer(host, autofacBuilder);
            
            // override registrations
            autofacBuilder.RegisterType<TestConfigClient>().As<IConfigClient>().SingleInstance();
            autofacBuilder.RegisterType<TestKafkaAdmin>().As<IKafkaAdmin>().SingleInstance();
            autofacBuilder.RegisterFactory<TestKafkaConsumer<Null, ConfigChange>, IKafkaConsumer<Null, ConfigChange>>();
        });

        WebApplication app = builder.Build();
        IdGenService.ConfigurePipeline(app, options);

        return app;
    }

    private static async Task RunIdGenService(WebApplication app)
    {
        bool initialized = await app.Services.Init();
        if (!initialized)
        {
            await TestContext.Progress.WriteLineAsync("Failed to initialize");
            return;
        }

        _ = app
            .RunAsync()
            .ContinueWith(task => TestContext.Progress.WriteLine($"Unexpected error occurred: {task.Exception}"), TaskContinuationOptions.OnlyOnFaulted)
            .ContinueWith(_ => TestContext.Progress.WriteLine("Ended successfully"), TaskContinuationOptions.NotOnFaulted);
    }

    private static IIdGenClient CreateIdGenClient()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("idgen-client-settings.json", optional: false)
            .Build();

        ServiceCollection services = new();
        IdGenClientOptions idGenClientOptions = new();
        configuration.GetSection("IdGenClient").Bind(idGenClientOptions);
        services.AddIdGenClient(idGenClientOptions);
        services.AddSerilog(dispose: true);

        ContainerBuilder autofacBuilder = new();
        autofacBuilder.RegisterModule(new IdGenClientAutofacModule(configuration.GetSection("IdGenClient")));
        autofacBuilder.Populate(services);
        autofacBuilder.RegisterType<MonotonicClock>().As<IClock>().SingleInstance();
        IContainer autofacServices = autofacBuilder.Build();

        IServiceProvider serviceProvider = new AutofacServiceProvider(autofacServices);
        IIdGenClient client = serviceProvider.GetRequiredService<IIdGenClient>();

        return client;
    }

    [OneTimeTearDown]
    public async Task AfterAllTests()
    {
        Client.Dispose();
        await App.DisposeAsync();
    }
}
