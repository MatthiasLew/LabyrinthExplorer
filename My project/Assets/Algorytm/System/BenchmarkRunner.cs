using System;
using System.Collections;
using System.Collections.Generic;
using Algorytm.Dane;
using UnityEngine;

namespace Algorytm.System
{
    /// <summary>
    /// Odpowiada za uruchamianie benchmarków pojedynczych algorytmów
    /// oraz porównań między dwoma algorytmami.
    /// </summary>
    public class BenchmarkRunner : MonoBehaviour
    {
        [Header("Benchmark Settings")]
        [SerializeField] private bool exportJson = true;
        [SerializeField] private bool exportCsv = true;
        [SerializeField] private string exportFilePrefix = "benchmark_results";

        private readonly List<AlgorithmMetrics> _allMetrics = new();

        /// <summary>
        /// Zwraca pełną listę metryk zebranych podczas ostatniego uruchomienia benchmarku.
        /// </summary>
        public IReadOnlyList<AlgorithmMetrics> AllMetrics => _allMetrics;

        /// <summary>
        /// Uruchamia serię porównawczą dla dwóch algorytmów i zwraca wynik ich zbiorczego porównania.
        /// </summary>
        /// <param name="firstAlgorithm">Pierwszy algorytm do porównania.</param>
        /// <param name="secondAlgorithm">Drugi algorytm do porównania.</param>
        /// <param name="baseContext">Bazowy kontekst uruchomienia benchmarku.</param>
        /// <param name="runCount">Liczba uruchomień dla każdego algorytmu.</param>
        /// <param name="onCompleted">Opcjonalna akcja wywoływana po zakończeniu benchmarku.</param>
        /// <returns>Korutyna wykonująca benchmark porównawczy.</returns>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy którykolwiek z wymaganych argumentów ma wartość null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Rzucany, gdy parametr <paramref name="runCount"/> jest mniejszy lub równy zero.
        /// </exception>
        public IEnumerator RunComparison(
            IMazeAlgorithm firstAlgorithm,
            IMazeAlgorithm secondAlgorithm,
            MazeAlgorithmContext baseContext,
            int runCount,
            Action<AlgorithmComparisonResult> onCompleted = null)
        {
            if (firstAlgorithm == null)
            {
                throw new ArgumentNullException(nameof(firstAlgorithm));
            }

            if (secondAlgorithm == null)
            {
                throw new ArgumentNullException(nameof(secondAlgorithm));
            }

            if (baseContext == null)
            {
                throw new ArgumentNullException(nameof(baseContext));
            }

            if (runCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(runCount), "Run count must be greater than zero.");
            }

            _allMetrics.Clear();

            var firstAlgorithmMetrics = new List<AlgorithmMetrics>();
            var secondAlgorithmMetrics = new List<AlgorithmMetrics>();

            for (int runIndex = 0; runIndex < runCount; runIndex++)
            {
                int runSeed = baseContext.randomSeed + runIndex;

                MazeAlgorithmContext firstContext = CloneContext(baseContext, runSeed);
                MazeAlgorithmContext secondContext = CloneContext(baseContext, runSeed);

                yield return RunSingleAlgorithm(firstAlgorithm, firstContext, runIndex, firstAlgorithmMetrics);
                yield return RunSingleAlgorithm(secondAlgorithm, secondContext, runIndex, secondAlgorithmMetrics);
            }

            AlgorithmSummary firstSummary = AlgorithmSummary.FromMetrics(firstAlgorithmMetrics);
            AlgorithmSummary secondSummary = AlgorithmSummary.FromMetrics(secondAlgorithmMetrics);
            AlgorithmComparisonResult comparisonResult = AlgorithmComparisonResult.Create(firstSummary, secondSummary);

            ExportResultsIfEnabled();

            onCompleted?.Invoke(comparisonResult);
        }

