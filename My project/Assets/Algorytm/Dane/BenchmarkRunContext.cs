using System;
using UnityEngine;

namespace Algorytm.Dane
{
    [Serializable]
    public class BenchmarkRunContext
    {
        public string testId;
        public int runIndex;
        public int randomSeed;
        public string mazeName;
        public string mazeType;

        public int mazeWidth;
        public int mazeHeight;
        public Vector2Int startPosition;
        public Vector2Int finishPosition;

        public static BenchmarkRunContext Create(
            string algorithmTestPrefix,
            int runIndex,
            int randomSeed,
            string mazeName,
            string mazeType,
            int mazeWidth,
            int mazeHeight,
            Vector2Int startPosition,
            Vector2Int finishPosition)
        {
            return new BenchmarkRunContext
            {
                testId = $"{algorithmTestPrefix}_{DateTime.Now:yyyyMMdd_HHmmss}_{runIndex}",
                runIndex = runIndex,
                randomSeed = randomSeed,
                mazeName = mazeName,
                mazeType = mazeType,
                mazeWidth = mazeWidth,
                mazeHeight = mazeHeight,
                startPosition = startPosition,
                finishPosition = finishPosition
            };
        }

        public void ApplyTo(AlgorithmMetrics metrics)
        {
            metrics.testId = testId;
            metrics.runIndex = runIndex;
            metrics.randomSeed = randomSeed;
            metrics.mazeName = mazeName;
            metrics.mazeType = mazeType;
            metrics.mazeWidth = mazeWidth;
            metrics.mazeHeight = mazeHeight;
            metrics.startPosition = startPosition;
            metrics.finishPosition = finishPosition;
        }
    }
}