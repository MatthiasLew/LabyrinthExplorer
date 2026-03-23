using System;
using System.Text;

namespace Algorytm.Dane
{
    /// <summary>
    /// Odpowiada za tworzenie czytelnej tekstowej reprezentacji metryk algorytmu
    /// przeznaczonej do logowania i diagnostyki.
    /// </summary>
    public static class MetricsFormatter
    {
        /// <summary>
        /// Konwertuje przekazane metryki do wielowierszowego tekstu o czytelnej strukturze.
        /// </summary>
        /// <param name="metrics">Obiekt metryk do sformatowania.</param>
        /// <returns>Czytelna tekstowa reprezentacja metryk.</returns>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy parametr <paramref name="metrics"/> ma wartość null.
        /// </exception>
        public static string ToReadableText(AlgorithmMetrics metrics)
        {
            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Algorithm: {metrics.algorithmName}");
            stringBuilder.AppendLine($"AlgorithmVersion: {metrics.algorithmVersion}");
            stringBuilder.AppendLine($"TestId: {metrics.testId}");
            stringBuilder.AppendLine($"RunIndex: {metrics.runIndex}");
            stringBuilder.AppendLine($"RandomSeed: {metrics.randomSeed}");
            stringBuilder.AppendLine($"Maze: {metrics.mazeName} ({metrics.mazeWidth}x{metrics.mazeHeight})");
            stringBuilder.AppendLine($"MazeType: {metrics.mazeType}");
            stringBuilder.AppendLine($"StartPosition: {metrics.startPosition}");
            stringBuilder.AppendLine($"FinishPosition: {metrics.finishPosition}");
            stringBuilder.AppendLine($"ReachedGoal: {metrics.reachedGoal}");
            stringBuilder.AppendLine($"FoundOptimalPath: {metrics.foundOptimalPath}");
            stringBuilder.AppendLine($"EndReason: {metrics.endReason}");
            stringBuilder.AppendLine($"StepsTaken: {metrics.stepsTaken}");
            stringBuilder.AppendLine($"PathLength: {metrics.pathLength}");
            stringBuilder.AppendLine($"ShortestPossiblePathLength: {metrics.shortestPossiblePathLength}");
            stringBuilder.AppendLine($"PathEfficiency: {metrics.pathEfficiency:F3}");
            stringBuilder.AppendLine($"VisitedCells: {metrics.visitedCells}");
            stringBuilder.AppendLine($"RevisitedCells: {metrics.revisitedCells}");
            stringBuilder.AppendLine($"BacktrackCount: {metrics.backtrackCount}");
            stringBuilder.AppendLine($"WallHits: {metrics.wallHits}");
            stringBuilder.AppendLine($"DeadEndsEncountered: {metrics.deadEndsEncountered}");
            stringBuilder.AppendLine($"ExpandedNodes: {metrics.expandedNodes}");
            stringBuilder.AppendLine($"FrontierMaxSize: {metrics.frontierMaxSize}");
            stringBuilder.AppendLine($"Iterations: {metrics.iterations}");
            stringBuilder.AppendLine($"Generations: {metrics.generations}");
            stringBuilder.AppendLine($"RestartCount: {metrics.restartCount}");
            stringBuilder.AppendLine($"StagnationIterations: {metrics.stagnationIterations}");
            stringBuilder.AppendLine($"BestFitness: {metrics.bestFitness:F4}");
            stringBuilder.AppendLine($"AverageFitness: {metrics.averageFitness:F4}");
            stringBuilder.AppendLine($"LogicTimeMs: {metrics.logicTimeMs:F3}");
            stringBuilder.AppendLine($"VisualizationTimeMs: {metrics.visualizationTimeMs:F3}");
            stringBuilder.AppendLine($"TotalRuntimeMs: {metrics.totalRuntimeMs:F3}");
            stringBuilder.AppendLine($"AverageIterationTimeMs: {metrics.averageIterationTimeMs:F4}");
            stringBuilder.AppendLine($"MaxIterationTimeMs: {metrics.maxIterationTimeMs:F4}");
            stringBuilder.AppendLine($"ManagedMemoryDeltaBytes: {metrics.managedMemoryDeltaBytes}");
            stringBuilder.AppendLine($"ProcessMemoryDeltaBytes: {metrics.processMemoryDeltaBytes}");
            stringBuilder.AppendLine($"AverageFps: {metrics.averageFps:F2}");
            stringBuilder.AppendLine($"MinFps: {metrics.minFps:F2}");
            stringBuilder.AppendLine($"MaxFps: {metrics.maxFps:F2}");

            if (!string.IsNullOrWhiteSpace(metrics.additionalInfo))
            {
                stringBuilder.AppendLine($"AdditionalInfo: {metrics.additionalInfo}");
            }

            return stringBuilder.ToString();
        }
    }
}