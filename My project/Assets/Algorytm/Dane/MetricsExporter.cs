using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Algorytm.Dane
{
    public static class MetricsExporter
    {
        [Serializable]
        private class AlgorithmMetricsCollection
        {
            public List<AlgorithmMetrics> items = new();
        }

        public static void ExportToJson(string fileName, IReadOnlyList<AlgorithmMetrics> metricsList)
        {
            var collection = new AlgorithmMetricsCollection();
            collection.items.AddRange(metricsList);

            string json = JsonUtility.ToJson(collection, true);
            string path = Path.Combine(Application.persistentDataPath, fileName);

            File.WriteAllText(path, json, Encoding.UTF8);
            Debug.Log($"Metrics exported to JSON: {path}");
        }

        public static void ExportToCsv(string fileName, IReadOnlyList<AlgorithmMetrics> metricsList)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(
                "AlgorithmName,TestId,RunIndex,RandomSeed,MazeName,MazeType,MazeWidth,MazeHeight,ReachedGoal,FoundOptimalPath," +
                "StepsTaken,PathLength,ShortestPossiblePathLength,PathEfficiency,VisitedCells,RevisitedCells,BacktrackCount,WallHits," +
                "DeadEndsEncountered,ExpandedNodes,ValidMovesConsidered,InvalidMovesConsidered,FrontierMaxSize,Iterations,Generations," +
                "RestartCount,StagnationIterations,BestFitness,AverageFitness,LogicTimeMs,VisualizationTimeMs,TotalRuntimeMs," +
                "AverageIterationTimeMs,MaxIterationTimeMs,ManagedMemoryBeforeBytes,ManagedMemoryAfterBytes,ManagedPeakMemoryBytes," +
                "ManagedMemoryDeltaBytes,ProcessMemoryBeforeBytes,ProcessMemoryAfterBytes,ProcessPeakMemoryBytes,ProcessMemoryDeltaBytes," +
                "AverageFps,MinFps,MaxFps,TotalFrames,VisualizationDurationSeconds,EndReason,AdditionalInfo");

            foreach (AlgorithmMetrics metrics in metricsList)
            {
                stringBuilder.AppendLine(string.Join(",",
                    Escape(metrics.algorithmName),
                    Escape(metrics.testId),
                    metrics.runIndex,
                    metrics.randomSeed,
                    Escape(metrics.mazeName),
                    Escape(metrics.mazeType),
                    metrics.mazeWidth,
                    metrics.mazeHeight,
                    metrics.reachedGoal,
                    metrics.foundOptimalPath,
                    metrics.stepsTaken,
                    metrics.pathLength,
                    metrics.shortestPossiblePathLength,
                    metrics.pathEfficiency,
                    metrics.visitedCells,
                    metrics.revisitedCells,
                    metrics.backtrackCount,
                    metrics.wallHits,
                    metrics.deadEndsEncountered,
                    metrics.expandedNodes,
                    metrics.validMovesConsidered,
                    metrics.invalidMovesConsidered,
                    metrics.frontierMaxSize,
                    metrics.iterations,
                    metrics.generations,
                    metrics.restartCount,
                    metrics.stagnationIterations,
                    metrics.bestFitness,
                    metrics.averageFitness,
                    metrics.logicTimeMs,
                    metrics.visualizationTimeMs,
                    metrics.totalRuntimeMs,
                    metrics.averageIterationTimeMs,
                    metrics.maxIterationTimeMs,
                    metrics.managedMemoryBeforeBytes,
                    metrics.managedMemoryAfterBytes,
                    metrics.managedPeakMemoryBytes,
                    metrics.managedMemoryDeltaBytes,
                    metrics.processMemoryBeforeBytes,
                    metrics.processMemoryAfterBytes,
                    metrics.processPeakMemoryBytes,
                    metrics.processMemoryDeltaBytes,
                    metrics.averageFps,
                    metrics.minFps,
                    metrics.maxFps,
                    metrics.totalFrames,
                    metrics.visualizationDurationSeconds,
                    Escape(metrics.endReason),
                    Escape(metrics.additionalInfo)
                ));
            }

            string path = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllText(path, stringBuilder.ToString(), Encoding.UTF8);
            Debug.Log($"Metrics exported to CSV: {path}");
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            string escapedValue = value.Replace("\"", "\"\"");
            return $"\"{escapedValue}\"";
        }
    }
}