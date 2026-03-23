using System;
using UnityEngine;

namespace Algorytm.Genetyczny
{
    /// <summary>
    /// Reprezentuje kierunek ruchu w siatce labiryntu.
    /// </summary>
    public enum MoveDirection
    {
        /// <summary>
        /// Ruch w górę.
        /// </summary>
        Up = 0,

        /// <summary>
        /// Ruch w prawo.
        /// </summary>
        Right = 1,

        /// <summary>
        /// Ruch w dół.
        /// </summary>
        Down = 2,

        /// <summary>
        /// Ruch w lewo.
        /// </summary>
        Left = 3
    }

    /// <summary>
    /// Udostępnia metody pomocnicze dla typu <see cref="MoveDirection"/>.
    /// </summary>
    public static class MoveDirectionExtensions
    {
        /// <summary>
        /// Konwertuje kierunek ruchu na odpowiadający mu wektor przesunięcia w siatce.
        /// </summary>
        /// <param name="direction">Kierunek ruchu do skonwertowania.</param>
        /// <returns>Wektor przesunięcia odpowiadający wskazanemu kierunkowi.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Rzucany, gdy przekazana wartość enuma nie reprezentuje obsługiwanego kierunku.
        /// </exception>
        public static Vector2Int ToVector(this MoveDirection direction)
        {
            return direction switch
            {
                MoveDirection.Up => Vector2Int.up,
                MoveDirection.Right => Vector2Int.right,
                MoveDirection.Down => Vector2Int.down,
                MoveDirection.Left => Vector2Int.left,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported move direction.")
            };
        }
    }
}