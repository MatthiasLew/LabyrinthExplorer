using System;
using UnityEngine;
using System.Collections.Generic;

namespace Algorytm.Dane
{
    /// <summary>
    /// Pojedynczy czytelny fragment animacji przebiegu algorytmu.
    /// Dla genetycznego jest to nowe najlepsze potomstwo, a dla mrówkowego
    /// reprezentatywna najlepsza mrówka danej iteracji lub mrówka zwycięska.
    /// </summary>
    [Serializable]
    public class AlgorithmReplaySegment
    {
        public int iteration;
        public int agentIndex;
        public bool reachedGoal;
        public List<Vector2Int> path = new();

        public AlgorithmReplaySegment Clone()
        {
            var clone = new AlgorithmReplaySegment
            {
                iteration = iteration,
                agentIndex = agentIndex,
                reachedGoal = reachedGoal
            };

            clone.path.AddRange(path);
            return clone;
        }
    }

    /// <summary>
    /// Przechowuje komplet metryk opisujących pojedyncze uruchomienie algorytmu
    /// wyszukiwania ścieżki w labiryncie.
    /// </summary>
    [Serializable]
    public class AlgorithmMetrics
    {
        /// <summary>
        /// Nazwa badanego algorytmu.
        /// </summary>
        [Header("Algorithm Identity")]
        public string algorithmName;

        /// <summary>
        /// Wersja badanego algorytmu.
        /// </summary>
        public string algorithmVersion;

        /// <summary>
        /// Dodatkowe informacje opisujące konfigurację lub wariant algorytmu.
        /// </summary>
        public string additionalInfo;

        /// <summary>
        /// Powód zakończenia działania algorytmu.
        /// </summary>
        public string endReason;

        /// <summary>
        /// Identyfikator testu lub serii testowej.
        /// </summary>
        [Header("Test Context")]
        public string testId;

        /// <summary>
        /// Indeks bieżącego uruchomienia w ramach serii testów.
        /// </summary>
        public int runIndex;

        /// <summary>
        /// Ziarno generatora liczb losowych użyte podczas testu.
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
        [Header("Maze Info")]
        public int mazeWidth;

        /// <summary>
        /// Wysokość labiryntu w komórkach.
        /// </summary>
        public int mazeHeight;

        /// <summary>
        /// Łączna liczba komórek w labiryncie.
        /// Wartość wyliczana na podstawie szerokości i wysokości.
        /// </summary>
        public int totalCells;

        /// <summary>
        /// Pozycja startowa w labiryncie.
        /// </summary>
        public Vector2Int startPosition;

        /// <summary>
        /// Pozycja końcowa w labiryncie.
        /// </summary>
        public Vector2Int finishPosition;

        /// <summary>
        /// Określa, czy algorytm osiągnął cel.
        /// </summary>
        [Header("Result")]
        public bool reachedGoal;

        /// <summary>
        /// Określa, czy odnaleziona ścieżka była optymalna.
        /// </summary>
        public bool foundOptimalPath;

        /// <summary>
        /// Liczba wykonanych kroków przez algorytm.
        /// </summary>
        [Header("Path Quality")]
        public int stepsTaken;

        /// <summary>
        /// Długość surowej ścieżki, którą sam algorytm doprowadził do mety.
        /// </summary>
        public int pathLength;

        /// <summary>
        /// Długość ścieżki wygładzonej BFS-em wyłącznie w podgrafie odkrytym przez algorytm.
        /// Wartość prezentacyjna; nie zastępuje surowego wyniku algorytmu.
        /// </summary>
        public int optimizedDiscoveredPathLength;

        /// <summary>
        /// Efektywność ścieżki wygładzonej BFS-em po odkryciach algorytmu.
        /// </summary>
        public float optimizedDiscoveredPathEfficiency;

        /// <summary>
        /// Czy BFS po odkrytych komórkach uzyskał globalne minimum labiryntu.
        /// </summary>
        public bool foundOptimalDiscoveredPath;

        /// <summary>
        /// Długość najkrótszej możliwej ścieżki w danym labiryncie.
        /// </summary>
        public int shortestPossiblePathLength;

        /// <summary>
        /// Efektywność ścieżki wyrażona jako stosunek długości optymalnej ścieżki
        /// do długości ścieżki znalezionej przez algorytm.
        /// </summary>
        public float pathEfficiency;

        /// <summary>
        /// Liczba unikalnych odwiedzonych komórek.
        /// </summary>
        [Header("Traversal Stats")]
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
        /// Maksymalny rozmiar struktury frontier w trakcie działania algorytmu.
        /// </summary>
        public int frontierMaxSize;

        /// <summary>
        /// Liczba iteracji wykonanych przez algorytm.
        /// </summary>
        [Header("Iteration Stats")]
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
        [Header("Fitness / Heuristic Stats")]
        public float bestFitness;

        /// <summary>
        /// Średnia wartość funkcji fitness.
        /// </summary>
        public float averageFitness;

        /// <summary>
        /// Czas wykonania logiki algorytmu w milisekundach.
        /// </summary>
        [Header("Timing")]
        public double logicTimeMs;

        /// <summary>
        /// Czas poświęcony na wizualizację w milisekundach.
        /// </summary>
        public double visualizationTimeMs;

