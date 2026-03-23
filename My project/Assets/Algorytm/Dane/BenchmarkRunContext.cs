using System;
using UnityEngine;

namespace Algorytm.Dane
{
    /// <summary>
    /// Przechowuje kontekst pojedynczego uruchomienia benchmarku,
    /// który może zostać następnie przepisany do obiektu metryk.
    /// </summary>
    [Serializable]
    public class BenchmarkRunContext
    {
        /// <summary>
        /// Unikalny identyfikator testu.
        /// </summary>
        public string testId;

        /// <summary>
        /// Indeks bieżącego uruchomienia w ramach serii testów.
        /// </summary>
        public int runIndex;

        /// <summary>
        /// Ziarno generatora liczb losowych użyte dla danego uruchomienia.
        /// </summary>
        public int randomSeed;

        /// <summary>
        /// Nazwa badanego labiryntu.
        /// </summary>
        public string mazeName;

        /// <summary>
        /// Typ badanego labiryntu.
        /// </summary>
        public string mazeType;

        /// <summary>
        /// Szerokość labiryntu w komórkach.
        /// </summary>
        public int mazeWidth;

        /// <summary>
        /// Wysokość labiryntu w komórkach.
        /// </summary>
        public int mazeHeight;

        /// <summary>
        /// Pozycja startowa w labiryncie.
        /// </summary>
        public Vector2Int startPosition;

        /// <summary>
        /// Pozycja końcowa w labiryncie.
        /// </summary>
        public Vector2Int finishPosition;

        /// <summary>
        /// Tworzy nowy kontekst pojedynczego uruchomienia benchmarku.
        /// </summary>
        /// <param name="algorithmTestPrefix">Prefiks używany podczas budowania identyfikatora testu.</param>
        /// <param name="runIndex">Indeks bieżącego uruchomienia.</param>
        /// <param name="randomSeed">Ziarno generatora liczb losowych.</param>
        /// <param name="mazeName">Nazwa badanego labiryntu.</param>
        /// <param name="mazeType">Typ badanego labiryntu.</param>
        /// <param name="mazeWidth">Szerokość labiryntu w komórkach.</param>
        /// <param name="mazeHeight">Wysokość labiryntu w komórkach.</param>
        /// <param name="startPosition">Pozycja startowa w labiryncie.</param>
        /// <param name="finishPosition">Pozycja końcowa w labiryncie.</param>
        /// <returns>Nowy obiekt kontekstu uruchomienia benchmarku.</returns>
        /// <exception cref="ArgumentException">
        /// Rzucany, gdy parametr <paramref name="algorithmTestPrefix"/> jest pusty lub zawiera wyłącznie białe znaki.
        /// </exception>
        public static BenchmarkRunContext Create(
            string algorithmTestPrefix,
            int runIndex,
            int randomSeed,
            string mazeName,
            string mazeType,
            int mazeWidth,
            int mazeHeight,
            Vector2Int startPosition,
            Vector2Int finishPosition)
        {
            if (string.IsNullOrWhiteSpace(algorithmTestPrefix))
            {
                throw new ArgumentException("Algorithm test prefix cannot be null or empty.", nameof(algorithmTestPrefix));
            }

            return new BenchmarkRunContext
            {
                testId = $"{algorithmTestPrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{runIndex}",
                runIndex = runIndex,
                randomSeed = randomSeed,
                mazeName = mazeName,
                mazeType = mazeType,
                mazeWidth = mazeWidth,
                mazeHeight = mazeHeight,
                startPosition = startPosition,
                finishPosition = finishPosition
            };
        }

        /// <summary>
        /// Przepisuje dane kontekstu uruchomienia do obiektu metryk.
        /// </summary>
        /// <param name="metrics">Obiekt metryk, który zostanie uzupełniony danymi kontekstowymi.</param>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy parametr <paramref name="metrics"/> ma wartość null.
        /// </exception>
        public void ApplyTo(AlgorithmMetrics metrics)
        {
            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            metrics.testId = testId;
            metrics.runIndex = runIndex;
            metrics.randomSeed = randomSeed;
            metrics.mazeName = mazeName;
            metrics.mazeType = mazeType;
            metrics.mazeWidth = mazeWidth;
            metrics.mazeHeight = mazeHeight;
            metrics.startPosition = startPosition;
            metrics.finishPosition = finishPosition;
        }
    }
}