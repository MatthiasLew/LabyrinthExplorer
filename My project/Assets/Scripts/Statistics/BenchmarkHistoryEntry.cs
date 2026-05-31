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
        public string mazeType;
        public int mazeWidth;
        public int mazeHeight;
        public int mazeSeed;
        public string mazeLayoutHash;
        public int randomSeed;
        public string benchmarkObjective;
        public int candidateEvaluations;
        public int firstSuccessCandidateEvaluation;
        public bool reachedGoal;
        public double totalRuntimeMs;
        public double logicTimeMs;
        // Surowy wynik algorytmu, bez prezentacyjnego BFS-u.
        public int pathLength;
        public float pathEfficiency;

        // Trasa wyliczona po sukcesie z odkrytego podgrafu, tylko do dodatkowej analizy.
        public int optimizedDiscoveredPathLength;
        public float optimizedDiscoveredPathEfficiency;
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
                mazeType = metrics.mazeType,
                mazeWidth = metrics.mazeWidth,
                mazeHeight = metrics.mazeHeight,
                mazeSeed = metrics.mazeSeed,
                mazeLayoutHash = metrics.mazeLayoutHash,
                randomSeed = metrics.randomSeed,
                benchmarkObjective = metrics.benchmarkObjective,
                candidateEvaluations = metrics.candidateEvaluations,
                firstSuccessCandidateEvaluation = metrics.firstSuccessCandidateEvaluation,
                reachedGoal = metrics.reachedGoal,
                totalRuntimeMs = metrics.totalRuntimeMs,
                logicTimeMs = metrics.logicTimeMs,
                pathLength = metrics.pathLength,
                pathEfficiency = metrics.pathEfficiency,
                optimizedDiscoveredPathLength = metrics.optimizedDiscoveredPathLength,
                optimizedDiscoveredPathEfficiency = metrics.optimizedDiscoveredPathEfficiency,
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

        public double GetComparableLogicTimeMs()
        {
            // Starsze wpisy historii nie mają logicTimeMs; w takim przypadku
            // zachowujemy ich dotychczasowy czas całkowity jako przybliżenie.
            return logicTimeMs > 0d ? logicTimeMs : totalRuntimeMs;
        }

        public string GetLogicTimeFormatted()
        {
            double value = GetComparableLogicTimeMs();
            if (value < 1000)
            {
                return $"{value:F2} ms";
            }

            return $"{value / 1000.0:F2}s";
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
