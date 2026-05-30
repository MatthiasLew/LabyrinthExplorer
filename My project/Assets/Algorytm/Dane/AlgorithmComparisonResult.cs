using System;

namespace Algorytm.Dane
{
    /// <summary>
    /// Reprezentuje wynik porównania dwóch algorytmów na podstawie ich statystyk zbiorczych.
    /// </summary>
    [Serializable]
    public class AlgorithmComparisonResult
    {
        /// <summary>
        /// Podsumowanie statystyk pierwszego algorytmu.
        /// </summary>
        public AlgorithmSummary firstAlgorithmSummary;

        /// <summary>
        /// Podsumowanie statystyk drugiego algorytmu.
        /// </summary>
        public AlgorithmSummary secondAlgorithmSummary;

        /// <summary>
        /// Nazwa algorytmu, który osiągnął niższy średni czas logiki algorytmu.
        /// Pusta wartość oznacza remis.
        /// </summary>
        public string fasterAlgorithmName;

        /// <summary>
        /// Nazwa algorytmu, który osiągnął wyższy współczynnik skuteczności.
        /// Pusta wartość oznacza remis.
        /// </summary>
        public string moreReliableAlgorithmName;

        /// <summary>
        /// Nazwa algorytmu, który osiągnął wyższą średnią efektywność surowej ścieżki.
        /// Pusta wartość oznacza remis albo brak poprawnych rozwiązań.
        /// </summary>
        public string betterPathAlgorithmName;

        /// <summary>
        /// Tworzy wynik porównania dwóch algorytmów na podstawie przekazanych podsumowań statystyk.
        /// </summary>
        /// <param name="firstAlgorithmSummary">Podsumowanie statystyk pierwszego algorytmu.</param>
        /// <param name="secondAlgorithmSummary">Podsumowanie statystyk drugiego algorytmu.</param>
        /// <returns>Obiekt zawierający wynik porównania obu algorytmów.</returns>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy co najmniej jeden z argumentów ma wartość null.
        /// </exception>
        public static AlgorithmComparisonResult Create(
            AlgorithmSummary firstAlgorithmSummary,
            AlgorithmSummary secondAlgorithmSummary)
        {
            if (firstAlgorithmSummary == null)
            {
                throw new ArgumentNullException(nameof(firstAlgorithmSummary));
            }

            if (secondAlgorithmSummary == null)
            {
                throw new ArgumentNullException(nameof(secondAlgorithmSummary));
            }

            // Jakość porównujemy po surowej trasie agenta, a nie po prezentacyjnym BFS-ie.
            // Pusty tekst oznacza remis albo brak poprawnych rozwiązań.
            string betterPathAlgorithmName = string.Empty;

            if (firstAlgorithmSummary.successfulRunCount > 0 &&
                secondAlgorithmSummary.successfulRunCount > 0)
            {
                const double epsilon = 0.000001d;
                double difference =
                    firstAlgorithmSummary.averageSuccessfulPathEfficiency -
                    secondAlgorithmSummary.averageSuccessfulPathEfficiency;

                if (Math.Abs(difference) > epsilon)
                {
                    betterPathAlgorithmName = difference > 0d
                        ? firstAlgorithmSummary.algorithmName
                        : secondAlgorithmSummary.algorithmName;
                }
            }
            else if (firstAlgorithmSummary.successfulRunCount > 0)
            {
                betterPathAlgorithmName = firstAlgorithmSummary.algorithmName;
            }
            else if (secondAlgorithmSummary.successfulRunCount > 0)
            {
                betterPathAlgorithmName = secondAlgorithmSummary.algorithmName;
            }

            // TotalRuntime w korutynie obejmuje oczekiwanie na następne klatki po yield return null.
            // Ranking szybkości opieramy na czasie faktycznie wykonywanej logiki.
            string fasterAlgorithmName =
                Math.Abs(firstAlgorithmSummary.averageLogicTimeMs - secondAlgorithmSummary.averageLogicTimeMs) <= 0.000001d
                    ? string.Empty
                    : (firstAlgorithmSummary.averageLogicTimeMs < secondAlgorithmSummary.averageLogicTimeMs
                        ? firstAlgorithmSummary.algorithmName
                        : secondAlgorithmSummary.algorithmName);

            return new AlgorithmComparisonResult
            {
                firstAlgorithmSummary = firstAlgorithmSummary,
                secondAlgorithmSummary = secondAlgorithmSummary,
                fasterAlgorithmName = fasterAlgorithmName,
                moreReliableAlgorithmName =
                    firstAlgorithmSummary.successRate == secondAlgorithmSummary.successRate
                        ? string.Empty
                        : (firstAlgorithmSummary.successRate > secondAlgorithmSummary.successRate
                            ? firstAlgorithmSummary.algorithmName
                            : secondAlgorithmSummary.algorithmName),
                betterPathAlgorithmName = betterPathAlgorithmName
            };
        }
    }
}