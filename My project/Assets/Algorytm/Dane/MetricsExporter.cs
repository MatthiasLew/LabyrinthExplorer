using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Algorytm.Dane
{
    /// <summary>
    /// Odpowiada za eksport listy metryk algorytmów do plików JSON oraz CSV.
    /// </summary>
    public static class MetricsExporter
    {
        /// <summary>
        /// Pomocniczy kontener wymagany przez JsonUtility do serializacji kolekcji.
        /// </summary>
        [Serializable]
        private class AlgorithmMetricsCollection
        {
            /// <summary>
            /// Lista metryk przeznaczonych do serializacji.
            /// </summary>
            public List<AlgorithmMetrics> items = new();
        }

        /// <summary>
        /// Eksportuje listę metryk do pliku JSON.
        /// </summary>
        /// <param name="fileName">Nazwa pliku wyjściowego.</param>
        /// <param name="metricsList">Lista metryk do zapisania.</param>
        /// <exception cref="ArgumentException">
        /// Rzucany, gdy nazwa pliku jest pusta lub zawiera wyłącznie białe znaki.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy lista metryk ma wartość null.
        /// </exception>
        public static void ExportToJson(string fileName, IReadOnlyList<AlgorithmMetrics> metricsList)
        {
            ValidateArguments(fileName, metricsList);

            var collection = new AlgorithmMetricsCollection();
            collection.items.AddRange(metricsList);

            string json = JsonUtility.ToJson(collection, true);
            string path = GetOutputPath(fileName);

            File.WriteAllText(path, json, Encoding.UTF8);
            Debug.Log($"Metrics exported to JSON: {path}");
        }

        /// <summary>
        /// Eksportuje listę metryk do pliku CSV.
        /// </summary>
        /// <param name="fileName">Nazwa pliku wyjściowego.</param>
        /// <param name="metricsList">Lista metryk do zapisania.</param>
        /// <exception cref="ArgumentException">
        /// Rzucany, gdy nazwa pliku jest pusta lub zawiera wyłącznie białe znaki.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy lista metryk ma wartość null.
        /// </exception>
        public static void ExportToCsv(string fileName, IReadOnlyList<AlgorithmMetrics> metricsList)
        {
            ValidateArguments(fileName, metricsList);

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(
                "AlgorithmName,AlgorithmVersion,TestId,RunIndex,RandomSeed,MazeName,MazeType,MazeWidth,MazeHeight,TotalCells," +
                "StartPosition,FinishPosition,ReachedGoal,FoundOptimalPath,StepsTaken,PathLength,ShortestPossiblePathLength," +
                "PathEfficiency,VisitedCells,RevisitedCells,BacktrackCount,WallHits,DeadEndsEncountered,ExpandedNodes," +
                "ValidMovesConsidered,InvalidMovesConsidered,FrontierMaxSize,Iterations,Generations,RestartCount," +
                "StagnationIterations,BestFitness,AverageFitness,LogicTimeMs,VisualizationTimeMs,TotalRuntimeMs," +
                "AverageIterationTimeMs,MaxIterationTimeMs,ManagedMemoryBeforeBytes,ManagedMemoryAfterBytes," +
                "ManagedPeakMemoryBytes,ManagedMemoryDeltaBytes,ProcessMemoryBeforeBytes,ProcessMemoryAfterBytes," +
                "ProcessPeakMemoryBytes,ProcessMemoryDeltaBytes,AverageFps,MinFps,MaxFps,TotalFrames," +
                "VisualizationDurationSeconds,EndReason,AdditionalInfo");

            foreach (AlgorithmMetrics metrics in metricsList)
            {
                stringBuilder.AppendLine(string.Join(",",
                    Escape(metrics.algorithmName),
                    Escape(metrics.algorithmVersion),
                    Escape(metrics.testId),
                    Format(metrics.runIndex),
                    Format(metrics.randomSeed),
                    Escape(metrics.mazeName),
                    Escape(metrics.mazeType),
                    Format(metrics.mazeWidth),
                    Format(metrics.mazeHeight),
                    Format(metrics.totalCells),
                    Escape(metrics.startPosition.ToString()),
                    Escape(metrics.finishPosition.ToString()),
                    Format(metrics.reachedGoal),
                    Format(metrics.foundOptimalPath),
                    Format(metrics.stepsTaken),
                    Format(metrics.pathLength),
                    Format(metrics.shortestPossiblePathLength),
                    Format(metrics.pathEfficiency),
                    Format(metrics.visitedCells),
                    Format(metrics.revisitedCells),
                    Format(metrics.backtrackCount),
                    Format(metrics.wallHits),
                    Format(metrics.deadEndsEncountered),
                    Format(metrics.expandedNodes),
                    Format(metrics.validMovesConsidered),
                    Format(metrics.invalidMovesConsidered),
                    Format(metrics.frontierMaxSize),
                    Format(metrics.iterations),
                    Format(metrics.generations),
                    Format(metrics.restartCount),
                    Format(metrics.stagnationIterations),
                    Format(metrics.bestFitness),
                    Format(metrics.averageFitness),
                    Format(metrics.logicTimeMs),
                    Format(metrics.visualizationTimeMs),
                    Format(metrics.totalRuntimeMs),
                    Format(metrics.averageIterationTimeMs),
                    Format(metrics.maxIterationTimeMs),
                    Format(metrics.managedMemoryBeforeBytes),
                    Format(metrics.managedMemoryAfterBytes),
                    Format(metrics.managedPeakMemoryBytes),
                    Format(metrics.managedMemoryDeltaBytes),
                    Format(metrics.processMemoryBeforeBytes),
                    Format(metrics.processMemoryAfterBytes),
                    Format(metrics.processPeakMemoryBytes),
                    Format(metrics.processMemoryDeltaBytes),
                    Format(metrics.averageFps),
                    Format(metrics.minFps),
                    Format(metrics.maxFps),
                    Format(metrics.totalFrames),
                    Format(metrics.visualizationDurationSeconds),
                    Escape(metrics.endReason),
                    Escape(metrics.additionalInfo)
                ));
            }

            string path = GetOutputPath(fileName);
            File.WriteAllText(path, stringBuilder.ToString(), Encoding.UTF8);
            Debug.Log($"Metrics exported to CSV: {path}");
        }

        /// <summary>
        /// Formatuje wartość do zapisu CSV z użyciem kultury niezależnej od ustawień systemowych.
        /// </summary>
        /// <typeparam name="T">Typ formatowanej wartości.</typeparam>
        /// <param name="value">Wartość do sformatowania.</param>
        /// <returns>Tekstowa reprezentacja wartości gotowa do zapisu w pliku CSV.</returns>
        private static string Format<T>(T value)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        /// <summary>
        /// Ucieka tekst zgodnie z zasadami formatu CSV.
        /// </summary>
        /// <param name="value">Wartość tekstowa do zapisania.</param>
        /// <returns>Poprawnie ucieknięta wartość tekstowa.</returns>
        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            string escapedValue = value.Replace("\"", "\"\"");
            return $"\"{escapedValue}\"";
        }

        /// <summary>
        /// Waliduje argumenty wejściowe metod eksportujących.
        /// </summary>
        /// <param name="fileName">Nazwa pliku wyjściowego.</param>
        /// <param name="metricsList">Lista metryk do zapisania.</param>
        private static void ValidateArguments(string fileName, IReadOnlyList<AlgorithmMetrics> metricsList)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
            }

            if (metricsList == null)
            {
                throw new ArgumentNullException(nameof(metricsList));
            }
        }

        /// <summary>
        /// Buduje pełną ścieżkę pliku wyjściowego i zapewnia istnienie katalogu docelowego.
        /// </summary>
        /// <param name="fileName">Nazwa pliku wyjściowego.</param>
        /// <returns>Pełna ścieżka zapisu pliku.</returns>
        private static string GetOutputPath(string fileName)
        {
            string path = Path.Combine(Application.persistentDataPath, fileName);
            string directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return path;
        }
    }
}