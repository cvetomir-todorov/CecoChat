using CecoChat.IdGen.Client;
using Common;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;

namespace CecoChat.IdGen.Testing.Tests;

/// <summary>
/// Check ID Gen test client configuration in order to see:
/// * How frequently the IDs are refreshed
/// * How many new IDs are obtained on each refresh
/// </summary>
public class GenerateIds : BaseTest
{
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(32)]
    [TestCase(128)]
    [TestCase(256)]
    [TestCase(1024)]
    public async Task Consecutively(int idCount)
    {
        List<long> idSequence = new(capacity: idCount);
        DateTime start = DateTime.UtcNow;

        for (int i = 0; i < idCount; ++i)
        {
            GetIdResult result = await Client.GetId(CancellationToken.None);
            if (result.Success)
            {
                idSequence.Add(result.Id);
            }
        }

        using (new AssertionScope())
        {
            VerifyIdSequence(idSequence, start, idCount);
        }
    }

    [TestCase(16, 1)]
    [TestCase(16, 16)]
    [TestCase(16, 32)]
    [TestCase(64, 16)]
    public async Task Concurrently(int concurrency, int idCount)
    {
        IEnumerable<int> concurrencyIds = Enumerable.Range(0, concurrency);
        Dictionary<int, List<long>> idMap = new(capacity: concurrency);
        DateTime start = DateTime.UtcNow;

        await Parallel.ForEachAsync(concurrencyIds, async (concurrencyId, ct) =>
        {
            List<long> idSequence = new(capacity: idCount);

            for (int i = 0; i < idCount; ++i)
            {
                GetIdResult result = await Client.GetId(ct);
                if (result.Success)
                {
                    idSequence.Add(result.Id);
                }
            }

            idMap.Add(concurrencyId, idSequence);
        });

        using (new AssertionScope())
        {
            foreach (KeyValuePair<int,List<long>> pair in idMap)
            {
                VerifyIdSequence(pair.Value, start, idCount);
            }
        }
    }

    private static void VerifyIdSequence(List<long> ids, DateTime start, int expectedCount)
    {
        ids.Count.Should().Be(expectedCount);
        ids.Should().BeInAscendingOrder();

        foreach (long id in ids)
        {
            DateTime idDateTime = id.ToTimestamp();
            idDateTime.Should().BeWithin(TimeSpan.FromSeconds(1)).After(start.AddSeconds(-0.1));
        }
    }
}
