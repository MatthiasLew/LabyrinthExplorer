using System;
using System.Collections.Generic;
using UnityEngine;

namespace Algorytm.Dane
{
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
        public int randomSeed;
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
