using System.Net;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Common.Testing.AspNet;

/// <summary>
/// Represents a base class for bootstrapping an ASP.NET service in order to test it.
/// </summary>
public abstract class BaseService : IAsyncDisposable
{
    protected WebApplication App { get; init; } = null!;
    protected ServiceOptions? Options { get; set; }

    protected WebApplicationBuilder CreateBuilder(Type programType, ServiceOptions options)
    {
        string? appName = programType.Assembly.GetName().Name;
        WebApplicationBuilder builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            ApplicationName = appName,
            EnvironmentName = options.Environment
        });
        builder.Configuration.AddJsonFile(options.ConfigFilePath, optional: false);
        builder.WebHost.UseKestrel(kestrel =>
        {
            kestrel.Listen(IPAddress.Loopback, options.ListenPort, listenOptions =>
            {
                listenOptions.UseHttps(options.CertificatePath, options.CertificatePassword);
            });
        });
        builder.Host.UseSerilog(dispose: false);
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

        Options = options;

        return builder;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        TimeSpan stopTimeout = Options?.StopTimeout ?? ServiceOptions.DefaultStopTimeout;
        await App.StopAsync(stopTimeout);
        await App.DisposeAsync();
    }

    public async Task Start()
    {
        await App.StartAsync();
    }
}