        /// <summary>
        /// Całkowity czas wykonania algorytmu w milisekundach.
        /// </summary>
        public double totalRuntimeMs;

        /// <summary>
        /// Średni czas pojedynczej iteracji w milisekundach.
        /// </summary>
        public double averageIterationTimeMs;

        /// <summary>
        /// Maksymalny czas pojedynczej iteracji w milisekundach.
        /// </summary>
        public double maxIterationTimeMs;

        /// <summary>
        /// Ilość pamięci zarządzanej przed rozpoczęciem działania algorytmu.
        /// </summary>
        [Header("Managed Memory")]
        public long managedMemoryBeforeBytes;

        /// <summary>
        /// Ilość pamięci zarządzanej po zakończeniu działania algorytmu.
        /// </summary>
        public long managedMemoryAfterBytes;

        /// <summary>
        /// Szczytowe zużycie pamięci zarządzanej.
        /// </summary>
        public long managedPeakMemoryBytes;

        /// <summary>
        /// Zmiana zużycia pamięci zarządzanej między początkiem a końcem działania.
        /// </summary>
        public long managedMemoryDeltaBytes;

        /// <summary>
        /// Ilość pamięci procesu przed rozpoczęciem działania algorytmu.
        /// </summary>
        [Header("Process Memory")]
        public long processMemoryBeforeBytes;

        /// <summary>
        /// Ilość pamięci procesu po zakończeniu działania algorytmu.
        /// </summary>
        public long processMemoryAfterBytes;

        /// <summary>
        /// Szczytowe zużycie pamięci procesu.
        /// </summary>
        public long processPeakMemoryBytes;

        /// <summary>
        /// Zmiana zużycia pamięci procesu między początkiem a końcem działania.
        /// </summary>
        public long processMemoryDeltaBytes;

        /// <summary>
        /// Średnia liczba klatek na sekundę podczas wizualizacji.
        /// </summary>
        [Header("Visualization")]
        public float averageFps;

        /// <summary>
        /// Minimalna liczba klatek na sekundę podczas wizualizacji.
        /// </summary>
        public float minFps;

        /// <summary>
        /// Maksymalna liczba klatek na sekundę podczas wizualizacji.
        /// </summary>
        public float maxFps;

        /// <summary>
        /// Łączna liczba wyrenderowanych klatek.
        /// </summary>
        public int totalFrames;

        /// <summary>
        /// Czas trwania wizualizacji w sekundach.
        /// </summary>
        public float visualizationDurationSeconds;

        /// <summary>
        /// Wylicza metryki pochodne na podstawie wcześniej zapisanych danych surowych.
        /// </summary>
        /// <remarks>
        /// Metoda powinna zostać wywołana po uzupełnieniu podstawowych pól metryk,
        /// ponieważ nadpisuje wartości wyliczane, takie jak liczba komórek, efektywność ścieżki,
        /// informacja o ścieżce optymalnej oraz delty pamięci.
        /// </remarks>
        public void FinalizeDerivedMetrics()
        {
            totalCells = Math.Max(0, mazeWidth) * Math.Max(0, mazeHeight);

            if (reachedGoal && shortestPossiblePathLength > 0 && pathLength > 0)
            {
                pathEfficiency = (float)shortestPossiblePathLength / pathLength;
                pathEfficiency = Mathf.Clamp01(pathEfficiency);
            }
            else
            {
                pathEfficiency = 0f;
            }

            foundOptimalPath = reachedGoal &&
                               shortestPossiblePathLength > 0 &&
                               pathLength == shortestPossiblePathLength;

            if (reachedGoal && shortestPossiblePathLength > 0 && optimizedDiscoveredPathLength > 0)
            {
                optimizedDiscoveredPathEfficiency =
                    Mathf.Clamp01((float)shortestPossiblePathLength / optimizedDiscoveredPathLength);
            }
            else
            {
                optimizedDiscoveredPathEfficiency = 0f;
            }

            foundOptimalDiscoveredPath = reachedGoal &&
                                         shortestPossiblePathLength > 0 &&
                                         optimizedDiscoveredPathLength == shortestPossiblePathLength;

            managedMemoryDeltaBytes = managedMemoryAfterBytes - managedMemoryBeforeBytes;
            processMemoryDeltaBytes = processMemoryAfterBytes - processMemoryBeforeBytes;
        }
        
        /// <summary>
        /// Pola odkryte przez algorytm w kolejności pierwszego odwiedzenia.
        /// Lista służy do uczciwego odtworzenia eksploracji po zakończeniu pomiaru czasu.
        /// </summary>
        public List<Vector2Int> explorationTrace = new();

        /// <summary>
        /// Segmenty przeznaczone wyłącznie do czytelnego odtworzenia działania algorytmu.
        /// Nie wpływają na czas pomiaru ani na obliczone metryki.
        /// </summary>
        public List<AlgorithmReplaySegment> replaySegments = new();

        /// <summary>
        /// Surowa trasa udanego agenta przed optymalizacją prezentacyjną.
        /// </summary>
        public List<Vector2Int> rawFinalPath = new();

        /// <summary>
        /// Trasa prezentowana na planszy po sukcesie: BFS po komórkach odkrytych przez algorytm.
        /// </summary>
        public List<Vector2Int> finalPath = new();
    }
}