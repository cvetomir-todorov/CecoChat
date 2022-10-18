using Autofac;
using CecoChat.Autofac;
using CecoChat.Data.Config;
using CecoChat.Server.IDGen.Generation;
using CecoChat.Server.IDGen.HostedServices;

namespace CecoChat.Server.IDGen;

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;
    }

    public IConfiguration Configuration { get; }

    public IWebHostEnvironment Environment { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // clients
        services.AddGrpc(rpc => rpc.EnableDetailedErrors = Environment.IsDevelopment());

        // required
        services.AddOptions();
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        // ordered hosted services
        builder.RegisterHostedService<InitDynamicConfig>();

        // configuration
        builder.RegisterModule(new ConfigDbAutofacModule
        {
            RedisConfiguration = Configuration.GetSection("ConfigDB"),
            RegisterSnowflake = true
        });
        builder.RegisterOptions<ConfigOptions>(Configuration.GetSection("Config"));

        // snowflake
        builder.RegisterType<SnowflakeGenerator>().As<IIdentityGenerator>().SingleInstance();
        builder.RegisterType<FnvHash>().As<INonCryptoHash>().SingleInstance();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<GrpcIDGenService>();
        });
    }
}