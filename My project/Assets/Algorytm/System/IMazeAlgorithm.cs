using System.Collections;
using Algorytm.Dane;

namespace Algorytm.System
{
    /// <summary>
    /// Reprezentuje wspólny kontrakt dla algorytmów wyszukiwania ścieżki
    /// uruchamianych w ramach benchmarku labiryntu.
    /// </summary>
    public interface IMazeAlgorithm
    {
        /// <summary>
        /// Zwraca nazwę algorytmu prezentowaną w metrykach oraz wynikach benchmarku.
        /// </summary>
        string AlgorithmName { get; }

        /// <summary>
        /// Zwraca wersję implementacji algorytmu.
        /// </summary>
        string AlgorithmVersion { get; }

        /// <summary>
        /// Uruchamia algorytm dla zadanego kontekstu i zapisuje wynik do obiektu metryk.
        /// </summary>
        /// <param name="context">Kontekst wejściowy zawierający dane labiryntu i ustawienia uruchomienia.</param>
        /// <param name="metrics">Obiekt metryk uzupełniany przez implementację algorytmu.</param>
        /// <param name="profiler">Profiler odpowiedzialny za pomiar czasu i pamięci.</param>
        /// <returns>Korutyna wykonująca działanie algorytmu.</returns>
        IEnumerator Run(
            MazeAlgorithmContext context,
            AlgorithmMetrics metrics,
            AlgorithmProfiler profiler);
    }
}