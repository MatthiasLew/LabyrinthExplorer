using System;
using UnityEngine;

namespace Algorytm.Dane
{
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

        public object MazeData;
        public MonoBehaviour coroutineHost;
        public FpsTracker fpsTracker;

        public T GetMazeData<T>() where T : class
        {
            return MazeData as T;
        }
    }
}