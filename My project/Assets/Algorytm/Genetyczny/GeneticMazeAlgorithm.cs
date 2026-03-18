using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Algorytm.Dane;
using Algorytm.System;
using UnityEngine;

namespace Algorytm.Genetyczny
{
    public class GeneticMazeAlgorithm : IMazeAlgorithm
    {
        public string AlgorithmName => "Genetic Algorithm";
        public string AlgorithmVersion => "1.0.0";

        private const int PopulationSize = 50;
        private const int MaxGenerations = 200;
        private const float MutationChance = 0.08f;
        private const int ChromosomeLength = 128;
        private const int TournamentSize = 3;
        private const int MaxStagnationGenerations = 30;

        public IEnumerator Run(
            MazeAlgorithmContext context,
            AlgorithmMetrics metrics,
            AlgorithmProfiler profiler)
        {
            MazeGrid maze = context.GetMazeData<MazeGrid>();
            if (maze == null)
            {
                metrics.reachedGoal = false;
                metrics.endReason = "MissingMazeData";
                metrics.additionalInfo = "MazeData is null or has invalid type.";
                yield break;
            }
            if (!maze.IsWalkable(context.startPosition) || !maze.IsWalkable(context.finishPosition))
            {
                metrics.reachedGoal = false;
                metrics.endReason = "InvalidStartOrFinish";
                metrics.additionalInfo = "Start or finish position is not walkable.";
                yield break;
            }

            Random.InitState(context.randomSeed);

            var result = new MazeAlgorithmResult();
            var globallyVisited = new HashSet<Vector2Int>();
            int revisitedCells = 0;
            int wallHits = 0;
            int deadEnds = 0;
            int validMoves = 0;
            int invalidMoves = 0;
            int bestGeneration = 0;

            List<Chromosome> population = CreateInitialPopulation();

            float bestFitnessEver = float.MinValue;
            Chromosome bestChromosomeEver = null;
            int stagnationCounter = 0;

            for (int generation = 0; generation < MaxGenerations; generation++)
            {
                profiler.BeginIteration();

                EvaluatePopulation(
                    population,
                    maze,
                    context.startPosition,
                    context.finishPosition,
                    globallyVisited,
                    ref revisitedCells,
                    ref wallHits,
                    ref deadEnds,
                    ref validMoves,
                    ref invalidMoves);

                population = population
                    .OrderByDescending(chromosome => chromosome.Fitness)
                    .ToList();

                Chromosome bestOfGeneration = population[0];
                float averageFitness = population.Average(chromosome => chromosome.Fitness);

                result.generations = generation + 1;
                result.iterations = generation + 1;
                result.bestFitness = bestOfGeneration.Fitness;
                result.averageFitness = averageFitness;
                result.frontierMaxSize = Mathf.Max(result.frontierMaxSize, population.Count);

                if (bestOfGeneration.Fitness > bestFitnessEver)
                {
                    bestFitnessEver = bestOfGeneration.Fitness;
                    bestChromosomeEver = bestOfGeneration.Clone();
                    bestGeneration = generation;
                    stagnationCounter = 0;
                }
                else
                {
                    stagnationCounter++;
                }

                if (bestOfGeneration.ReachedGoal)
                {
                    FillResultFromChromosome(result, bestOfGeneration, maze, context);
                    result.stagnationIterations = stagnationCounter;
                    result.revisitedCells = revisitedCells;
                    result.wallHits = wallHits;
                    result.deadEndsEncountered = deadEnds;
                    result.validMovesConsidered = validMoves;
                    result.invalidMovesConsidered = invalidMoves;
                    result.visitedCells = globallyVisited.Count;
                    result.endReason = "GoalReached";
                    result.additionalInfo =
                        $"Population={PopulationSize}; Mutation={MutationChance}; BestGeneration={generation + 1}";
                    profiler.EndIteration();
                    result.ApplyTo(metrics);
                    yield break;
                }

                if (stagnationCounter >= MaxStagnationGenerations)
                {
                    result.restartCount++;
                    population = CreateInitialPopulation();
                    stagnationCounter = 0;

                    profiler.EndIteration();

                    if (context.enableVisualization && context.stepDelaySeconds > 0f)
                    {
                        profiler.BeginVisualization();
                        yield return new WaitForSeconds(context.stepDelaySeconds);
                        profiler.EndVisualization();
                    }
                    else
                    {
                        yield return null;
                    }

                    continue;
                }

                population = CreateNextGeneration(population);

                profiler.EndIteration();

                if (context.enableVisualization && context.stepDelaySeconds > 0f)
                {
                    yield return new WaitForSeconds(context.stepDelaySeconds);
                }
            }

            if (bestChromosomeEver != null)
            {
                FillResultFromChromosome(result, bestChromosomeEver, maze, context);
            }

            result.reachedGoal = false;
            result.endReason = "MaxGenerationsReached";
            result.stagnationIterations = stagnationCounter;
            result.revisitedCells = revisitedCells;
            result.wallHits = wallHits;
            result.deadEndsEncountered = deadEnds;
            result.validMovesConsidered = validMoves;
            result.invalidMovesConsidered = invalidMoves;
            result.visitedCells = globallyVisited.Count;
            result.additionalInfo =
                $"Population={PopulationSize}; Mutation={MutationChance}; BestGeneration={bestGeneration + 1}";

            result.ApplyTo(metrics);
        }