        /// <summary>
        /// Uruchamia serię benchmarków dla pojedynczego algorytmu i zwraca jego zbiorcze podsumowanie.
        /// </summary>
        /// <param name="algorithm">Algorytm do uruchomienia.</param>
        /// <param name="baseContext">Bazowy kontekst uruchomienia benchmarku.</param>
        /// <param name="runCount">Liczba uruchomień algorytmu.</param>
        /// <param name="onCompleted">Opcjonalna akcja wywoływana po zakończeniu serii.</param>
        /// <returns>Korutyna wykonująca serię benchmarków.</returns>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy parametr <paramref name="algorithm"/> lub <paramref name="baseContext"/> ma wartość null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Rzucany, gdy parametr <paramref name="runCount"/> jest mniejszy lub równy zero.
        /// </exception>
        public IEnumerator RunSingleAlgorithmSeries(
            IMazeAlgorithm algorithm,
            MazeAlgorithmContext baseContext,
            int runCount,
            Action<AlgorithmSummary> onCompleted = null)
        {
            if (algorithm == null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            if (baseContext == null)
            {
                throw new ArgumentNullException(nameof(baseContext));
            }

            if (runCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(runCount), "Run count must be greater than zero.");
            }

            _allMetrics.Clear();

            var algorithmMetrics = new List<AlgorithmMetrics>();

            for (int runIndex = 0; runIndex < runCount; runIndex++)
            {
                int runSeed = baseContext.randomSeed + runIndex;
                MazeAlgorithmContext runContext = CloneContext(baseContext, runSeed);

                yield return RunSingleAlgorithm(algorithm, runContext, runIndex, algorithmMetrics);
            }

            AlgorithmSummary summary = AlgorithmSummary.FromMetrics(algorithmMetrics);

            ExportResultsIfEnabled();

            onCompleted?.Invoke(summary);
        }

        /// <summary>
        /// Uruchamia pojedynczy przebieg wybranego algorytmu i zapisuje zebrane metryki do wskazanej listy.
        /// </summary>
        /// <param name="algorithm">Algorytm do uruchomienia.</param>
        /// <param name="context">Kontekst uruchomienia.</param>
        /// <param name="runIndex">Indeks bieżącego uruchomienia.</param>
        /// <param name="targetList">Lista docelowa, do której zostaną dodane metryki.</param>
        /// <returns>Korutyna wykonująca pojedynczy przebieg algorytmu.</returns>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy parametr <paramref name="algorithm"/>, <paramref name="context"/> lub <paramref name="targetList"/> ma wartość null.
        /// </exception>
        private IEnumerator RunSingleAlgorithm(
            IMazeAlgorithm algorithm,
            MazeAlgorithmContext context,
            int runIndex,
            List<AlgorithmMetrics> targetList)
        {
            if (algorithm == null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (targetList == null)
            {
                throw new ArgumentNullException(nameof(targetList));
            }

            AlgorithmMetrics metrics = CreateBaseMetrics(algorithm, context, runIndex);
            AlgorithmProfiler profiler = new AlgorithmProfiler();

            ApplyContextToMetrics(context, metrics);
            PrepareFpsTracking(context);

            profiler.Begin();
            yield return algorithm.Run(context, metrics, profiler);
            profiler.End();

            FillVisualizationMetrics(context, metrics);
            profiler.FillMetrics(metrics);

            if (string.IsNullOrWhiteSpace(metrics.endReason))
            {
                metrics.endReason = metrics.reachedGoal ? "GoalReached" : "FinishedWithoutGoal";
            }

            metrics.FinalizeDerivedMetrics();

            targetList.Add(metrics);
            _allMetrics.Add(metrics);

            Debug.Log(MetricsFormatter.ToReadableText(metrics));
        }

        /// <summary>
        /// Tworzy bazowy obiekt metryk dla pojedynczego uruchomienia algorytmu.
        /// </summary>
        /// <param name="algorithm">Algorytm, którego dotyczą metryki.</param>
        /// <param name="context">Kontekst uruchomienia.</param>
        /// <param name="runIndex">Indeks bieżącego uruchomienia.</param>
        /// <returns>Nowy obiekt metryk z uzupełnionymi danymi podstawowymi.</returns>
        private static AlgorithmMetrics CreateBaseMetrics(
            IMazeAlgorithm algorithm,
            MazeAlgorithmContext context,
            int runIndex)
        {
            return new AlgorithmMetrics
            {
                algorithmName = algorithm.AlgorithmName,
                algorithmVersion = algorithm.AlgorithmVersion,
                testId = $"{algorithm.AlgorithmName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{runIndex}",
                runIndex = runIndex,
                randomSeed = context.randomSeed
            };
        }

        /// <summary>
        /// Przepisuje podstawowe informacje z kontekstu benchmarku do obiektu metryk.
        /// </summary>
        /// <param name="context">Kontekst uruchomienia.</param>
        /// <param name="metrics">Obiekt metryk do uzupełnienia.</param>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy parametr <paramref name="context"/> lub <paramref name="metrics"/> ma wartość null.
        /// </exception>
        private static void ApplyContextToMetrics(MazeAlgorithmContext context, AlgorithmMetrics metrics)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            metrics.mazeName = context.mazeName;
            metrics.mazeType = context.mazeType;
            metrics.mazeWidth = context.mazeWidth;
            metrics.mazeHeight = context.mazeHeight;
            metrics.startPosition = context.startPosition;
            metrics.finishPosition = context.finishPosition;
        }

