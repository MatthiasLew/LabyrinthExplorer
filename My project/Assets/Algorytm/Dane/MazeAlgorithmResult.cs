using System;

namespace Algorytm.Dane
{
    [Serializable]
    public class MazeAlgorithmResult
    {
        public bool reachedGoal;
        public string endReason;

        public int stepsTaken;
        public int pathLength;
        public int shortestPossiblePathLength;

        public int visitedCells;
        public int revisitedCells;
        public int backtrackCount;
        public int wallHits;
        public int deadEndsEncountered;
        public int expandedNodes;
        public int validMovesConsidered;
        public int invalidMovesConsidered;
        public int frontierMaxSize;

        public int iterations;
        public int generations;
        public int restartCount;
        public int stagnationIterations;

        public float bestFitness;
        public float averageFitness;

        public string additionalInfo;

        public void ApplyTo(AlgorithmMetrics metrics)
        {
            metrics.reachedGoal = reachedGoal;
            metrics.endReason = endReason;
            metrics.stepsTaken = stepsTaken;
            metrics.pathLength = pathLength;
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
        }
    }
}