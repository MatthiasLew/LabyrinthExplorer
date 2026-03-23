using System;
using System.Collections.Generic;
using System.Linq;

namespace Algorytm.Dane
{
    /// <summary>
    /// Reprezentuje zbiorcze podsumowanie wyników wielu uruchomień tego samego algorytmu.
    /// </summary>
    [Serializable]
    public class AlgorithmSummary
    {
        /// <summary>
        /// Nazwa algorytmu, którego dotyczy podsumowanie.
        /// </summary>
        public string algorithmName;

        /// <summary>
        /// Łączna liczba uruchomień uwzględnionych w podsumowaniu.
        /// </summary>
        public int runCount;

        /// <summary>
        /// Liczba uruchomień zakończonych osiągnięciem celu.
        /// </summary>
        public int successCount;

        /// <summary>
        /// Współczynnik skuteczności algorytmu wyrażony jako stosunek udanych uruchomień do wszystkich uruchomień.
        /// </summary>
        public float successRate;

        /// <summary>
        /// Średni całkowity czas wykonania algorytmu w milisekundach.
        /// </summary>
        public double averageTotalRuntimeMs;

        /// <summary>
        /// Minimalny całkowity czas wykonania algorytmu w milisekundach.
        /// </summary>
        public double minTotalRuntimeMs;

        /// <summary>
        /// Maksymalny całkowity czas wykonania algorytmu w milisekundach.
        /// </summary>
        public double maxTotalRuntimeMs;

        /// <summary>
        /// Średni czas wykonania logiki algorytmu w milisekundach.
        /// </summary>
        public double averageLogicTimeMs;

        /// <summary>
        /// Średni czas poświęcony na wizualizację w milisekundach.
        /// </summary>
        public double averageVisualizationTimeMs;

        /// <summary>
        /// Średnia długość ścieżki znalezionej przez algorytm.
        /// </summary>
        public double averagePathLength;

        /// <summary>
        /// Średnia liczba wykonanych kroków.
        /// </summary>
        public double averageStepsTaken;

        /// <summary>
        /// Średnia liczba odwiedzonych komórek.
        /// </summary>
        public double averageVisitedCells;

        /// <summary>
        /// Średnia efektywność ścieżki.
        /// </summary>
        public double averagePathEfficiency;

        /// <summary>
        /// Średnia liczba klatek na sekundę osiągnięta podczas wizualizacji.
        /// </summary>
        public float averageFps;

        /// <summary>
        /// Tworzy podsumowanie na podstawie listy metryk pojedynczych uruchomień algorytmu.
        /// </summary>
        /// <param name="metricsList">Lista metryk do zagregowania.</param>
        /// <returns>
        /// Obiekt zawierający zbiorcze statystyki dla przekazanych metryk.
        /// Jeśli lista jest pusta lub ma wartość null, zwracany jest pusty obiekt podsumowania.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Rzucany, gdy lista zawiera metryki należące do różnych algorytmów.
        /// </exception>
        public static AlgorithmSummary FromMetrics(IReadOnlyList<AlgorithmMetrics> metricsList)
        {
            if (metricsList == null || metricsList.Count == 0)
            {
                return new AlgorithmSummary();
            }

            string algorithmName = metricsList[0].algorithmName;

            if (metricsList.Any(metric => metric.algorithmName != algorithmName))
            {
                throw new ArgumentException("All metrics must belong to the same algorithm.", nameof(metricsList));
            }

            int runCount = metricsList.Count;
            int successCount = metricsList.Count(metric => metric.reachedGoal);

            return new AlgorithmSummary
            {
                algorithmName = algorithmName,
                runCount = runCount,
                successCount = successCount,
                successRate = (float)successCount / runCount,

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