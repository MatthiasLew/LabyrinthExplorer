using System.Collections;
using Algorytm.Dane;

namespace Algorytm.System
{
    public interface IMazeAlgorithm
    {
        string AlgorithmName { get; }
        string AlgorithmVersion { get; }

        IEnumerator Run(
            MazeAlgorithmContext context,
            AlgorithmMetrics metrics,
            AlgorithmProfiler profiler);
    }
}