using System;
using System.Collections.Generic;
using System.Linq;

namespace Algorytm.Dane
{
    [Serializable]
    public class AlgorithmSummary
    {
        public string algorithmName;
        public int runCount;
        public int successCount;
        public float successRate;

        public double averageTotalRuntimeMs;
        public double minTotalRuntimeMs;
        public double maxTotalRuntimeMs;

        public double averageLogicTimeMs;
        public double averageVisualizationTimeMs;

        public double averagePathLength;
        public double averageStepsTaken;
        public double averageVisitedCells;
        public double averagePathEfficiency;

        public float averageFps;

        public static AlgorithmSummary FromMetrics(IReadOnlyList<AlgorithmMetrics> metricsList)
        {
            if (metricsList == null || metricsList.Count == 0)
            {
                return new AlgorithmSummary();
            }

            return new AlgorithmSummary
            {
                algorithmName = metricsList[0].algorithmName,
                runCount = metricsList.Count,
                successCount = metricsList.Count(metric => metric.reachedGoal),
                successRate = (float)metricsList.Count(metric => metric.reachedGoal) / metricsList.Count,

                averageTotalRuntimeMs = metricsList.Average(metric => metric.totalRuntimeMs),
                minTotalRuntimeMs = metricsList.Min(metric => metric.totalRuntimeMs),
                maxTotalRuntimeMs = metricsList.Max(metric => metric.totalRuntimeMs),

                averageLogicTimeMs = metricsList.Average(metric => metric.logicTimeMs),
                averageVisualizationTimeMs = metricsList.Average(metric => metric.visualizationTimeMs),

                averagePathLength = metricsList.Average(metric => metric.pathLength),
                averageStepsTaken = metricsList.Average(metric => metric.stepsTaken),
                averageVisitedCells = metricsList.Average(metric => metric.visitedCells),
                averagePathEfficiency = metricsList.Average(metric => metric.pathEfficiency),

                averageFps = metricsList.Average(metric => metric.averageFps)
            };
        }
    }
}