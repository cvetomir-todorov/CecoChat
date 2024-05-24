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
    [TestCase(2)]
    [TestCase(16)]
    // should force a new call
    [TestCase(32)]
    [TestCase(48)]
    [TestCase(64)]
    [TestCase(128)]
    public async Task Consecutively(int idCount)
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

    // client is configured to refresh IDs by 16 each second
    [TestCase(2, 1)]
    [TestCase(4, 4)]
    [TestCase(16, 1)]
    // should force a new call
    [TestCase(16, 2)]
    [TestCase(16, 4)]
    [TestCase(16, 8)]
    [TestCase(16, 16)]
    [TestCase(16, 32)]
    public async Task Simultaneously(int parallelCount, int idCountPerParallel)
    {
        IEnumerable<int> parallels = Enumerable.Range(0, parallelCount);
        Dictionary<int, List<long>> ids = new(capacity: parallelCount);
        DateTime start = DateTime.UtcNow;

        await Parallel.ForEachAsync(parallels, async (parallel, ct) =>
        {
            int localParallel = parallel;
            ids[localParallel] = new List<long>();

            for (int i = 0; i < idCountPerParallel; ++i)
            {
                GetIdResult result = await Client.GetId(ct);
                if (result.Success)
                {
                    ids[localParallel].Add(result.Id);
                }
            }
        });

        using (new AssertionScope())
        {
            foreach (KeyValuePair<int,List<long>> pair in ids)
            {
                List<long> idsForCurrentParallel = pair.Value;

                idsForCurrentParallel.Count.Should().Be(idCountPerParallel);
                idsForCurrentParallel.Should().BeInAscendingOrder();

                foreach (long id in idsForCurrentParallel)
                {
                    DateTime idDateTime = id.ToTimestamp();
                    idDateTime.Should().BeWithin(TimeSpan.FromSeconds(2.5)).After(start.AddSeconds(-0.5));
                }
            }
        }
    }
}
