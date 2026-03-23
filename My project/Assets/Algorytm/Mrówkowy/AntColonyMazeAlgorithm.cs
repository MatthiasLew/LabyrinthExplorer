using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Algorytm.Dane;
using Algorytm.Genetyczny;
using Algorytm.System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Algorytm.Mrówkowy
{
    /// <summary>
    /// Implementuje algorytm kolonii mrówek do wyszukiwania ścieżki w labiryncie.
    /// </summary>
    public class AntColonyMazeAlgorithm : IMazeAlgorithm
    {
        /// <summary>
        /// Nazwa algorytmu prezentowana w metrykach oraz wynikach benchmarku.
        /// </summary>
        public string AlgorithmName => "Ant Colony Algorithm";

        /// <summary>
        /// Wersja implementacji algorytmu.
        /// </summary>
        public string AlgorithmVersion => "1.0.0";

        private const int AntCount = 40;
        private const int MaxIterations = 150;
        private const int MaxStepsPerAnt = 200;
        private const int MaxStagnationIterations = 25;

        private const float Alpha = 1.0f;
        private const float Beta = 2.5f;
        private const float EvaporationRate = 0.18f;
        private const float PheromoneDeposit = 18f;
        private const float InitialPheromone = 0.2f;
        private const float ExplorationChance = 0.08f;

        /// <summary>
        /// Uruchamia algorytm kolonii mrówek dla zadanego kontekstu i zapisuje wynik do przekazanego obiektu metryk.
        /// </summary>
        /// <param name="context">Kontekst uruchomienia algorytmu.</param>
        /// <param name="metrics">Obiekt metryk, który zostanie uzupełniony wynikiem działania.</param>
        /// <param name="profiler">Profiler odpowiedzialny za pomiar czasu i pamięci.</param>
        /// <returns>Korutyna wykonująca kolejne iteracje algorytmu.</returns>
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

            Random.InitState(context.randomSeed);

            float[,] pheromones = CreateInitialPheromoneMap(maze);

            var globallyVisited = new HashSet<Vector2Int>();
            int revisitedCells = 0;
            int wallHits = 0;
            int deadEnds = 0;
            int validMoves = 0;
            int invalidMoves = 0;

            AntRunData bestAntEver = null;
            float bestFitnessEver = float.MinValue;
            int stagnationCounter = 0;
            int bestIterationIndex = -1;

            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                profiler.BeginIteration();

                var ants = new List<AntRunData>(AntCount);

                for (int antIndex = 0; antIndex < AntCount; antIndex++)
                {
                    AntRunData ant = RunSingleAnt(
                        maze,
                        pheromones,
                        context.startPosition,
                        context.finishPosition,
                        globallyVisited,
                        ref revisitedCells,
                        ref wallHits,
                        ref deadEnds,
                        ref validMoves,
                        ref invalidMoves);

                    ants.Add(ant);
                }

                AntRunData bestAntThisIteration = ants
                    .OrderByDescending(ant => ant.Fitness)
                    .First();

                float averageFitness = ants.Average(ant => ant.Fitness);

                result.iterations = iteration + 1;
                result.bestFitness = bestAntThisIteration.Fitness;
                result.averageFitness = averageFitness;
                result.frontierMaxSize = Mathf.Max(result.frontierMaxSize, ants.Count);

                if (bestAntThisIteration.Fitness > bestFitnessEver)
                {
                    bestFitnessEver = bestAntThisIteration.Fitness;
                    bestAntEver = bestAntThisIteration.Clone();
                    stagnationCounter = 0;
                    bestIterationIndex = iteration;
                }
                else
                {
                    stagnationCounter++;
                }

                EvaporatePheromones(pheromones, maze);
                DepositPheromones(pheromones, ants, maze, context.finishPosition);

                if (bestAntThisIteration.ReachedGoal)
                {
                    FillResultFromAnt(result, bestAntThisIteration, maze, context);
                    FillSharedResultStats(
                        result,
                        globallyVisited,
                        revisitedCells,
                        wallHits,
                        deadEnds,
                        validMoves,
                        invalidMoves,
                        stagnationCounter,
                        iteration,
                        "GoalReached");

                    profiler.EndIteration();
                    result.ApplyTo(metrics);
                    yield break;
                }

                if (stagnationCounter >= MaxStagnationIterations)
                {
                    result.restartCount++;
                    ResetPheromones(pheromones, maze);
                    stagnationCounter = 0;
                }

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

            if (bestAntEver != null)
            {
                FillResultFromAnt(result, bestAntEver, maze, context);
            }

            result.reachedGoal = false;

            FillSharedResultStats(
                result,
                globallyVisited,
                revisitedCells,
                wallHits,
                deadEnds,
                validMoves,
                invalidMoves,
                stagnationCounter,
                bestIterationIndex,
                "MaxIterationsReached");

            result.ApplyTo(metrics);
        }

        /// <summary>
        /// Tworzy początkową mapę feromonów dla całego labiryntu.
        /// </summary>
        /// <param name="maze">Labirynt, dla którego tworzona jest mapa feromonów.</param>
        /// <returns>Dwuwymiarowa tablica poziomów feromonów.</returns>
        private static float[,] CreateInitialPheromoneMap(MazeGrid maze)
        {
            var pheromones = new float[maze.Width, maze.Height];

            for (int x = 0; x < maze.Width; x++)
            {
                for (int y = 0; y < maze.Height; y++)
                {
                    pheromones[x, y] = InitialPheromone;
                }
            }

            return pheromones;
        }

        /// <summary>
        /// Resetuje mapę feromonów do wartości początkowej.
        /// </summary>
        /// <param name="pheromones">Mapa feromonów do zresetowania.</param>
        /// <param name="maze">Labirynt określający rozmiar mapy feromonów.</param>
        private static void ResetPheromones(float[,] pheromones, MazeGrid maze)
        {
            for (int x = 0; x < maze.Width; x++)
            {
                for (int y = 0; y < maze.Height; y++)
                {
                    pheromones[x, y] = InitialPheromone;
                }
            }
        }

        /// <summary>
        /// Zmniejsza poziom feromonów w całej mapie zgodnie z parametrem ewaporacji.
        /// </summary>
        /// <param name="pheromones">Mapa feromonów.</param>
        /// <param name="maze">Labirynt określający rozmiar mapy.</param>
        private static void EvaporatePheromones(float[,] pheromones, MazeGrid maze)
        {
            for (int x = 0; x < maze.Width; x++)
            {
                for (int y = 0; y < maze.Height; y++)
                {
                    pheromones[x, y] *= 1f - EvaporationRate;

                    if (pheromones[x, y] < 0.01f)
                    {
                        pheromones[x, y] = 0.01f;
                    }
                }
            }
        }

        /// <summary>
        /// Dodaje feromony na podstawie tras wykonanych przez mrówki w bieżącej iteracji.
        /// </summary>
        /// <param name="pheromones">Mapa feromonów.</param>
        /// <param name="ants">Lista mrówek z bieżącej iteracji.</param>
        /// <param name="maze">Labirynt używany do obliczeń pomocniczych.</param>
        /// <param name="finish">Pozycja docelowa.</param>
        private static void DepositPheromones(
            float[,] pheromones,
            List<AntRunData> ants,
            MazeGrid maze,
            Vector2Int finish)
        {
            foreach (AntRunData ant in ants)
            {
                if (ant.Path.Count == 0)
                {
                    continue;
                }

                float depositStrength;

                if (ant.ReachedGoal)
                {
                    depositStrength = PheromoneDeposit / Mathf.Max(1, ant.Path.Count);
                }
                else
                {
                    int distance = maze.GetManhattanDistance(ant.FinalPosition, finish);
                    depositStrength = (PheromoneDeposit * 0.25f) / Mathf.Max(1, distance + ant.Path.Count);
                }

                foreach (Vector2Int step in ant.Path)
                {
                    pheromones[step.x, step.y] += depositStrength;
                }
            }
        }

        /// <summary>
        /// Uruchamia pojedynczą mrówkę i zwraca dane o jej trasie oraz jakości rozwiązania.
        /// </summary>
        /// <param name="maze">Labirynt używany przez mrówkę.</param>
        /// <param name="pheromones">Mapa feromonów.</param>
        /// <param name="start">Pozycja początkowa.</param>
        /// <param name="finish">Pozycja docelowa.</param>
        /// <param name="globallyVisited">Zbiór pól odwiedzonych globalnie przez wszystkie mrówki.</param>
        /// <param name="revisitedCells">Licznik ponownych odwiedzeń pól.</param>
        /// <param name="wallHits">Licznik prób wejścia w ścianę.</param>
        /// <param name="deadEnds">Licznik napotkanych ślepych zaułków.</param>
        /// <param name="validMoves">Licznik poprawnych ruchów.</param>
        /// <param name="invalidMoves">Licznik niepoprawnych ruchów.</param>
        /// <returns>Dane opisujące przebieg pojedynczej mrówki.</returns>
        private static AntRunData RunSingleAnt(
            MazeGrid maze,
            float[,] pheromones,
            Vector2Int start,
            Vector2Int finish,
            HashSet<Vector2Int> globallyVisited,
            ref int revisitedCells,
            ref int wallHits,
            ref int deadEnds,
            ref int validMoves,
            ref int invalidMoves)
        {
            var ant = new AntRunData();
            var localVisited = new HashSet<Vector2Int>();

            Vector2Int current = start;
            ant.Path.Add(current);
            localVisited.Add(current);
            globallyVisited.Add(current);

            int initialDistance = maze.GetManhattanDistance(start, finish);
            int bestDistance = initialDistance;

            for (int stepIndex = 0; stepIndex < MaxStepsPerAnt; stepIndex++)
            {
                List<Vector2Int> neighbors = maze
                    .GetNeighbors(current)
                    .Where(maze.IsWalkable)
                    .ToList();

                if (neighbors.Count == 0)
                {
                    deadEnds++;
                    break;
                }

                Vector2Int next = SelectNextMove(
                    neighbors,
                    pheromones,
                    maze,
                    finish);

                if (!maze.IsWalkable(next))
                {
                    wallHits++;
                    invalidMoves++;
                    continue;
                }

                validMoves++;
                ant.StepsTaken++;

                current = next;
                ant.Path.Add(current);

                int currentDistance = maze.GetManhattanDistance(current, finish);
                if (currentDistance < bestDistance)
                {
                    ant.Fitness += (bestDistance - currentDistance) * 2f;
                    bestDistance = currentDistance;
                }

                bool wasVisitedGlobally = !globallyVisited.Add(current);
                bool wasVisitedLocally = !localVisited.Add(current);

                if (wasVisitedLocally)
                {
                    revisitedCells++;
                    ant.BacktrackCount++;
                    ant.Fitness -= 2.5f;
                }
                else if (wasVisitedGlobally)
                {
                    revisitedCells++;
                    ant.Fitness -= 1.0f;
                }

                if (maze.GetWalkableNeighborCount(current) <= 1 && current != finish)
                {
                    deadEnds++;
                    ant.Fitness -= 2f;
                }

                ant.Fitness += pheromones[current.x, current.y] * 0.5f;

                if (current == finish)
                {
                    ant.ReachedGoal = true;
                    ant.Fitness += 10000f;
                    break;
                }
            }

            int distanceToGoal = maze.GetManhattanDistance(current, finish);

            ant.Fitness -= distanceToGoal * 10f;
            ant.Fitness -= ant.StepsTaken * 0.25f;
            ant.FinalPosition = current;

            if (!ant.ReachedGoal && distanceToGoal < initialDistance)
            {
                ant.Fitness += (initialDistance - distanceToGoal) * 1.5f;
            }

            return ant;
        }

        /// <summary>
        /// Wybiera kolejny ruch mrówki na podstawie feromonów, heurystyki i eksploracji losowej.
        /// </summary>
        /// <param name="neighbors">Lista dostępnych sąsiadów.</param>
        /// <param name="pheromones">Mapa feromonów.</param>
        /// <param name="maze">Labirynt używany do obliczenia heurystyki.</param>
        /// <param name="finish">Pozycja docelowa.</param>
        /// <returns>Wybrana pozycja kolejnego ruchu.</returns>
        private static Vector2Int SelectNextMove(
            List<Vector2Int> neighbors,
            float[,] pheromones,
            MazeGrid maze,
            Vector2Int finish)
        {
            if (neighbors.Count == 1)
            {
                return neighbors[0];
            }

            if (Random.value < ExplorationChance)
            {
                return neighbors[Random.Range(0, neighbors.Count)];
            }

            float totalWeight = 0f;
            var weights = new float[neighbors.Count];

            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector2Int cell = neighbors[i];

                float pheromone = Mathf.Pow(Mathf.Max(0.01f, pheromones[cell.x, cell.y]), Alpha);
                float heuristic = Mathf.Pow(1f / Mathf.Max(1f, maze.GetManhattanDistance(cell, finish)), Beta);

                float weight = pheromone * heuristic;
                weights[i] = weight;
                totalWeight += weight;
            }

            if (totalWeight <= 0f)
            {
                return neighbors[Random.Range(0, neighbors.Count)];
            }

            float roll = Random.value * totalWeight;
            float cumulative = 0f;

            for (int i = 0; i < neighbors.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                {
                    return neighbors[i];
                }
            }

            return neighbors[neighbors.Count - 1];
        }

        /// <summary>
        /// Przepisuje do wyniku dane pochodzące z najlepszej mrówki.
        /// </summary>
        /// <param name="result">Obiekt wyniku do uzupełnienia.</param>
        /// <param name="ant">Najlepsza mrówka.</param>
        /// <param name="maze">Labirynt używany do obliczeń pomocniczych.</param>
        /// <param name="context">Kontekst uruchomienia algorytmu.</param>
        private static void FillResultFromAnt(
            MazeAlgorithmResult result,
            AntRunData ant,
            MazeGrid maze,
            MazeAlgorithmContext context)
        {
            result.reachedGoal = ant.ReachedGoal;
            result.stepsTaken = ant.StepsTaken;
            result.pathLength = Mathf.Max(0, ant.Path.Count - 1);
            result.shortestPossiblePathLength = maze.GetShortestPathLength(
                context.startPosition,
                context.finishPosition);

            result.expandedNodes = ant.Path.Count;
            result.backtrackCount = ant.BacktrackCount;
        }

        /// <summary>
        /// Uzupełnia wspólne statystyki wyniku niezależne od konkretnej końcowej mrówki.
        /// </summary>
        /// <param name="result">Obiekt wyniku do uzupełnienia.</param>
        /// <param name="globallyVisited">Zbiór globalnie odwiedzonych pól.</param>
        /// <param name="revisitedCells">Liczba ponownych odwiedzeń pól.</param>
        /// <param name="wallHits">Liczba uderzeń w ścianę.</param>
        /// <param name="deadEnds">Liczba napotkanych ślepych zaułków.</param>
        /// <param name="validMoves">Liczba poprawnych ruchów.</param>
        /// <param name="invalidMoves">Liczba niepoprawnych ruchów.</param>
        /// <param name="stagnationCounter">Aktualna liczba iteracji stagnacji.</param>
        /// <param name="bestIterationIndex">Indeks najlepszej iteracji.</param>
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
            int bestIterationIndex,
            string endReason)
        {
            result.revisitedCells = revisitedCells;
            result.wallHits = wallHits;
            result.deadEndsEncountered = deadEnds;
            result.validMovesConsidered = validMoves;
            result.invalidMovesConsidered = invalidMoves;
            result.visitedCells = globallyVisited.Count;
            result.stagnationIterations = stagnationCounter;
            result.endReason = endReason;
            result.additionalInfo =
                $"Ants={AntCount}; Alpha={Alpha}; Beta={Beta}; Evaporation={EvaporationRate}; BestIteration={bestIterationIndex + 1}";
        }

        /// <summary>
        /// Przechowuje wynik pojedynczego przebiegu mrówki.
        /// </summary>
        private sealed class AntRunData
        {
            /// <summary>
            /// Określa, czy mrówka osiągnęła cel.
            /// </summary>
            public bool ReachedGoal;

            /// <summary>
            /// Liczba wykonanych kroków.
            /// </summary>
            public int StepsTaken;

            /// <summary>
            /// Liczba wykrytych nawrotów.
            /// </summary>
            public int BacktrackCount;

            /// <summary>
            /// Wartość funkcji fitness przypisana trasie mrówki.
            /// </summary>
            public float Fitness;

            /// <summary>
            /// Końcowa pozycja mrówki po zakończeniu przebiegu.
            /// </summary>
            public Vector2Int FinalPosition;

            /// <summary>
            /// Ścieżka przebyta przez mrówkę.
            /// </summary>
            public List<Vector2Int> Path { get; } = new();

            /// <summary>
            /// Tworzy pełną kopię bieżących danych mrówki.
            /// </summary>
            /// <returns>Nowa instancja zawierająca skopiowane dane.</returns>
            public AntRunData Clone()
            {
                var clone = new AntRunData
                {
                    ReachedGoal = ReachedGoal,
                    StepsTaken = StepsTaken,
                    BacktrackCount = BacktrackCount,
                    Fitness = Fitness,
                    FinalPosition = FinalPosition
                };

                clone.Path.AddRange(Path);
                return clone;
            }
        }
    }
}