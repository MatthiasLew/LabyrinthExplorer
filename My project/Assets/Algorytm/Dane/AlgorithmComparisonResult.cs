using System;

namespace Algorytm.Dane
{
    [Serializable]
    public class AlgorithmComparisonResult
    {
        public AlgorithmSummary firstAlgorithmSummary;
        public AlgorithmSummary secondAlgorithmSummary;

        public string fasterAlgorithmName;
        public string moreReliableAlgorithmName;
        public string betterPathAlgorithmName;

        public static AlgorithmComparisonResult Create(
            AlgorithmSummary firstAlgorithmSummary,
            AlgorithmSummary secondAlgorithmSummary)
        {
            var result = new AlgorithmComparisonResult
            {
                firstAlgorithmSummary = firstAlgorithmSummary,
                secondAlgorithmSummary = secondAlgorithmSummary
            };

            result.fasterAlgorithmName =
                firstAlgorithmSummary.averageTotalRuntimeMs <= secondAlgorithmSummary.averageTotalRuntimeMs
                    ? firstAlgorithmSummary.algorithmName
                    : secondAlgorithmSummary.algorithmName;

            result.moreReliableAlgorithmName =
                firstAlgorithmSummary.successRate >= secondAlgorithmSummary.successRate
                    ? firstAlgorithmSummary.algorithmName
                    : secondAlgorithmSummary.algorithmName;

            result.betterPathAlgorithmName =
                firstAlgorithmSummary.averagePathEfficiency >= secondAlgorithmSummary.averagePathEfficiency
                    ? firstAlgorithmSummary.algorithmName
                    : secondAlgorithmSummary.algorithmName;

            return result;
        }
    }
}