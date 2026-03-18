using UnityEngine;

namespace Algorytm.Genetyczny
{
    public enum MoveDirection
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    public static class MoveDirectionExtensions
    {
        public static Vector2Int ToVector(this MoveDirection direction)
        {
            return direction switch
            {
                MoveDirection.Up => Vector2Int.up,
                MoveDirection.Right => Vector2Int.right,
                MoveDirection.Down => Vector2Int.down,
                MoveDirection.Left => Vector2Int.left,
                _ => Vector2Int.zero
            };
        }
    }
}