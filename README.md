# LabyrinthExplorer

**LabyrinthExplorer** is a Unity application for creating mazes and comparing two heuristic path-search algorithms:

* **Genetic Algorithm (GA)**
* **Ant Colony Optimization Algorithm (ACO)**

The application allows the user to design or generate a maze, define start and finish points, run algorithm benchmarks, replay selected exploration paths, and review saved measurement results.

## Main Features

* Interactive maze editor.
* Random solvable maze generation using randomized DFS.
* Adjustable maze sizes.
* Start and finish point placement.
* Comparison of Genetic Algorithm and Ant Colony Optimization.
* Repeated benchmark runs on the same maze configuration.
* Separate measurement of algorithm logic and post-benchmark visualization.
* Replay of representative algorithm attempts.
* Presentation of optimized paths discovered within each algorithm's explored area.
* Benchmark history panel.
* Export of measurement results to JSON and CSV files.
* Polish and English interface support.
* Resolution and fullscreen settings.

## Application Purpose

The purpose of the project is to visualize and compare the behaviour of heuristic algorithms while solving the same maze.

Unlike classical shortest-path algorithms such as BFS, both implemented algorithms are stochastic and may produce different results depending on their random seed and work budget. The application therefore focuses on:

* whether an algorithm reaches the goal,
* how much work it requires,
* how long its logic executes,
* how good its raw route is,
* how much of the maze it explores,
* how the two algorithms behave visually.

## Implemented Algorithms

### Genetic Algorithm

The Genetic Algorithm represents a possible route as a chromosome containing a sequence of movement directions.

Its implementation includes:

* randomly generated initial populations,
* fitness-based evaluation,
* tournament selection,
* one-point crossover,
* mutation,
* elitism,
* restart after stagnation,
* penalties for collisions, revisits and dead ends,
* rewards for progress toward the finish point.

Each chromosome is evaluated as a possible route through the maze. The best successful chromosome is retained as the raw solution produced by the algorithm.

### Ant Colony Optimization

The Ant Colony Optimization algorithm simulates multiple ants searching for a route through the maze.

Its implementation includes:

* multiple ants per iteration,
* pheromone map initialization,
* pheromone evaporation,
* pheromone deposition,
* heuristic preference for cells closer to the finish,
* random exploration,
* stagnation detection,
* pheromone reset after prolonged stagnation.

Each ant selects legal neighboring cells based on pheromone strength and distance-based heuristic information. The best successful ant route is retained as the raw solution produced by the algorithm.

## Benchmark Methodology

Both algorithms are tested on the same maze and under comparable benchmark conditions.

For each comparison run:

1. The same maze layout is used for both algorithms.
2. The same algorithm seed is assigned to both algorithms for a given run.
3. The order of algorithm execution alternates between runs to reduce runtime warm-up bias.
4. Both algorithms receive the same candidate-evaluation budget:

   * one evaluated chromosome for the Genetic Algorithm,
   * one evaluated ant for the Ant Colony Algorithm.
5. Visualization is disabled during measurement so that animation delays do not affect algorithm timing.
6. After measurement, the application replays selected algorithm attempts for presentation purposes.

### Important Methodological Note

The candidate-evaluation budget provides a practical common comparison unit, but it does not mean that both algorithms perform exactly the same number of elementary operations.

A chromosome in the Genetic Algorithm and an ant in the Ant Colony Algorithm work differently internally. Therefore, the benchmark compares their practical behaviour under a shared evaluation budget rather than claiming strict operation-by-operation equivalence.

## Raw Route vs Presented BFS Route

The application intentionally distinguishes between:

### Raw Algorithm Route

The actual route found by the selected successful chromosome or ant.

This route is used when comparing the quality of the algorithms.

### Presented BFS Route

After an algorithm reaches the finish point, the application runs BFS only on the cells that were genuinely visited by that algorithm during its run.

This produces the shortest path available within the area already discovered by the algorithm. It is used as a visualization and analysis layer, not as a replacement for the raw result.

In other words:

* GA and ACO perform the exploration.
* BFS does not solve the maze for them.
* BFS only simplifies the displayed route within already discovered cells.
* Algorithm quality comparisons are based on raw routes.

## Visualization Legend

During replay and result presentation:

| Colour | Meaning                                                                        |
| ------ | ------------------------------------------------------------------------------ |
| Blue   | Genetic Algorithm exploration and route attempts                               |
| Orange | Ant Colony Algorithm exploration and pheromone-influenced movement             |
| Violet | BFS route calculated only over cells discovered by the corresponding algorithm |
| Green  | Start point                                                                    |
| Red    | Finish point                                                                   |

The replay is performed after benchmark measurements have completed. It represents recorded algorithm behaviour without affecting measured logic execution time.

## Collected Metrics

The application records detailed information for every algorithm run, including:

