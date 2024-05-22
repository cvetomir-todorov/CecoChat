using CecoChat.IdGen.Client;
using Common;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;

namespace CecoChat.IdGen.Testing.Tests;

public class GenerateIds : BaseTest
{
    // client is configured to refresh IDs by 16 each second
    [TestCase(1)]
    //[TestCase(2)]
    //[TestCase(16)]
    // should force a new call
    //[TestCase(32)]
    public async Task GenerateNumberOfIds(int idCount)
    {
        List<long> ids = new(capacity: idCount);
        DateTime start = DateTime.UtcNow;

        for (int i = 0; i < idCount; ++i)
        {
            GetIdResult result = await Client.GetId(CancellationToken.None);
            if (result.Success)
            {
                ids.Add(result.Id);
            }
        }

        using (new AssertionScope())
        {
            ids.Count.Should().Be(idCount);
            ids.Should().BeInAscendingOrder();

            foreach (long id in ids)
            {
                DateTime idDateTime = id.ToTimestamp();
                idDateTime.Should().BeWithin(TimeSpan.FromSeconds(2.5)).After(start.AddSeconds(-0.5));
            }
        }
    }
}
