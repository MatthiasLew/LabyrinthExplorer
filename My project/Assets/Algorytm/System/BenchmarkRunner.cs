using System;
using System.Collections;
using System.Collections.Generic;
using Algorytm.Dane;
using UnityEngine;

namespace Algorytm.System
{
    public class BenchmarkRunner : MonoBehaviour
    {
        [Header("Benchmark Settings")]
        [SerializeField] private bool exportJson = true;
        [SerializeField] private bool exportCsv = true;
        [SerializeField] private string exportFilePrefix = "benchmark_results";

        private readonly List<AlgorithmMetrics> _allMetrics = new();

        public IReadOnlyList<AlgorithmMetrics> AllMetrics => _allMetrics;

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

            var firstSummary = AlgorithmSummary.FromMetrics(firstAlgorithmMetrics);
            var secondSummary = AlgorithmSummary.FromMetrics(secondAlgorithmMetrics);
            var comparisonResult = AlgorithmComparisonResult.Create(firstSummary, secondSummary);

            ExportResultsIfEnabled();

            onCompleted?.Invoke(comparisonResult);
        }

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

        private IEnumerator RunSingleAlgorithm(
            IMazeAlgorithm algorithm,
            MazeAlgorithmContext context,
            int runIndex,
            List<AlgorithmMetrics> targetList)
        {
            AlgorithmMetrics metrics = CreateBaseMetrics(algorithm, context, runIndex);
            AlgorithmProfiler profiler = new AlgorithmProfiler();

            ApplyContextToMetrics(context, metrics);

            PrepareFpsTracking(context);

            profiler.Begin();

            if (context.enableVisualization)
            {
                profiler.BeginVisualization();
            }

            yield return algorithm.Run(context, metrics, profiler);

            if (context.enableVisualization)
            {
                profiler.EndVisualization();
            }

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

        private static AlgorithmMetrics CreateBaseMetrics(
            IMazeAlgorithm algorithm,
            MazeAlgorithmContext context,
            int runIndex)
        {
            return new AlgorithmMetrics
            {
                algorithmName = algorithm.AlgorithmName,
                algorithmVersion = algorithm.AlgorithmVersion,
                testId = $"{algorithm.AlgorithmName}_{DateTime.Now:yyyyMMdd_HHmmss}_{runIndex}",
                runIndex = runIndex,
                randomSeed = context.randomSeed
            };
        }

        private static void ApplyContextToMetrics(MazeAlgorithmContext context, AlgorithmMetrics metrics)
        {
            metrics.mazeName = context.mazeName;
            metrics.mazeType = context.mazeType;
            metrics.mazeWidth = context.mazeWidth;
            metrics.mazeHeight = context.mazeHeight;
            metrics.startPosition = context.startPosition;
            metrics.finishPosition = context.finishPosition;
        }

        private static MazeAlgorithmContext CloneContext(MazeAlgorithmContext sourceContext, int randomSeed)
        {
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
                MazeData = sourceContext.MazeData,
                coroutineHost = sourceContext.coroutineHost,
                fpsTracker = sourceContext.fpsTracker
            };
        }

        private static void PrepareFpsTracking(MazeAlgorithmContext context)
        {
            if (!context.enableVisualization || context.fpsTracker == null)
            {
                return;
            }

            context.fpsTracker.StartTracking();
        }

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

        private void ExportResultsIfEnabled()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

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