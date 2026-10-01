using Common.Testing.AspNet;
using NUnit.Framework;

namespace CecoChat.IdGen.Testing.Tests;

public abstract class BaseTest
{
    private IdGenService _idGenService;
    private IdGenClient _idGenClient;

    [OneTimeSetUp]
    public async Task BeforeAllTests()
    {
        ServiceOptions options = new()
        {
            Environment = "Test",
            ListenPort = 32002,
            CertificatePath = "services.pfx",
            CertificatePassword = "cecochat",
            ConfigFilePath = "idgen-service.json"
        };
        _idGenService = new(options);
        await _idGenService.Start();

        _idGenClient = new IdGenClient(configFilePath: "idgen-client.json");
    }

    [OneTimeTearDown]
    public async Task AfterAllTests()
    {
        _idGenClient.Dispose();
        await _idGenService.DisposeAsync();
    }

    protected IdGenClient Client => _idGenClient;
}
