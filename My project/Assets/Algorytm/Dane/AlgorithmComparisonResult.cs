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
        /// Nazwa algorytmu, który osiągnął niższy średni czas wykonania.
        /// W przypadku remisu wybierany jest pierwszy algorytm.
        /// </summary>
        public string fasterAlgorithmName;

        /// <summary>
        /// Nazwa algorytmu, który osiągnął wyższy współczynnik skuteczności.
        /// W przypadku remisu wybierany jest pierwszy algorytm.
        /// </summary>
        public string moreReliableAlgorithmName;

        /// <summary>
        /// Nazwa algorytmu, który osiągnął wyższą średnią efektywność ścieżki.
        /// W przypadku remisu wybierany jest pierwszy algorytm.
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

            // Jakość ścieżki porównujemy wyłącznie dla przebiegów, które dotarły do mety.
            // Pusty tekst oznacza, że żaden algorytm nie znalazł poprawnego rozwiązania.
            string betterPathAlgorithmName = string.Empty;

            if (firstAlgorithmSummary.successfulRunCount > 0 &&
                secondAlgorithmSummary.successfulRunCount > 0)
            {
                betterPathAlgorithmName =
                    firstAlgorithmSummary.averageSuccessfulPathEfficiency >=
                    secondAlgorithmSummary.averageSuccessfulPathEfficiency
                        ? firstAlgorithmSummary.algorithmName
                        : secondAlgorithmSummary.algorithmName;
            }
            else if (firstAlgorithmSummary.successfulRunCount > 0)
            {
                betterPathAlgorithmName = firstAlgorithmSummary.algorithmName;
            }
            else if (secondAlgorithmSummary.successfulRunCount > 0)
            {
                betterPathAlgorithmName = secondAlgorithmSummary.algorithmName;
            }

            return new AlgorithmComparisonResult
            {
                firstAlgorithmSummary = firstAlgorithmSummary,
                secondAlgorithmSummary = secondAlgorithmSummary,
                fasterAlgorithmName =
                    firstAlgorithmSummary.averageTotalRuntimeMs <= secondAlgorithmSummary.averageTotalRuntimeMs
                        ? firstAlgorithmSummary.algorithmName
                        : secondAlgorithmSummary.algorithmName,
                moreReliableAlgorithmName =
                    firstAlgorithmSummary.successRate >= secondAlgorithmSummary.successRate
                        ? firstAlgorithmSummary.algorithmName
                        : secondAlgorithmSummary.algorithmName,
                betterPathAlgorithmName = betterPathAlgorithmName
            };
        }
    }
}