        private static List<Chromosome> CreateInitialPopulation()
        {
            var population = new List<Chromosome>(PopulationSize);

            for (int i = 0; i < PopulationSize; i++)
            {
                population.Add(Chromosome.CreateRandom(ChromosomeLength));
            }

            return population;
        }

        private static void EvaluatePopulation(
            List<Chromosome> population,
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
            foreach (Chromosome chromosome in population)
            {
                chromosome.Evaluate(
                    maze,
                    start,
                    finish,
                    globallyVisited,
                    ref revisitedCells,
                    ref wallHits,
                    ref deadEnds,
                    ref validMoves,
                    ref invalidMoves);
            }
        }

        private static List<Chromosome> CreateNextGeneration(List<Chromosome> sortedPopulation)
        {
            var nextPopulation = new List<Chromosome>(PopulationSize)
            {
                sortedPopulation[0].Clone(),
                sortedPopulation[1].Clone()
            };

            while (nextPopulation.Count < PopulationSize)
            {
                Chromosome parentA = TournamentSelect(sortedPopulation);
                Chromosome parentB = TournamentSelect(sortedPopulation);

                Chromosome child = Crossover(parentA, parentB);
                Mutate(child);

                nextPopulation.Add(child);
            }

            return nextPopulation;
        }

        private static Chromosome TournamentSelect(List<Chromosome> population)
        {
            Chromosome best = null;

            for (int i = 0; i < TournamentSize; i++)
            {
                Chromosome candidate = population[Random.Range(0, population.Count)];
                if (best == null || candidate.Fitness > best.Fitness)
                {
                    best = candidate;
                }
            }

            return best;
        }

        private static Chromosome Crossover(Chromosome a, Chromosome b)
        {
            var child = new Chromosome(a.Genes.Length);
            int splitIndex = Random.Range(1, a.Genes.Length - 1);

            for (int i = 0; i < a.Genes.Length; i++)
            {
                child.Genes[i] = i < splitIndex ? a.Genes[i] : b.Genes[i];
            }

            return child;
        }

        private static void Mutate(Chromosome chromosome)
        {
            for (int i = 0; i < chromosome.Genes.Length; i++)
            {
                if (Random.value < MutationChance)
                {
                    chromosome.Genes[i] = (MoveDirection)Random.Range(0, 4);
                }
            }
        }

        private static void FillResultFromChromosome(
            MazeAlgorithmResult result,
            Chromosome chromosome,
            MazeGrid maze,
            MazeAlgorithmContext context)
        {
            result.reachedGoal = chromosome.ReachedGoal;
            result.stepsTaken = chromosome.StepsTaken;
            result.pathLength = chromosome.Path.Count;
            result.shortestPossiblePathLength = maze.GetShortestPathLength(
                context.startPosition,
                context.finishPosition);

            result.expandedNodes = chromosome.Path.Count;
            result.backtrackCount = chromosome.BacktrackCount;
        }
    }
}