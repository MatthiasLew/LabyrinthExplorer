using System;
using System.Collections.Generic;
using UnityEngine;

namespace Algorytm.Genetyczny
{
    [Serializable]
    public class MazeGrid
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private bool[] walkableCells;

        public int Width => width;
        public int Height => height;

        public MazeGrid(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
            }

            this.width = width;
            this.height = height;
            walkableCells = new bool[width * height];
        }

        public MazeGrid(bool[,] walkableMap)
        {
            if (walkableMap == null)
            {
                throw new ArgumentNullException(nameof(walkableMap));
            }

            width = walkableMap.GetLength(0);
            height = walkableMap.GetLength(1);
            walkableCells = new bool[width * height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    SetWalkable(new Vector2Int(x, y), walkableMap[x, y]);
                }
            }
        }

        public bool IsInside(Vector2Int position)
        {
            return position.x >= 0 &&
                   position.x < width &&
                   position.y >= 0 &&
                   position.y < height;
        }

        public bool IsWalkable(Vector2Int position)
        {
            return IsInside(position) && walkableCells[GetIndex(position)];
        }

        public void SetWalkable(Vector2Int position, bool isWalkable)
        {
            if (!IsInside(position))
            {
                throw new ArgumentOutOfRangeException(nameof(position), $"Position {position} is outside the maze.");
            }

            walkableCells[GetIndex(position)] = isWalkable;
        }

        public int GetWalkableNeighborCount(Vector2Int position)
        {
            int count = 0;

            foreach (Vector2Int neighbor in GetNeighbors(position))
            {
                if (IsWalkable(neighbor))
                {
                    count++;
                }
            }

            return count;
        }

        public IEnumerable<Vector2Int> GetNeighbors(Vector2Int position)
        {
            yield return position + Vector2Int.up;
            yield return position + Vector2Int.right;
            yield return position + Vector2Int.down;
            yield return position + Vector2Int.left;
        }

        public int GetShortestPathLength(Vector2Int start, Vector2Int finish)
        {
            if (!IsWalkable(start) || !IsWalkable(finish))
            {
                return 0;
            }

            if (start == finish)
            {
                return 1;
            }

            var visited = new HashSet<Vector2Int> { start };
            var queue = new Queue<(Vector2Int Position, int Distance)>();
            queue.Enqueue((start, 1));

            while (queue.Count > 0)
            {
                (Vector2Int currentPosition, int distance) = queue.Dequeue();

                foreach (Vector2Int neighbor in GetNeighbors(currentPosition))
                {
                    if (!IsWalkable(neighbor) || visited.Contains(neighbor))
                    {
                        continue;
                    }

                    if (neighbor == finish)
                    {
                        return distance + 1;
                    }

                    visited.Add(neighbor);
                    queue.Enqueue((neighbor, distance + 1));
                }
            }

            return 0;
        }

        public int GetManhattanDistance(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
        }

        private int GetIndex(Vector2Int position)
        {
            return position.y * width + position.x;
        }
    }
}