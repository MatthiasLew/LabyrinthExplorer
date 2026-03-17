using System;
using UnityEngine;

namespace Algorytm.Dane
{
    [Serializable]
    public class AlgorithmMetrics
    {
        [Header("Algorithm Identity")]
        public string algorithmName;
        public string algorithmVersion;
        public string additionalInfo;
        public string endReason;

        [Header("Test Context")]
        public string testId;
        public int runIndex;
        public int randomSeed;
        public string mazeName;
        public string mazeType;

        [Header("Maze Info")]
        public int mazeWidth;
        public int mazeHeight;
        public int totalCells;
        public Vector2Int startPosition;
        public Vector2Int finishPosition;

        [Header("Result")]
        public bool reachedGoal;
        public bool foundOptimalPath;

        [Header("Path Quality")]
        public int stepsTaken;
        public int pathLength;
        public int shortestPossiblePathLength;
        public float pathEfficiency;

        [Header("Traversal Stats")]
        public int visitedCells;
        public int revisitedCells;
        public int backtrackCount;
        public int wallHits;
        public int deadEndsEncountered;
        public int expandedNodes;
        public int validMovesConsidered;
        public int invalidMovesConsidered;
        public int frontierMaxSize;

        [Header("Iteration Stats")]
        public int iterations;
        public int generations;
        public int restartCount;
        public int stagnationIterations;

        [Header("Fitness / Heuristic Stats")]
        public float bestFitness;
        public float averageFitness;

        [Header("Timing")]
        public double logicTimeMs;
        public double visualizationTimeMs;
        public double totalRuntimeMs;
        public double averageIterationTimeMs;
        public double maxIterationTimeMs;

        [Header("Managed Memory")]
        public long managedMemoryBeforeBytes;
        public long managedMemoryAfterBytes;
        public long managedPeakMemoryBytes;
        public long managedMemoryDeltaBytes;

        [Header("Process Memory")]
        public long processMemoryBeforeBytes;
        public long processMemoryAfterBytes;
        public long processPeakMemoryBytes;
        public long processMemoryDeltaBytes;

        [Header("Visualization")]
        public float averageFps;
        public float minFps;
        public float maxFps;
        public int totalFrames;
        public float visualizationDurationSeconds;

        public void FinalizeDerivedMetrics()
        {
            totalCells = mazeWidth * mazeHeight;

            if (shortestPossiblePathLength > 0 && pathLength > 0)
            {
                pathEfficiency = (float)shortestPossiblePathLength / pathLength;
            }
            else
            {
                pathEfficiency = 0f;
            }

            if (pathEfficiency > 1f)
            {
                pathEfficiency = 1f;
            }

            foundOptimalPath = reachedGoal &&
                               shortestPossiblePathLength > 0 &&
                               pathLength == shortestPossiblePathLength;

            managedMemoryDeltaBytes = managedMemoryAfterBytes - managedMemoryBeforeBytes;
            processMemoryDeltaBytes = processMemoryAfterBytes - processMemoryBeforeBytes;
        }
    }
}