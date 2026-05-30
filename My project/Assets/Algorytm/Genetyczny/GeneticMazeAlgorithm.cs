using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Algorytm.Dane;
using Algorytm.System;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Algorytm.Genetyczny
{
    /// <summary>
    /// Implementuje algorytm genetyczny wyszukujący ścieżkę od punktu startowego do punktu końcowego w labiryncie.
    /// </summary>
    public class GeneticMazeAlgorithm : IMazeAlgorithm
    {
        /// <summary>
        /// Nazwa algorytmu prezentowana w metrykach oraz wynikach benchmarku.
        /// </summary>
        public string AlgorithmName => "Genetic Algorithm";

        /// <summary>
        /// Wersja implementacji algorytmu.
        /// </summary>
        public string AlgorithmVersion => "1.1.0";

        private const int PopulationSize = 50;
        private const float MutationChance = 0.08f;
        private const int MinimumChromosomeLength = 128;
        private const int TournamentSize = 3;
        private const int MaxStagnationGenerations = 30;
        private const int MaxReplaySegments = 20;

        /// <summary>
        /// Uruchamia algorytm genetyczny dla zadanego kontekstu i zapisuje wynik do przekazanego obiektu metryk.
        /// </summary>
        /// <param name="context">Kontekst uruchomienia algorytmu.</param>
        /// <param name="metrics">Obiekt metryk, który zostanie uzupełniony wynikiem działania.</param>
        /// <param name="profiler">Profiler odpowiedzialny za pomiar czasu i pamięci.</param>
        /// <returns>Korutyna wykonująca kolejne generacje algorytmu.</returns>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy parametr <paramref name="context"/>, <paramref name="metrics"/> lub <paramref name="profiler"/> ma wartość null.
        /// </exception>
        public IEnumerator Run(
            MazeAlgorithmContext context,
            AlgorithmMetrics metrics,
            AlgorithmProfiler profiler)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            if (profiler == null)
            {
                throw new ArgumentNullException(nameof(profiler));
            }

            MazeGrid maze = context.GetMazeData<MazeGrid>();
            var result = new MazeAlgorithmResult();

            if (maze == null)
            {
                result.reachedGoal = false;
                result.endReason = "MissingMazeData";
                result.additionalInfo = "Maze data is null or has an invalid type.";
                result.ApplyTo(metrics);
                yield break;
            }

            if (!maze.IsWalkable(context.startPosition) || !maze.IsWalkable(context.finishPosition))
            {
                result.reachedGoal = false;
                result.endReason = "InvalidStartOrFinish";
                result.additionalInfo = "Start or finish position is not walkable.";
                result.ApplyTo(metrics);
                yield break;
            }

            // Nie pobieramy trasy BFS przed działaniem algorytmu.
            // Budżet ruchów zależy wyłącznie od rozmiaru mapy.
            int moveBudget = Mathf.Max(
                MinimumChromosomeLength,
                maze.Width * maze.Height * 2);

            var rng = new global::System.Random(context.randomSeed);
            int maxGenerations = context.maxIterations > 0 ? context.maxIterations : 500;
            double maxRuntimeMs = context.maxRuntimeMs > 0d ? context.maxRuntimeMs : 10000d;
            var runTimer = Stopwatch.StartNew();

            var globallyVisited = new HashSet<Vector2Int>();
            // Do odtwarzania i końcowego BFS dopuszczamy tylko ścieżki najlepszych
            // potomków, które użytkownik faktycznie zobaczy na ekranie.
            var presentedDiscoveredCells = new HashSet<Vector2Int>();
            var explorationTrace = new List<Vector2Int>();
            var visualizedCells = context.enableVisualization ? new HashSet<Vector2Int>() : null;
            int revisitedCells = 0;
            int wallHits = 0;
            int deadEnds = 0;
            int validMoves = 0;
            int invalidMoves = 0;
            int bestGenerationIndex = -1;

            List<Chromosome> population = CreateInitialPopulation(moveBudget, rng);

            float bestFitnessEver = float.MinValue;
            int stagnationCounter = 0;

            // Przebieg jest ograniczony czasowo i iteracyjnie, aby benchmark zawsze kończył się deterministycznie.
            for (int generation = 0; generation < maxGenerations; generation++)
            {
                if (runTimer.Elapsed.TotalMilliseconds >= maxRuntimeMs)
                {
                    result.endReason = "Timeout";
                    break;
                }

                profiler.BeginIteration();

                EvaluatePopulation(
                    population,
                    maze,
                    context.startPosition,
                    context.finishPosition,
                    globallyVisited,
                    null,
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

                if (context.enableVisualization && context.onVisualizationStep != null)
                {
                    EmitNewVisitedCells(globallyVisited, visualizedCells, context.onVisualizationStep);
                    EmitPath(context.onVisualizationStep, bestOfGeneration.Path);
                }

                result.generations = generation + 1;
                result.iterations = generation + 1;
                result.bestFitness = bestOfGeneration.Fitness;
                result.averageFitness = averageFitness;
                result.frontierMaxSize = Mathf.Max(result.frontierMaxSize, population.Count);

                bool newBestOffspring = bestOfGeneration.Fitness > bestFitnessEver;
                if (newBestOffspring)
                {
                    bestFitnessEver = bestOfGeneration.Fitness;
                    bestGenerationIndex = generation;
                    stagnationCounter = 0;

                    if (result.replaySegments.Count < MaxReplaySegments || bestOfGeneration.ReachedGoal)
                    {
                        AddReplaySegment(
                            result.replaySegments,
                            bestOfGeneration.Path,
                            generation + 1,
                            1,
                            bestOfGeneration.ReachedGoal);
                        AddPresentedDiscovery(
                            bestOfGeneration.Path,
                            presentedDiscoveredCells,
                            explorationTrace);
                    }
                }
                else
                {
                    stagnationCounter++;
                }

                if (bestOfGeneration.ReachedGoal)
                {
                    if (!newBestOffspring)
                    {
                        AddReplaySegment(
                            result.replaySegments,
                            bestOfGeneration.Path,
                            generation + 1,
                            1,
                            true);
                        AddPresentedDiscovery(
                            bestOfGeneration.Path,
                            presentedDiscoveredCells,
                            explorationTrace);
                    }

                    if (context.enableVisualization && context.onVisualizationStep != null)
                    {
                        EmitPath(context.onVisualizationStep, bestOfGeneration.Path);
                    }

                    FillResultFromChromosome(result, bestOfGeneration, maze, context, presentedDiscoveredCells);
                    FillSharedResultStats(
                        result,
                        globallyVisited,
                        revisitedCells,
                        wallHits,
                        deadEnds,
                        validMoves,
                        invalidMoves,
                        stagnationCounter,
                        generation,
                        "GoalReached_OptimizedWithinGeneticDiscovery");

                    result.explorationTrace.Clear();
                    result.explorationTrace.AddRange(explorationTrace);

                    profiler.EndIteration();
                    result.ApplyTo(metrics);
                    yield break;
                }

                if (stagnationCounter >= MaxStagnationGenerations)
                {
                    result.restartCount++;
                    population = CreateInitialPopulation(moveBudget, rng);
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

                population = CreateNextGeneration(population, rng);

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
            }

            result.reachedGoal = false;
            result.finalPath.Clear();
            result.rawFinalPath.Clear();
            result.pathLength = 0;
            result.optimizedDiscoveredPathLength = 0;
            result.explorationTrace.Clear();
            result.explorationTrace.AddRange(explorationTrace);

            string failureReason = string.IsNullOrWhiteSpace(result.endReason)
                ? "MaxGenerationsReached"
                : result.endReason;

            FillSharedResultStats(
                result,
                globallyVisited,
                revisitedCells,
                wallHits,
                deadEnds,
                validMoves,
                invalidMoves,
                stagnationCounter,
                bestGenerationIndex,
                failureReason);

            result.additionalInfo += $"; MaxGenerations={maxGenerations}; MaxRuntimeMs={maxRuntimeMs:F0}";
            result.ApplyTo(metrics);
        }

        private static void EmitNewVisitedCells (
            HashSet<Vector2Int> globallyVisited,
            HashSet<Vector2Int> visualizedCells,
            Action<Vector2Int> onStep)
        {
            if (globallyVisited == null || visualizedCells == null || onStep == null)
            {
                return;
            }

            foreach (Vector2Int position in globallyVisited)
            {
                if (visualizedCells.Add(position))
                {
                    onStep(position);
                }
            }
        }

        private static void EmitPath(Action<Vector2Int> onStep, List<Vector2Int> path)
        {
            if (onStep == null || path == null)
            {
                return;
            }

            for (int i = 0; i < path.Count; i++)
            {
                onStep(path[i]);
            }
        }

        private static void AddReplaySegment(
            List<AlgorithmReplaySegment> segments,
            IReadOnlyList<Vector2Int> path,
            int generation,
            int offspringIndex,
            bool reachedGoal)
        {
            if (segments == null || path == null || path.Count == 0)
            {
                return;
            }

            var segment = new AlgorithmReplaySegment
            {
                iteration = generation,
                agentIndex = offspringIndex,
                reachedGoal = reachedGoal
            };
            segment.path.AddRange(path);
            segments.Add(segment);
        }

        private static void AddPresentedDiscovery(
            IReadOnlyList<Vector2Int> path,
            HashSet<Vector2Int> discoveredCells,
            List<Vector2Int> explorationTrace)
        {
            if (path == null || discoveredCells == null || explorationTrace == null)
            {
                return;
            }

            for (int i = 0; i < path.Count; i++)
            {
                if (discoveredCells.Add(path[i]))
                {
                    explorationTrace.Add(path[i]);
                }
            }
        }

        /// <summary>
        /// Tworzy początkową populację losowych chromosomów.
        /// </summary>
        /// <returns>Lista chromosomów o rozmiarze równym konfiguracji populacji.</returns>
        private static List<Chromosome> CreateInitialPopulation(int chromosomeLength, global::System.Random rng)
        {
            var population = new List<Chromosome>(PopulationSize);

            for (int i = 0; i < PopulationSize; i++)
            {
                population.Add(Chromosome.CreateRandom(chromosomeLength, rng));
            }

            return population;
        }

        /// <summary>
        /// Ocenia wszystkie chromosomy w populacji i aktualizuje zbiorcze statystyki przebiegu.
        /// </summary>
        /// <param name="population">Populacja do oceny.</param>
        /// <param name="maze">Labirynt używany podczas ewaluacji.</param>
        /// <param name="start">Pozycja początkowa.</param>
        /// <param name="finish">Pozycja docelowa.</param>
        /// <param name="globallyVisited">Zbiór globalnie odwiedzonych pól.</param>
        /// <param name="revisitedCells">Licznik ponownych odwiedzeń pól.</param>
        /// <param name="wallHits">Licznik prób wejścia w ścianę.</param>
        /// <param name="deadEnds">Licznik napotkanych ślepych zaułków.</param>
        /// <param name="validMoves">Licznik poprawnych ruchów.</param>
        /// <param name="invalidMoves">Licznik niepoprawnych ruchów.</param>
        private static void EvaluatePopulation(
            List<Chromosome> population,
            MazeGrid maze,
            Vector2Int start,
            Vector2Int finish,
            HashSet<Vector2Int> globallyVisited,
            List<Vector2Int> explorationTrace,
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
                    explorationTrace,
                    ref revisitedCells,
                    ref wallHits,
                    ref deadEnds,
                    ref validMoves,
                    ref invalidMoves);
            }
        }

        /// <summary>
        /// Tworzy kolejną generację populacji na podstawie posortowanej populacji bieżącej.
        /// </summary>
        /// <param name="sortedPopulation">Populacja posortowana malejąco według fitness.</param>
        /// <returns>Nowa populacja.</returns>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy parametr <paramref name="sortedPopulation"/> ma wartość null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Rzucany, gdy populacja zawiera mniej niż dwóch osobników.
        /// </exception>
        private static List<Chromosome> CreateNextGeneration(List<Chromosome> sortedPopulation, global::System.Random rng)
        {
            if (sortedPopulation == null)
            {
                throw new ArgumentNullException(nameof(sortedPopulation));
            }

            if (sortedPopulation.Count < 2)
            {
                throw new ArgumentException("Population must contain at least two chromosomes.", nameof(sortedPopulation));
            }

            var nextPopulation = new List<Chromosome>(PopulationSize)
            {
                sortedPopulation[0].Clone(),
                sortedPopulation[1].Clone()
            };

            while (nextPopulation.Count < PopulationSize)
            {
                Chromosome parentA = TournamentSelect(sortedPopulation, rng);
                Chromosome parentB = TournamentSelect(sortedPopulation, rng);

                Chromosome child = Crossover(parentA, parentB, rng);
                Mutate(child, rng);

                nextPopulation.Add(child);
            }

            return nextPopulation;
        }

        /// <summary>
        /// Wybiera osobnika metodą turniejową.
        /// </summary>
        /// <param name="population">Populacja źródłowa.</param>
        /// <returns>Najlepszy osobnik wylosowany w turnieju.</returns>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy parametr <paramref name="population"/> ma wartość null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Rzucany, gdy populacja jest pusta.
        /// </exception>
        private static Chromosome TournamentSelect(List<Chromosome> population, global::System.Random rng)
        {
            if (population == null)
            {
                throw new ArgumentNullException(nameof(population));
            }

            if (population.Count == 0)
            {
                throw new ArgumentException("Population cannot be empty.", nameof(population));
            }

            Chromosome best = null;

            for (int i = 0; i < TournamentSize; i++)
            {
                Chromosome candidate = population[rng.Next(0, population.Count)];
                if (best == null || candidate.Fitness > best.Fitness)
                {
                    best = candidate;
                }
            }

            return best;
        }

        /// <summary>
        /// Tworzy potomka przez jednopunktowe krzyżowanie dwóch rodziców.
        /// </summary>
        /// <param name="a">Pierwszy rodzic.</param>
        /// <param name="b">Drugi rodzic.</param>
        /// <returns>Nowy chromosom potomny.</returns>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy którykolwiek rodzic ma wartość null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Rzucany, gdy rodzice mają różne długości chromosomów.
        /// </exception>
        private static Chromosome Crossover(Chromosome a, Chromosome b, global::System.Random rng)
        {
            if (a == null)
            {
                throw new ArgumentNullException(nameof(a));
            }

            if (b == null)
            {
                throw new ArgumentNullException(nameof(b));
            }

            if (a.Genes.Length != b.Genes.Length)
            {
                throw new ArgumentException("Both parents must have the same chromosome length.");
            }

            var child = new Chromosome(a.Genes.Length);
            int splitIndex = rng.Next(1, a.Genes.Length - 1);

            for (int i = 0; i < a.Genes.Length; i++)
            {
                child.Genes[i] = i < splitIndex ? a.Genes[i] : b.Genes[i];
            }

            return child;
        }

        /// <summary>
        /// Mutuje geny chromosomu zgodnie z ustalonym prawdopodobieństwem mutacji.
        /// </summary>
        /// <param name="chromosome">Chromosom poddawany mutacji.</param>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy parametr <paramref name="chromosome"/> ma wartość null.
        /// </exception>
        private static void Mutate(Chromosome chromosome, global::System.Random rng)
        {
            if (chromosome == null)
            {
                throw new ArgumentNullException(nameof(chromosome));
            }

            for (int i = 0; i < chromosome.Genes.Length; i++)
            {
                if (rng.NextDouble() < MutationChance)
                {
                    chromosome.Genes[i] = (MoveDirection)rng.Next(0, 4);
                }
            }
        }

        /// <summary>
        /// Przepisuje do wyniku dane pochodzące z najlepszego chromosomu.
        /// </summary>
        /// <param name="result">Obiekt wyniku do uzupełnienia.</param>
        /// <param name="chromosome">Chromosom źródłowy.</param>
        /// <param name="maze">Labirynt używany do obliczeń pomocniczych.</param>
        /// <param name="context">Kontekst uruchomienia algorytmu.</param>
        private static void FillResultFromChromosome(
            MazeAlgorithmResult result,
            Chromosome chromosome,
            MazeGrid maze,
            MazeAlgorithmContext context,
            ISet<Vector2Int> discoveredCells)
        {
            result.reachedGoal = chromosome.ReachedGoal;
            result.stepsTaken = chromosome.StepsTaken;
            result.shortestPossiblePathLength = maze.GetShortestPathLength(
                context.startPosition,
                context.finishPosition);

            result.expandedNodes = chromosome.Path.Count;
            result.backtrackCount = chromosome.BacktrackCount;

            // BFS jest uruchamiany dopiero po znalezieniu mety przez algorytm
            // i może korzystać wyłącznie z komórek odkrytych w jego przebiegu.
            // Nie podstawia globalnego rozwiązania labiryntu jako wyniku genetycznego.
            List<Vector2Int> optimizedDiscoveredPath = chromosome.ReachedGoal
                ? maze.GetShortestPathWithinCells(
                    context.startPosition,
                    context.finishPosition,
                    discoveredCells)
                : new List<Vector2Int>();

            List<Vector2Int> pathToPresent = optimizedDiscoveredPath.Count > 0
                ? optimizedDiscoveredPath
                : chromosome.Path;

            result.pathLength = chromosome.ReachedGoal ? Mathf.Max(0, chromosome.Path.Count - 1) : 0;
            result.optimizedDiscoveredPathLength = chromosome.ReachedGoal
                ? Mathf.Max(0, pathToPresent.Count - 1)
                : 0;

            result.rawFinalPath.Clear();
            if (chromosome.ReachedGoal)
            {
                result.rawFinalPath.AddRange(chromosome.Path);
            }

            result.finalPath.Clear();
            if (chromosome.ReachedGoal)
            {
                result.finalPath.AddRange(pathToPresent);
            }
        }

        /// <summary>
        /// Uzupełnia wspólne statystyki wyniku niezależne od konkretnego chromosomu końcowego.
        /// </summary>
        /// <param name="result">Obiekt wyniku do uzupełnienia.</param>
        /// <param name="globallyVisited">Zbiór globalnie odwiedzonych pól.</param>
        /// <param name="revisitedCells">Liczba ponownych odwiedzeń pól.</param>
        /// <param name="wallHits">Liczba uderzeń w ścianę.</param>
        /// <param name="deadEnds">Liczba napotkanych ślepych zaułków.</param>
        /// <param name="validMoves">Liczba poprawnych ruchów.</param>
        /// <param name="invalidMoves">Liczba niepoprawnych ruchów.</param>
        /// <param name="stagnationCounter">Aktualna liczba generacji stagnacji.</param>
        /// <param name="bestGenerationIndex">Indeks najlepszej generacji.</param>
        /// <param name="endReason">Powód zakończenia działania algorytmu.</param>
        private static void FillSharedResultStats(
            MazeAlgorithmResult result,
            HashSet<Vector2Int> globallyVisited,
            int revisitedCells,
            int wallHits,
            int deadEnds,
            int validMoves,
            int invalidMoves,
            int stagnationCounter,
            int bestGenerationIndex,
            string endReason)
        {
            result.stagnationIterations = stagnationCounter;
            result.revisitedCells = revisitedCells;
            result.wallHits = wallHits;
            result.deadEndsEncountered = deadEnds;
            result.validMovesConsidered = validMoves;
            result.invalidMovesConsidered = invalidMoves;
            result.visitedCells = globallyVisited.Count;
            result.endReason = endReason;
            result.additionalInfo =
                $"Population={PopulationSize}; Mutation={MutationChance}; BestGeneration={bestGenerationIndex + 1}; " +
                $"Replay=up to {MaxReplaySegments} new best offspring plus winning offspring; RawPath=successful offspring; " +
                "PresentedPath=BFS over shown Genetic cells";
        }
    }
}
