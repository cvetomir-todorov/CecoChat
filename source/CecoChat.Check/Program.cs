using System;
using System.Diagnostics;

namespace CecoChat.Check
{
    public static class Program
    {
        public static void Main()
        {
            EvaluateHashParams evaluateParams = new()
            {
                PartitionCount = 1000,
                MinUserID = 1,
                MaxUserID = 100_000_000
            };

            INonCryptoHash fnv = new FnvHash();
            INonCryptoHash xxHash = new XXHash();

            EvaluateHashResult fnvResult = EvaluateHash(fnv, evaluateParams);
            EvaluateHashResult xxHashResult = EvaluateHash(xxHash, evaluateParams);

            PrintResult(fnv.GetType().Name, evaluateParams, fnvResult);
            PrintResult(xxHash.GetType().Name, evaluateParams, xxHashResult);
        }

        private static void PrintResult(string hashName, EvaluateHashParams evaluateParams, EvaluateHashResult result)
        {
            Console.WriteLine(
                "{0} for user IDs in [{1}, {2}] and partition count = {3} calculated for {4:0.00} ms with distribution deviation = {5}.",
                hashName, evaluateParams.MinUserID, evaluateParams.MaxUserID, evaluateParams.PartitionCount,
                result.Time.TotalMilliseconds, result.DistributionDeviation);
        }

        private record EvaluateHashParams
        {
            public int PartitionCount { get; init; }
            public long MinUserID { get; init; }
            public long MaxUserID { get; init; }
        }

        private record EvaluateHashResult
        {
            public int DistributionDeviation { get; init; }
            public TimeSpan Time { get; init; }
        }

        private static EvaluateHashResult EvaluateHash(INonCryptoHash hash, EvaluateHashParams evaluateParams)
        {
            int[] partitions = new int[evaluateParams.PartitionCount];
            Stopwatch stopwatch = Stopwatch.StartNew();

            for (long userID = evaluateParams.MinUserID; userID <= evaluateParams.MaxUserID; ++userID)
            {
                int partition = Math.Abs(hash.Compute(userID) % evaluateParams.PartitionCount);
                partitions[partition]++;
            }

            stopwatch.Stop();

            int average = (int) Math.Round((evaluateParams.MaxUserID - evaluateParams.MinUserID + 1) / (double) evaluateParams.PartitionCount);
            int totalDeviation = 0;

            foreach (int partition in partitions)
            {
                int deviation = Math.Abs(partition - average);
                totalDeviation += deviation;
            }

            return new EvaluateHashResult
            {
                DistributionDeviation = totalDeviation,
                Time = stopwatch.Elapsed
            };
        }
    }
}
