using System;
using System.Collections.Generic;
using UnityEngine;

namespace Algorytm.Genetyczny
{
    /// <summary>
    /// Reprezentuje siatkę labiryntu z informacją o przechodniości poszczególnych komórek.
    /// </summary>
    [Serializable]
    public class MazeGrid
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private bool[] walkableCells;

        /// <summary>
        /// Szerokość labiryntu w komórkach.
        /// </summary>
        public int Width => width;

        /// <summary>
        /// Wysokość labiryntu w komórkach.
        /// </summary>
        public int Height => height;

        /// <summary>
        /// Inicjalizuje nową siatkę labiryntu o podanych wymiarach.
        /// Wszystkie komórki są początkowo nieprzechodnie, dopóki nie zostaną jawnie ustawione.
        /// </summary>
        /// <param name="width">Szerokość labiryntu w komórkach.</param>
        /// <param name="height">Wysokość labiryntu w komórkach.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Rzucany, gdy szerokość lub wysokość jest mniejsza lub równa zero.
        /// </exception>
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

        /// <summary>
        /// Inicjalizuje nową siatkę labiryntu na podstawie dwuwymiarowej mapy przechodniości.
        /// </summary>
        /// <param name="walkableMap">Mapa określająca, które komórki są przechodnie.</param>
        /// <exception cref="ArgumentNullException">
        /// Rzucany, gdy parametr <paramref name="walkableMap"/> ma wartość null.
        /// </exception>
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

        /// <summary>
        /// Sprawdza, czy wskazana pozycja znajduje się w granicach labiryntu.
        /// </summary>
        /// <param name="position">Pozycja do sprawdzenia.</param>
        /// <returns>
        /// <see langword="true"/>, jeśli pozycja mieści się w granicach siatki;
        /// w przeciwnym razie <see langword="false"/>.
        /// </returns>
        public bool IsInside(Vector2Int position)
        {
            return position.x >= 0 &&
                   position.x < width &&
                   position.y >= 0 &&
                   position.y < height;
        }

        /// <summary>
        /// Sprawdza, czy wskazana pozycja znajduje się w granicach labiryntu i jest przechodnia.
        /// </summary>
        /// <param name="position">Pozycja do sprawdzenia.</param>
        /// <returns>
        /// <see langword="true"/>, jeśli komórka jest przechodnia;
        /// w przeciwnym razie <see langword="false"/>.
        /// </returns>
        public bool IsWalkable(Vector2Int position)
        {
            return IsInside(position) && walkableCells[GetIndex(position)];
        }

        /// <summary>
        /// Ustawia stan przechodniości wskazanej komórki.
        /// </summary>
        /// <param name="position">Pozycja komórki do zmodyfikowania.</param>
        /// <param name="isWalkable">Nowy stan przechodniości komórki.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Rzucany, gdy wskazana pozycja znajduje się poza granicami labiryntu.
        /// </exception>
        public void SetWalkable(Vector2Int position, bool isWalkable)
        {
            if (!IsInside(position))
            {
                throw new ArgumentOutOfRangeException(nameof(position), $"Position {position} is outside the maze.");
            }

            walkableCells[GetIndex(position)] = isWalkable;
        }

        /// <summary>
        /// Zwraca liczbę przechodnich sąsiadów wskazanej komórki.
        /// </summary>
        /// <param name="position">Pozycja, dla której liczona jest liczba przechodnich sąsiadów.</param>
        /// <returns>Liczba przechodnich komórek sąsiednich.</returns>
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

        /// <summary>
        /// Zwraca czterech sąsiadów ortogonalnych wskazanej pozycji.
        /// Metoda może zwracać również pozycje znajdujące się poza granicami labiryntu.
        /// </summary>
        /// <param name="position">Pozycja źródłowa.</param>
        /// <returns>Enumeracja sąsiednich pozycji.</returns>
        public IEnumerable<Vector2Int> GetNeighbors(Vector2Int position)
        {
            yield return position + Vector2Int.up;
            yield return position + Vector2Int.right;
            yield return position + Vector2Int.down;
            yield return position + Vector2Int.left;
        }

        /// <summary>
        /// Oblicza długość najkrótszej ścieżki pomiędzy pozycją startową i końcową
        /// z użyciem przeszukiwania wszerz.
        /// </summary>
        /// <param name="start">Pozycja początkowa.</param>
        /// <param name="finish">Pozycja docelowa.</param>
        /// <returns>
        /// Liczba ruchów potrzebnych do osiągnięcia celu.
        /// Zwraca 0, jeśli start jest równy mety lub jeśli ścieżka nie istnieje.
        /// </returns>
        public int GetShortestPathLength(Vector2Int start, Vector2Int finish)
        {
            if (!IsWalkable(start) || !IsWalkable(finish))
            {
                return 0;
            }

            if (start == finish)
            {
                return 0;
            }

            var visited = new HashSet<Vector2Int> { start };
            var queue = new Queue<(Vector2Int Position, int Distance)>();
            queue.Enqueue((start, 0));

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

        /// <summary>
        /// Oblicza odległość Manhattan pomiędzy dwiema pozycjami.
        /// </summary>
        /// <param name="from">Pozycja początkowa.</param>
        /// <param name="to">Pozycja końcowa.</param>
        /// <returns>Suma różnic bezwzględnych współrzędnych X i Y.</returns>
        public int GetManhattanDistance(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
        }

        /// <summary>
        /// Zwraca indeks jednowymiarowej tablicy odpowiadający wskazanej pozycji w siatce.
        /// </summary>
        /// <param name="position">Pozycja w siatce.</param>
        /// <returns>Indeks komórki w tablicy przechodniości.</returns>
        private int GetIndex(Vector2Int position)
        {
            return position.y * width + position.x;
        }
    }
}