using System;
using UnityEngine;

namespace Algorytm.Dane
{
    /// <summary>
    /// Przechowuje dane wejściowe i zależności potrzebne do uruchomienia algorytmu
    /// wyszukiwania ścieżki w labiryncie.
    /// </summary>
    [Serializable]
    public class MazeAlgorithmContext
    {
        /// <summary>
        /// Nazwa badanego labiryntu.
        /// </summary>
        public string mazeName;

        /// <summary>
        /// Typ badanego labiryntu.
        /// </summary>
        public string mazeType;

        /// <summary>
        /// Szerokość labiryntu w komórkach.
        /// </summary>
        public int mazeWidth;

        /// <summary>
        /// Wysokość labiryntu w komórkach.
        /// </summary>
        public int mazeHeight;

        /// <summary>
        /// Pozycja startowa w labiryncie.
        /// </summary>
        public Vector2Int startPosition;

        /// <summary>
        /// Pozycja końcowa w labiryncie.
        /// </summary>
        public Vector2Int finishPosition;

        /// <summary>
        /// Ziarno generatora liczb losowych użyte podczas działania algorytmu.
        /// </summary>
        public int randomSeed;

        /// <summary>
        /// Określa, czy działanie algorytmu powinno być wizualizowane.
        /// </summary>
        public bool enableVisualization;

        /// <summary>
        /// Opóźnienie pomiędzy kolejnymi krokami wizualizacji wyrażone w sekundach.
        /// </summary>
        public float stepDelaySeconds;

        /// <summary>
        /// Dane reprezentujące strukturę labiryntu przekazywane do algorytmu.
        /// </summary>
        public object mazeData;

        /// <summary>
        /// Komponent MonoBehaviour wykorzystywany do obsługi korutyn.
        /// </summary>
        public MonoBehaviour coroutineHost;

        /// <summary>
        /// Komponent odpowiedzialny za śledzenie statystyk FPS podczas wizualizacji.
        /// </summary>
        public FpsTracker fpsTracker;

        /// <summary>
        /// Zwraca dane labiryntu rzutowane do oczekiwanego typu referencyjnego.
        /// </summary>
        /// <typeparam name="T">Oczekiwany typ danych labiryntu.</typeparam>
        /// <returns>
        /// Dane labiryntu rzutowane do typu <typeparamref name="T"/>,
        /// albo <see langword="null"/>, jeśli rzutowanie nie jest możliwe.
        /// </returns>
        public T GetMazeData<T>() where T : class
        {
            return mazeData as T;
        }
    }
}