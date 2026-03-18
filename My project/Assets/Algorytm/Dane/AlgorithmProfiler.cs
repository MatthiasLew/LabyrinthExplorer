using System;
using System.Diagnostics;

namespace Algorytm.Dane
{
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

        public void End()
        {
            if (_isVisualizationRunning)
            {
                EndVisualization();
            }

            _totalStopwatch.Stop();
            UpdatePeakMemory();
        }

        public void BeginIteration()
        {
            _iterationStopwatch.Restart();
        }

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

        public void BeginVisualization()
        {
            if (_isVisualizationRunning)
            {
                return;
            }

            _visualizationStopwatch.Restart();
            _isVisualizationRunning = true;
        }

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

        public void FillMetrics(AlgorithmMetrics metrics)
        {
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

        private static long GetCurrentProcessMemoryBytes()
        {
            using Process currentProcess = Process.GetCurrentProcess();
            return currentProcess.WorkingSet64;
        }
    }
}