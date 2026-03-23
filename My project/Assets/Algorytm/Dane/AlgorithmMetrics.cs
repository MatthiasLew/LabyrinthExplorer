using System;
using UnityEngine;

namespace Algorytm.Dane
{
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
        /// Długość odnalezionej ścieżki.
        /// </summary>
        public int pathLength;

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

            managedMemoryDeltaBytes = managedMemoryAfterBytes - managedMemoryBeforeBytes;
            processMemoryDeltaBytes = processMemoryAfterBytes - processMemoryBeforeBytes;
        }
    }
}