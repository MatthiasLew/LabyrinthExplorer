using System;
using UnityEngine;

namespace Statistics
{
    /// <summary>
    /// Represents a single benchmark run record stored in history.
    /// </summary>
    [Serializable]
    public class BenchmarkHistoryEntry
    {
        public string testId;
        public int runIndex;
        public string algorithmName;
        public string mazeName;
        public int mazeWidth;
        public int mazeHeight;
        public int randomSeed;
        public bool reachedGoal;
        public double totalRuntimeMs;
        public int pathLength;
        public float pathEfficiency;
        public long measurementUtcTicks;

        /// <summary>
        /// Creates a BenchmarkHistoryEntry from algorithm metrics.
        /// </summary>
        public static BenchmarkHistoryEntry FromMetrics(
            string testId,
            int runIndex,
            Algorytm.Dane.AlgorithmMetrics metrics)
        {
            if (metrics == null)
            {
                return null;
            }

            return new BenchmarkHistoryEntry
            {
                testId = testId,
                runIndex = runIndex,
                algorithmName = metrics.algorithmName,
                mazeName = metrics.mazeName,
                mazeWidth = metrics.mazeWidth,
                mazeHeight = metrics.mazeHeight,
                randomSeed = metrics.randomSeed,
                reachedGoal = metrics.reachedGoal,
                totalRuntimeMs = metrics.totalRuntimeMs,
                pathLength = metrics.pathLength,
                pathEfficiency = metrics.pathEfficiency,
                measurementUtcTicks = DateTime.UtcNow.Ticks
            };
        }

        public string GetMeasurementTimeFormatted()
        {
            try
            {
                DateTime utcTime = new DateTime(measurementUtcTicks, DateTimeKind.Utc);
                return utcTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                return "Unknown";
            }
        }

        public string GetRuntimeFormatted()
        {
            if (totalRuntimeMs < 1000)
            {
                return $"{totalRuntimeMs:F2} ms";
            }

            double seconds = totalRuntimeMs / 1000.0;
            int minutes = (int)(seconds / 60);
            double remainingSeconds = seconds % 60;

            if (minutes > 0)
            {
                return $"{minutes}m {remainingSeconds:F2}s";
            }

            return $"{seconds:F2}s";
        }
    }

    /// <summary>
    /// Container for serializing history entries to JSON.
    /// </summary>
    [Serializable]
    public class BenchmarkHistoryData
    {
        public BenchmarkHistoryEntry[] entries;

        public BenchmarkHistoryData()
        {
            entries = new BenchmarkHistoryEntry[0];
        }

        public BenchmarkHistoryData(BenchmarkHistoryEntry[] entries)
        {
            this.entries = entries ?? new BenchmarkHistoryEntry[0];
        }
    }
}
