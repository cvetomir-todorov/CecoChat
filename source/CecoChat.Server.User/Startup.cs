using System.Reflection;
using Autofac;
using Calzolari.Grpc.AspNetCore.Validation;
using CecoChat.Autofac;
using CecoChat.Data.User;
using CecoChat.Jwt;
using CecoChat.Server.Identity;
using CecoChat.Server.User.Clients;
using CecoChat.Server.User.HostedServices;
using FluentValidation;

namespace CecoChat.Server.User;

public class Startup
{
    private readonly UserDbOptions _userDbOptions;
    private readonly JwtOptions _jwtOptions;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;

        _userDbOptions = new();
        Configuration.GetSection("UserDB").Bind(_userDbOptions);

        _jwtOptions = new();
        Configuration.GetSection("Jwt").Bind(_jwtOptions);
    }

    public IConfiguration Configuration { get; }

    public IWebHostEnvironment Environment { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // security
        services.AddJwtAuthentication(_jwtOptions);
        services.AddAuthorization();

        // clients
        services.AddGrpc(grpc =>
        {
            grpc.EnableDetailedErrors = Environment.IsDevelopment();
            grpc.EnableMessageValidation();
        });
        services.AddGrpcValidation();

        // user db
        services.AddUserDb(_userDbOptions.Connect);

        // common
        services.AddAutoMapper(config =>
        {
            config.AddMaps(typeof(AutoMapperProfile));
        });
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddOptions();
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        // ordered hosted services
        builder.RegisterHostedService<InitUsersDb>();

        // user db
        builder.RegisterModule(new UserDbAutofacModule());
        builder.RegisterOptions<UserDbOptions>(Configuration.GetSection("UserDB"));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<ProfileService>();
        });
    }
}