using System;
using System.Collections.Generic;
using UnityEngine;

namespace Algorytm.Dane
{
    /// <summary>
    /// Przechowuje wynik pojedynczego uruchomienia algorytmu wyszukiwania ścieżki
    /// przed przepisaniem danych do obiektu metryk benchmarku.
    /// </summary>
    [Serializable]
    public class MazeAlgorithmResult
    {
        /// <summary>
        /// Określa, czy algorytm osiągnął cel.
        /// </summary>
        public bool reachedGoal;

        /// <summary>
        /// Powód zakończenia działania algorytmu.
        /// </summary>
        public string endReason;

        /// <summary>
        /// Liczba wykonanych kroków.
        /// </summary>
        public int stepsTaken;

        /// <summary>
        /// Długość surowej ścieżki, którą agent doprowadził do mety.
        /// </summary>
        public int pathLength;

        /// <summary>
        /// Długość trasy BFS ograniczonej do komórek odkrytych przez ten algorytm.
        /// </summary>
        public int optimizedDiscoveredPathLength;

        /// <summary>
        /// Długość najkrótszej możliwej ścieżki w danym labiryncie.
        /// </summary>
        public int shortestPossiblePathLength;

        /// <summary>
        /// Liczba unikalnych odwiedzonych komórek.
        /// </summary>
        public int visitedCells;

        /// <summary>
        /// Liczba ponownych odwiedzeń wcześniej odwiedzonych komórek.
        /// </summary>
        public int revisitedCells;

        /// <summary>
        /// Liczba wykonanych nawrotów.
        /// </summary>
        public int backtrackCount;

        /// <summary>
        /// Liczba prób wejścia w ścianę lub niedozwolone pole.
        /// </summary>
        public int wallHits;

        /// <summary>
        /// Liczba napotkanych ślepych zaułków.
        /// </summary>
        public int deadEndsEncountered;

        /// <summary>
        /// Liczba rozwiniętych węzłów podczas przeszukiwania.
        /// </summary>
        public int expandedNodes;

        /// <summary>
        /// Liczba rozważonych poprawnych ruchów.
        /// </summary>
        public int validMovesConsidered;

        /// <summary>
        /// Liczba rozważonych niepoprawnych ruchów.
        /// </summary>
        public int invalidMovesConsidered;

        /// <summary>
        /// Maksymalny rozmiar struktury frontier podczas działania algorytmu.
        /// </summary>
        public int frontierMaxSize;

        /// <summary>
        /// Liczba iteracji wykonanych przez algorytm.
        /// </summary>
        public int iterations;

        /// <summary>
        /// Liczba generacji wykonanych przez algorytm genetyczny.
        /// </summary>
        public int generations;

        /// <summary>
        /// Liczba restartów algorytmu.
        /// </summary>
        public int restartCount;

        /// <summary>
        /// Liczba iteracji bez poprawy wyniku.
        /// </summary>
        public int stagnationIterations;

        /// <summary>
        /// Najlepsza osiągnięta wartość funkcji fitness.
        /// </summary>
        public float bestFitness;

        /// <summary>
        /// Średnia wartość funkcji fitness.
        /// </summary>
        public float averageFitness;

        /// <summary>
        /// Dodatkowe informacje opisujące przebieg działania algorytmu.
        /// </summary>
        public string additionalInfo;

        /// <summary>
        /// Przepisuje dane wyniku algorytmu do obiektu metryk benchmarku.
        /// </summary>
        /// <param name="metrics">Obiekt metryk, który zostanie uzupełniony danymi wyniku.</param>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy parametr <paramref name="metrics"/> ma wartość null.
        /// </exception>
        
        /// <summary>
        /// Pola odkryte podczas wyszukiwania, zapisane w kolejności pierwszego odwiedzenia.
        /// </summary>
        public List<Vector2Int> explorationTrace = new();

        /// <summary>
        /// Czytelne segmenty animacji: najlepsze potomstwa albo reprezentatywne mrówki.
        /// </summary>
        public List<AlgorithmReplaySegment> replaySegments = new();

        /// <summary>
        /// Surowa ścieżka agenta, który osiągnął cel.
        /// </summary>
        public List<Vector2Int> rawFinalPath = new();

        /// <summary>
        /// Trasa prezentowana po sukcesie: BFS ograniczony do odkryć algorytmu.
        /// </summary>
        public List<Vector2Int> finalPath = new();
        
        public void ApplyTo(AlgorithmMetrics metrics)
        {
            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            metrics.reachedGoal = reachedGoal;
            metrics.endReason = endReason;

            metrics.stepsTaken = stepsTaken;
            metrics.pathLength = pathLength;
            metrics.optimizedDiscoveredPathLength = optimizedDiscoveredPathLength;
            metrics.shortestPossiblePathLength = shortestPossiblePathLength;

            metrics.visitedCells = visitedCells;
            metrics.revisitedCells = revisitedCells;
            metrics.backtrackCount = backtrackCount;
            metrics.wallHits = wallHits;
            metrics.deadEndsEncountered = deadEndsEncountered;
            metrics.expandedNodes = expandedNodes;
            metrics.validMovesConsidered = validMovesConsidered;
            metrics.invalidMovesConsidered = invalidMovesConsidered;
            metrics.frontierMaxSize = frontierMaxSize;

            metrics.iterations = iterations;
            metrics.generations = generations;
            metrics.restartCount = restartCount;
            metrics.stagnationIterations = stagnationIterations;

            metrics.bestFitness = bestFitness;
            metrics.averageFitness = averageFitness;
            metrics.additionalInfo = additionalInfo;

            metrics.explorationTrace = explorationTrace != null
                ? new List<Vector2Int>(explorationTrace)
                : new List<Vector2Int>();

            metrics.replaySegments = new List<AlgorithmReplaySegment>();
            if (replaySegments != null)
            {
                for (int i = 0; i < replaySegments.Count; i++)
                {
                    if (replaySegments[i] != null)
                    {
                        metrics.replaySegments.Add(replaySegments[i].Clone());
                    }
                }
            }
            
            metrics.rawFinalPath = rawFinalPath != null
                ? new List<Vector2Int>(rawFinalPath)
                : new List<Vector2Int>();

            metrics.finalPath = finalPath != null
                ? new List<Vector2Int>(finalPath)
                : new List<Vector2Int>();
        }
    }
}