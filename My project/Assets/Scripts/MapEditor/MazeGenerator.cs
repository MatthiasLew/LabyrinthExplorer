using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generuje rozwiązywalne labirynty korytarzowe metodą randomized depth-first search.
/// Dla jednego seeda wybiera najlepszy z kilku kandydatów, aby uzyskać dłuższą trasę.
/// </summary>
public class MazeGenerator
{
    private const int CandidateCount = 5;
    private const float ExtraOpeningsRatio = 0.05f;

    /// <summary>
    /// Generuje mapę przechodniości: true = pole przechodnie, false = ściana.
    /// Zewnętrzna ramka pozostaje ścianą; start i meta są wybierane później przez kontroler
    /// jako dwa odległe, przechodnie pola.
    /// </summary>
    public bool[,] GenerateMaze(int width, int height, int seed = 0)
    {
        if (width < 3 || height < 3)
        {
            throw new ArgumentException("Maze dimensions must be at least 3x3.");
        }

        int effectiveSeed = seed == 0 ? Environment.TickCount : seed;
        var seedGenerator = new global::System.Random(effectiveSeed);

        bool[,] bestMaze = null;
        int bestDistance = -1;

        for (int attempt = 0; attempt < CandidateCount; attempt++)
        {
            var rng = new global::System.Random(seedGenerator.Next());
            bool[,] candidate = GenerateCandidate(width, height, rng);
            AddExtraOpenings(candidate, rng);

            int distance = EstimateDiameter(candidate);
            if (distance > bestDistance)
            {
                bestMaze = candidate;
                bestDistance = distance;
            }
        }

        if (bestMaze == null)
        {
            throw new InvalidOperationException("Nie udało się wygenerować labiryntu.");
        }

        return bestMaze;
    }

    private static bool[,] GenerateCandidate(int width, int height, global::System.Random rng)
    {
        var maze = new bool[width, height];

        int maxCellX = width - 2;
        int maxCellY = height - 2;

        if (maxCellX % 2 == 0)
        {
            maxCellX--;
        }

        if (maxCellY % 2 == 0)
        {
            maxCellY--;
        }

        Vector2Int start = new Vector2Int(1, 1);
        var stack = new Stack<Vector2Int>();
        maze[start.x, start.y] = true;
        stack.Push(start);

        Vector2Int[] offsets =
        {
            new Vector2Int(0, 2),
            new Vector2Int(2, 0),
            new Vector2Int(0, -2),
            new Vector2Int(-2, 0)
        };

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek();
            var candidates = new List<Vector2Int>();

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2Int next = current + offsets[i];

                if (next.x < 1 || next.x > maxCellX ||
                    next.y < 1 || next.y > maxCellY ||
                    maze[next.x, next.y])
                {
                    continue;
                }

                candidates.Add(next);
            }

            if (candidates.Count == 0)
            {
                stack.Pop();
                continue;
            }

            Vector2Int selected = candidates[rng.Next(candidates.Count)];
            Vector2Int between = new Vector2Int(
                (current.x + selected.x) / 2,
                (current.y + selected.y) / 2);

            maze[between.x, between.y] = true;
            maze[selected.x, selected.y] = true;
            stack.Push(selected);
        }

        return maze;
    }

    private static void AddExtraOpenings(bool[,] maze, global::System.Random rng)
    {
        int width = maze.GetLength(0);
        int height = maze.GetLength(1);
        var openings = new List<Vector2Int>();

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (maze[x, y])
                {
                    continue;
                }

                bool horizontalConnection = maze[x - 1, y] && maze[x + 1, y];
                bool verticalConnection = maze[x, y - 1] && maze[x, y + 1];

                if (horizontalConnection || verticalConnection)
                {
                    openings.Add(new Vector2Int(x, y));
                }
            }
        }

        Shuffle(openings, rng);

        int openingCount = Mathf.Min(
            openings.Count,
            Mathf.Max(1, Mathf.RoundToInt(width * height * ExtraOpeningsRatio)));

        for (int i = 0; i < openingCount; i++)
        {
            Vector2Int opening = openings[i];
            maze[opening.x, opening.y] = true;
        }
    }

    private static int EstimateDiameter(bool[,] maze)
    {
        Vector2Int first = FindFirstWalkable(maze);
        if (first.x < 0)
        {
            return -1;
        }

        Vector2Int endA = FindFarthestCell(maze, first, out _);
        FindFarthestCell(maze, endA, out int distance);
        return distance;
    }

    private static Vector2Int FindFirstWalkable(bool[,] maze)
    {
        for (int x = 1; x < maze.GetLength(0) - 1; x++)
        {
            for (int y = 1; y < maze.GetLength(1) - 1; y++)
            {
                if (maze[x, y])
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return new Vector2Int(-1, -1);
    }

    private static Vector2Int FindFarthestCell(bool[,] maze, Vector2Int start, out int maxDistance)
    {
        int width = maze.GetLength(0);
        int height = maze.GetLength(1);
        var queue = new Queue<Vector2Int>();
        var distances = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                distances[x, y] = -1;
            }
        }

        queue.Enqueue(start);
        distances[start.x, start.y] = 0;

        Vector2Int farthest = start;
        maxDistance = 0;

        Vector2Int[] neighbors =
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.down
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int distance = distances[current.x, current.y];

            if (distance > maxDistance)
            {
                maxDistance = distance;
                farthest = current;
            }

            for (int i = 0; i < neighbors.Length; i++)
            {
                Vector2Int next = current + neighbors[i];

                if (next.x <= 0 || next.x >= width - 1 ||
                    next.y <= 0 || next.y >= height - 1 ||
                    !maze[next.x, next.y] ||
                    distances[next.x, next.y] >= 0)
                {
                    continue;
                }

                distances[next.x, next.y] = distance + 1;
                queue.Enqueue(next);
            }
        }

        return farthest;
    }

    private static void Shuffle<T>(IList<T> items, global::System.Random rng)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int index = rng.Next(i + 1);
            T temporary = items[i];
            items[i] = items[index];
            items[index] = temporary;
        }
    }
}
