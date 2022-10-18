﻿using System;
using System.Diagnostics;
using CecoChat;

namespace Check.Hashing;

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

        // warm-up
        fnv.Compute(evaluateParams.MinUserID);
        fnv.Compute(evaluateParams.MaxUserID);
        xxHash.Compute(evaluateParams.MinUserID);
        xxHash.Compute(evaluateParams.MaxUserID);

        // actual
        EvaluateHashResult fnvResult = EvaluateHash(fnv, evaluateParams);
        EvaluateHashResult xxHashResult = EvaluateHash(xxHash, evaluateParams);

        PrintResult(fnv.GetType().Name, evaluateParams, fnvResult);
        PrintResult(xxHash.GetType().Name, evaluateParams, xxHashResult);
    }

    private static void PrintResult(string hashName, EvaluateHashParams evaluateParams, EvaluateHashResult result)
    {
        Console.WriteLine("{0} for user IDs in [{1}, {2}] and partition count = {3}.", hashName,
            evaluateParams.MinUserID, evaluateParams.MaxUserID, evaluateParams.PartitionCount);
        Console.WriteLine("All {0} hashes calculated for {1:0.##} ms, 1000 hashes calculated for {2:0.####} ms.",
            evaluateParams.UserCount, result.TotalTime.TotalMilliseconds, result.AverageTimePer1000Hashes.TotalMilliseconds);
        Console.WriteLine("Total distribution deviation = {0}", result.TotalDistributionDeviation);
        Console.WriteLine("Max distribution deviation = {0}", result.MaxDistributionDeviation);
    }

    private record EvaluateHashParams
    {
        public int PartitionCount { get; init; }
        public long MinUserID { get; init; }
        public long MaxUserID { get; init; }
        public long UserCount => MaxUserID - MinUserID + 1;
    }

    private record EvaluateHashResult
    {
        public int TotalDistributionDeviation { get; init; }
        public int MaxDistributionDeviation { get; init; }
        public TimeSpan TotalTime { get; init; }
        public TimeSpan AverageTimePer1000Hashes { get; init; }
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

        int average = (int) Math.Round(evaluateParams.UserCount / (double) evaluateParams.PartitionCount);
        int totalDeviation = 0;
        int maxDeviation = 0;

        foreach (int partition in partitions)
        {
            int deviation = Math.Abs(partition - average);
            totalDeviation += deviation;
            maxDeviation = Math.Max(maxDeviation, deviation);
        }

        // ReSharper disable once PossibleLossOfFraction
        TimeSpan averageTime = stopwatch.Elapsed / (evaluateParams.UserCount / 1000);

        return new EvaluateHashResult
        {
            TotalDistributionDeviation = totalDeviation,
            MaxDistributionDeviation = maxDeviation,
            TotalTime = stopwatch.Elapsed,
            AverageTimePer1000Hashes = averageTime
        };
    }
}