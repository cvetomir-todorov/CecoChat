using CecoChat.Testing;
using NUnit.Framework;
using Serilog;

namespace CecoChat.IdGen.Testing;

[SetUpFixture]
public class TestEnv
{
    [OneTimeSetUp]
    public void BeforeAllTests()
    {
        TestEnvSetup.InitEnv();
        TestEnvSetup.ConfigureLogging();
    }

    [OneTimeTearDown]
    public void AfterAllTests()
    {
        Log.CloseAndFlush();
    }
}
