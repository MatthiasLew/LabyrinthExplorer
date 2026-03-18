using System;
using System.Collections.Generic;
using UnityEngine;

namespace Algorytm.Genetyczny
{
    [Serializable]
    public class Chromosome
    {
        private const float GoalReward = 10000f;
        private const float DistancePenaltyMultiplier = 12f;
        private const float StepPenaltyMultiplier = 0.35f;
        private const float WallHitPenalty = 6f;
        private const float RevisitPenalty = 2.5f;
        private const float BacktrackPenalty = 1.5f;
        private const float DeadEndPenalty = 3f;
        private const float ProgressReward = 1.25f;

        public MoveDirection[] Genes { get; }
        public float Fitness { get; private set; }
        public bool ReachedGoal { get; private set; }
        public int StepsTaken { get; private set; }
        public int BacktrackCount { get; private set; }
        public List<Vector2Int> Path { get; } = new();

        public Vector2Int FinalPosition => Path.Count > 0 ? Path[Path.Count - 1] : Vector2Int.zero;
        public Chromosome(int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Chromosome length must be greater than zero.");
            }

            Genes = new MoveDirection[length];
        }

        public static Chromosome CreateRandom(int length)
        {
            var chromosome = new Chromosome(length);

            for (int i = 0; i < chromosome.Genes.Length; i++)
            {
                chromosome.Genes[i] = (MoveDirection)UnityEngine.Random.Range(0, 4);
            }

            return chromosome;
        }

        public Chromosome Clone()
        {
            var clone = new Chromosome(Genes.Length);

            for (int i = 0; i < Genes.Length; i++)
            {
                clone.Genes[i] = Genes[i];
            }

            clone.Fitness = Fitness;
            clone.ReachedGoal = ReachedGoal;
            clone.StepsTaken = StepsTaken;
            clone.BacktrackCount = BacktrackCount;
            clone.Path.AddRange(Path);

            return clone;
        }

        public void Evaluate(
            MazeGrid maze,
            Vector2Int start,
            Vector2Int finish,
            HashSet<Vector2Int> globallyVisited,
            ref int revisitedCells,
            ref int wallHits,
            ref int deadEnds,
            ref int validMoves,
            ref int invalidMoves)
        {
            if (maze == null)
            {
                throw new ArgumentNullException(nameof(maze));
            }

            if (globallyVisited == null)
            {
                throw new ArgumentNullException(nameof(globallyVisited));
            }

            Fitness = 0f;
            ReachedGoal = false;
            StepsTaken = 0;
            BacktrackCount = 0;
            Path.Clear();

            var localVisited = new HashSet<Vector2Int>();
            Vector2Int currentPosition = start;

            Path.Add(currentPosition);
            localVisited.Add(currentPosition);
            globallyVisited.Add(currentPosition);

            int initialDistance = maze.GetManhattanDistance(start, finish);
            int bestDistance = initialDistance;

            for (int i = 0; i < Genes.Length; i++)
            {
                Vector2Int nextPosition = currentPosition + Genes[i].ToVector();

                if (!maze.IsWalkable(nextPosition))
                {
                    wallHits++;
                    invalidMoves++;
                    Fitness -= WallHitPenalty;
                    continue;
                }

                validMoves++;
                StepsTaken++;
                currentPosition = nextPosition;
                Path.Add(currentPosition);

                int currentDistance = maze.GetManhattanDistance(currentPosition, finish);
                if (currentDistance < bestDistance)
                {
                    Fitness += (bestDistance - currentDistance) * ProgressReward;
                    bestDistance = currentDistance;
                }

                bool wasVisitedGlobally = !globallyVisited.Add(currentPosition);
                bool wasVisitedLocally = !localVisited.Add(currentPosition);

                if (wasVisitedLocally)
                {
                    revisitedCells++;
                    BacktrackCount++;
                    Fitness -= RevisitPenalty;
                    Fitness -= BacktrackPenalty;
                }
                else if (wasVisitedGlobally)
                {
                    revisitedCells++;
                    Fitness -= RevisitPenalty;
                }

                if (maze.GetWalkableNeighborCount(currentPosition) <= 1 && currentPosition != finish)
                {
                    deadEnds++;
                    Fitness -= DeadEndPenalty;
                }

                if (currentPosition == finish)
                {
                    ReachedGoal = true;
                    Fitness += GoalReward;
                    break;
                }
            }

            int distanceToGoal = maze.GetManhattanDistance(currentPosition, finish);

            Fitness -= distanceToGoal * DistancePenaltyMultiplier;
            Fitness -= StepsTaken * StepPenaltyMultiplier;
            Fitness -= BacktrackCount * BacktrackPenalty;

            if (!ReachedGoal && distanceToGoal < initialDistance)
            {
                Fitness += (initialDistance - distanceToGoal) * ProgressReward;
            }
        }
    }
}