* algorithm name and version,
* maze name, size, seed and layout hash,
* algorithm seed,
* benchmark objective,
* whether the finish was reached,
* raw route length,
* BFS route length within discovered cells,
* shortest possible path length in the maze,
* raw path efficiency,
* optimized discovered-path efficiency,
* number of evaluated candidates,
* iteration or generation of first success,
* number of visited and revisited cells,
* number of backtracks and dead ends,
* number of valid and invalid moves,
* number of restarts,
* best and average fitness values,
* logic execution time,
* total runtime,
* memory measurements,
* visualization statistics where applicable.

Results can be saved for later analysis and exported to:

* **JSON**
* **CSV**

## Project Structure

```text
LabyrinthExplorer/
└── My project/
    ├── Assets/
    │   ├── Algorytm/
    │   │   ├── Dane/
    │   │   │   ├── AlgorithmMetrics.cs
    │   │   │   ├── AlgorithmSummary.cs
    │   │   │   ├── AlgorithmComparisonResult.cs
    │   │   │   ├── AlgorithmProfiler.cs
    │   │   │   ├── MazeAlgorithmContext.cs
    │   │   │   └── MetricsExporter.cs
    │   │   ├── Genetyczny/
    │   │   │   ├── GeneticMazeAlgorithm.cs
    │   │   │   ├── Chromosome.cs
    │   │   │   ├── MazeGrid.cs
    │   │   │   └── MoveDirection.cs
    │   │   ├── Mrówkowy/
    │   │   │   └── AntColonyMazeAlgorithm.cs
    │   │   └── System/
    │   │       ├── IMazeAlgorithm.cs
    │   │       └── BenchmarkRunner.cs
    │   ├── Scripts/
    │   │   ├── MazeAppController.cs
    │   │   ├── MazeApplication/
    │   │   ├── MapEditor/
    │   │   └── Statistics/
    │   └── Scenes/
    │       └── AppScene.unity
    ├── Packages/
    └── ProjectSettings/
```

## Technologies

* **Unity 6** — version `6000.3.11f1`
* **C#**
* **Unity UI / TextMesh Pro**
* **Unity Input System**
* **Unity Test Framework** dependency included in the project

## How to Run the Project

### Requirements

* Unity Hub
* Unity Editor version `6000.3.11f1`

### Installation

1. Clone the repository:

```bash
git clone https://github.com/MatthiasLew/LabyrinthExplorer.git
```

2. Open **Unity Hub**.

3. Select **Add project from disk**.

4. Choose the following project directory:

```text
LabyrinthExplorer/My project
```

5. Open the project using Unity `6000.3.11f1`.

6. Open the main scene:

```text
Assets/Scenes/AppScene.unity
```

7. Press **Play** in the Unity Editor.

## Basic Usage

1. Create a new maze or generate a random one.
2. Edit walls using the maze editor tools.
3. Set the start and finish positions.
4. Choose benchmark settings such as:

   * maze size,
   * number of runs,
   * algorithm work budget,
   * benchmark objective.
5. Run the comparison.
6. Observe the replay of representative GA and ACO attempts.
7. Review the benchmark summary and saved measurement history.
8. Export collected results to CSV or JSON for external analysis.

## Benchmark Objectives

The application supports two comparison objectives:

### Optimize Path Within Budget

Both algorithms continue working within the configured candidate-evaluation budget in order to search for better solutions.

This mode is suitable for comparing solution quality under equal practical constraints.

### Find First Solution

Each algorithm stops after finding its first successful route.

This mode is suitable for comparing how quickly an algorithm is able to discover any valid route.

## Generated Mazes

Random mazes are generated using a randomized depth-first search approach.

The generator:

* creates connected corridor-based maze layouts,
* keeps external borders closed,
* evaluates several generated candidates,
* selects a maze with a relatively long estimated route,
* adds a limited number of additional openings,
* supports deterministic generation through seeds.

Using a saved maze seed together with the stored layout hash makes benchmark scenarios easier to reproduce.

## Saved Data

The application stores benchmark history locally using Unity's persistent data directory.

Saved measurement entries can later be displayed in the statistics panel and used to compare previous benchmark runs.

Exported JSON and CSV files are also written to the application's persistent data location.

## Development Status

The project currently includes:

* functional maze editing,
* random maze generation,
* Genetic Algorithm implementation,
* Ant Colony Optimization implementation,
* benchmark measurement logic,
* post-measurement replay,
* statistics history,
* CSV and JSON export,
* localization and display settings.

Possible future improvements include:

* automated EditMode and PlayMode tests,
* visual charts for benchmark history,
* larger benchmark experiment presets,
* additional maze-solving algorithms,
* improved statistical reporting for stochastic runs,
* packaged desktop builds and releases.

## Author

Created by **MatthiasLew**, **Cerosene**.

Repository: `MatthiasLew/LabyrinthExplorer`
