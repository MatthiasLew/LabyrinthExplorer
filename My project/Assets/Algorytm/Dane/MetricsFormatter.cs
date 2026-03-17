using System.Text;

namespace Algorytm.Dane
{
    public static class MetricsFormatter
    {
        public static string ToReadableText(AlgorithmMetrics metrics)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Algorithm: {metrics.algorithmName}");
            stringBuilder.AppendLine($"TestId: {metrics.testId}");
            stringBuilder.AppendLine($"RunIndex: {metrics.runIndex}");
            stringBuilder.AppendLine($"Maze: {metrics.mazeName} ({metrics.mazeWidth}x{metrics.mazeHeight})");
            stringBuilder.AppendLine($"ReachedGoal: {metrics.reachedGoal}");
            stringBuilder.AppendLine($"EndReason: {metrics.endReason}");
            stringBuilder.AppendLine($"StepsTaken: {metrics.stepsTaken}");
            stringBuilder.AppendLine($"PathLength: {metrics.pathLength}");
            stringBuilder.AppendLine($"ShortestPossiblePathLength: {metrics.shortestPossiblePathLength}");
            stringBuilder.AppendLine($"PathEfficiency: {metrics.pathEfficiency:F3}");
            stringBuilder.AppendLine($"VisitedCells: {metrics.visitedCells}");
            stringBuilder.AppendLine($"ExpandedNodes: {metrics.expandedNodes}");
            stringBuilder.AppendLine($"FrontierMaxSize: {metrics.frontierMaxSize}");
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