        /// <summary>
        /// Tworzy kopię kontekstu benchmarku z nowym ziarnem generatora liczb losowych.
        /// </summary>
        /// <param name="sourceContext">Kontekst źródłowy.</param>
        /// <param name="randomSeed">Ziarno użyte w nowym kontekście.</param>
        /// <returns>Nowa instancja kontekstu.</returns>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy parametr <paramref name="sourceContext"/> ma wartość null.
        /// </exception>
        private static MazeAlgorithmContext CloneContext(MazeAlgorithmContext sourceContext, int randomSeed)
        {
            if (sourceContext == null)
            {
                throw new ArgumentNullException(nameof(sourceContext));
            }

            return new MazeAlgorithmContext
            {
                mazeName = sourceContext.mazeName,
                mazeType = sourceContext.mazeType,
                mazeWidth = sourceContext.mazeWidth,
                mazeHeight = sourceContext.mazeHeight,
                startPosition = sourceContext.startPosition,
                finishPosition = sourceContext.finishPosition,
                randomSeed = randomSeed,
                enableVisualization = sourceContext.enableVisualization,
                stepDelaySeconds = sourceContext.stepDelaySeconds,
                mazeData = sourceContext.mazeData,
                coroutineHost = sourceContext.coroutineHost,
                fpsTracker = sourceContext.fpsTracker
            };
        }

        /// <summary>
        /// Przygotowuje śledzenie FPS przed uruchomieniem algorytmu.
        /// </summary>
        /// <param name="context">Kontekst uruchomienia.</param>
        private static void PrepareFpsTracking(MazeAlgorithmContext context)
        {
            if (!context.enableVisualization || context.fpsTracker == null)
            {
                return;
            }

            context.fpsTracker.StartTracking();
        }

        /// <summary>
        /// Przepisuje do metryk statystyki FPS zebrane podczas wizualizacji.
        /// </summary>
        /// <param name="context">Kontekst uruchomienia.</param>
        /// <param name="metrics">Obiekt metryk do uzupełnienia.</param>
        private static void FillVisualizationMetrics(MazeAlgorithmContext context, AlgorithmMetrics metrics)
        {
            if (!context.enableVisualization || context.fpsTracker == null)
            {
                return;
            }

            context.fpsTracker.StopTracking();

            metrics.averageFps = context.fpsTracker.AverageFps;
            metrics.minFps = context.fpsTracker.MinFps;
            metrics.maxFps = context.fpsTracker.MaxFps;
            metrics.totalFrames = context.fpsTracker.TotalFrames;
            metrics.visualizationDurationSeconds = context.fpsTracker.DurationSeconds;
        }

        /// <summary>
        /// Eksportuje wszystkie zebrane metryki do plików JSON i CSV zgodnie z ustawieniami komponentu.
        /// </summary>
        private void ExportResultsIfEnabled()
        {
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            if (exportJson)
            {
                MetricsExporter.ExportToJson($"{exportFilePrefix}_{timestamp}.json", _allMetrics);
            }

            if (exportCsv)
            {
                MetricsExporter.ExportToCsv($"{exportFilePrefix}_{timestamp}.csv", _allMetrics);
            }
        }
    }
}