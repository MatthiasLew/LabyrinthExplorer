using System;
using System.Collections.Generic;
using UnityEngine;

namespace Algorytm.Dane
{
    /// <summary>
    /// Defines what a benchmark run is expected to optimize.
    /// Value zero is deliberately the fair-comparison default for existing Unity scenes.
    /// </summary>
    public enum BenchmarkObjective
    {
        OptimizePathWithinBudget = 0,
        FindFirstSolution = 1
    }

    /// <summary>
    /// Holds input data and callbacks required to run maze algorithms.
    /// </summary>
    [Serializable]
    public class MazeAlgorithmContext
    {
        public string mazeName;
        public string mazeType;
        public int mazeWidth;
        public int mazeHeight;
        public Vector2Int startPosition;
        public Vector2Int finishPosition;

        // Reproducibility: the map and stochastic algorithm have separate seeds.
        public int mazeSeed;
        public string mazeLayoutHash;
        public int randomSeed;

        // Hard safety limits make heuristic runs terminate predictably.
        public BenchmarkObjective objective = BenchmarkObjective.OptimizePathWithinBudget;
        public int maxIterations = 500;
        public int maxCandidateEvaluations = 20000;
        public double maxRuntimeMs = 10000d;

        public bool enableVisualization;
        public float stepDelaySeconds;
        public object mazeData;
        public MonoBehaviour coroutineHost;
        public FpsTracker fpsTracker;

        // Newer visualization callbacks used by MazeAppController.
        public Action<string, int> onAlgorithmRunStarted;
        public Action<string, int> onAlgorithmRunCompleted;
        public Action<Vector2Int> onVisualizationStep;

        // Legacy/alternate callbacks kept for compatibility with merged branches.
        public Action<Vector2Int> onCellVisited;
        public Action<Vector2Int> onCurrentCellChanged;
        public Action<IReadOnlyList<Vector2Int>> onFinalPathFound;

        public T GetMazeData<T>() where T : class
        {
            return mazeData as T;
        }
    }
}
