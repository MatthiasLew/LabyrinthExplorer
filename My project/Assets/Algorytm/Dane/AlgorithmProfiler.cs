using System;
using System.Diagnostics;

namespace Algorytm.Dane
{
    /// <summary>
    /// Odpowiada za profilowanie działania algorytmu poprzez pomiar czasu wykonania,
    /// czasu iteracji, czasu wizualizacji oraz zużycia pamięci.
    /// </summary>
    public class AlgorithmProfiler
    {
        private readonly Stopwatch _totalStopwatch = new();
        private readonly Stopwatch _iterationStopwatch = new();
        private readonly Stopwatch _visualizationStopwatch = new();

        private double _logicTimeMs;
        private double _visualizationTimeMs;
        private double _totalIterationTimeMs;
        private double _maxIterationTimeMs;
        private int _iterationCount;

        private long _managedMemoryBeforeBytes;
        private long _managedPeakMemoryBytes;

        private long _processMemoryBeforeBytes;
        private long _processPeakMemoryBytes;

        private bool _isVisualizationRunning;

        /// <summary>
        /// Rozpoczyna profilowanie nowego przebiegu algorytmu.
        /// Resetuje poprzedni stan pomiarów, uruchamia czyszczenie pamięci
        /// oraz zapisuje wartości początkowe czasu i pamięci.
        /// </summary>
        public void Begin()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            _logicTimeMs = 0d;
            _visualizationTimeMs = 0d;
            _totalIterationTimeMs = 0d;
            _maxIterationTimeMs = 0d;
            _iterationCount = 0;
            _isVisualizationRunning = false;

            _managedMemoryBeforeBytes = GC.GetTotalMemory(true);
            _managedPeakMemoryBytes = _managedMemoryBeforeBytes;

            _processMemoryBeforeBytes = GetCurrentProcessMemoryBytes();
            _processPeakMemoryBytes = _processMemoryBeforeBytes;

            _totalStopwatch.Restart();
        }

        /// <summary>
        /// Kończy profilowanie bieżącego przebiegu algorytmu.
        /// Zatrzymuje aktywny pomiar wizualizacji, zatrzymuje główny stoper
        /// oraz aktualizuje wartości szczytowego zużycia pamięci.
        /// </summary>
        public void End()
        {
            if (_isVisualizationRunning)
            {
                EndVisualization();
            }

            _totalStopwatch.Stop();
            UpdatePeakMemory();
        }

        /// <summary>
        /// Rozpoczyna pomiar czasu pojedynczej iteracji algorytmu.
        /// </summary>
        public void BeginIteration()
        {
            _iterationStopwatch.Restart();
        }

        /// <summary>
        /// Kończy pomiar czasu pojedynczej iteracji algorytmu
        /// i aktualizuje zbiorcze statystyki iteracyjne.
        /// </summary>
        public void EndIteration()
        {
            _iterationStopwatch.Stop();

            double elapsedMs = _iterationStopwatch.Elapsed.TotalMilliseconds;

            _logicTimeMs += elapsedMs;
            _totalIterationTimeMs += elapsedMs;
            _iterationCount++;

            if (elapsedMs > _maxIterationTimeMs)
            {
                _maxIterationTimeMs = elapsedMs;
            }

            UpdatePeakMemory();
        }

        /// <summary>
        /// Rozpoczyna pomiar czasu poświęconego na wizualizację.
        /// Jeśli pomiar wizualizacji jest już aktywny, metoda nie wykonuje żadnej akcji.
        /// </summary>
        public void BeginVisualization()
        {
            if (_isVisualizationRunning)
            {
                return;
            }

            _visualizationStopwatch.Restart();
            _isVisualizationRunning = true;
        }

        /// <summary>
        /// Kończy pomiar czasu wizualizacji i dodaje jego wartość do sumarycznego czasu wizualizacji.
        /// Jeśli pomiar wizualizacji nie był aktywny, metoda nie wykonuje żadnej akcji.
        /// </summary>
        public void EndVisualization()
        {
            if (!_isVisualizationRunning)
            {
                return;
            }

            _visualizationStopwatch.Stop();
            _visualizationTimeMs += _visualizationStopwatch.Elapsed.TotalMilliseconds;
            _isVisualizationRunning = false;

            UpdatePeakMemory();
        }

        /// <summary>
        /// Uzupełnia przekazany obiekt metryk wartościami zebranymi podczas profilowania.
        /// </summary>
        /// <param name="metrics">Obiekt metryk, który zostanie uzupełniony wynikami pomiaru.</param>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy parametr <paramref name="metrics"/> ma wartość null.
        /// </exception>
        public void FillMetrics(AlgorithmMetrics metrics)
        {
            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            long managedMemoryAfterBytes = GC.GetTotalMemory(false);
            long processMemoryAfterBytes = GetCurrentProcessMemoryBytes();

            metrics.totalRuntimeMs = _totalStopwatch.Elapsed.TotalMilliseconds;
            metrics.logicTimeMs = _logicTimeMs;
            metrics.visualizationTimeMs = _visualizationTimeMs;
            metrics.averageIterationTimeMs = _iterationCount > 0 ? _totalIterationTimeMs / _iterationCount : 0d;
            metrics.maxIterationTimeMs = _maxIterationTimeMs;

            metrics.managedMemoryBeforeBytes = _managedMemoryBeforeBytes;
            metrics.managedMemoryAfterBytes = managedMemoryAfterBytes;
            metrics.managedPeakMemoryBytes = Math.Max(_managedPeakMemoryBytes, managedMemoryAfterBytes);

            metrics.processMemoryBeforeBytes = _processMemoryBeforeBytes;
            metrics.processMemoryAfterBytes = processMemoryAfterBytes;
            metrics.processPeakMemoryBytes = Math.Max(_processPeakMemoryBytes, processMemoryAfterBytes);
        }

        /// <summary>
        /// Aktualizuje zapisane wartości szczytowego zużycia pamięci zarządzanej oraz pamięci procesu.
        /// </summary>
        private void UpdatePeakMemory()
        {
            long currentManagedMemoryBytes = GC.GetTotalMemory(false);
            if (currentManagedMemoryBytes > _managedPeakMemoryBytes)
            {
                _managedPeakMemoryBytes = currentManagedMemoryBytes;
            }

            long currentProcessMemoryBytes = GetCurrentProcessMemoryBytes();
            if (currentProcessMemoryBytes > _processPeakMemoryBytes)
            {
                _processPeakMemoryBytes = currentProcessMemoryBytes;
            }
        }

        /// <summary>
        /// Zwraca bieżące zużycie pamięci procesu w bajtach.
        /// </summary>
        /// <returns>Aktualne zużycie pamięci procesu wyrażone w bajtach.</returns>
        private static long GetCurrentProcessMemoryBytes()
        {
            using Process currentProcess = Process.GetCurrentProcess();
            return currentProcess.WorkingSet64;
        }
    }
}