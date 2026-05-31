using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Algorytm.Dane;
using Algorytm.Genetyczny;
using Algorytm.Mrówkowy;
using Algorytm.System;
using Statistics;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Odtwarzanie wizualne przebiegu genetycznego i mrówkowego oraz warstwa BFS.
/// Oddzielna odpowiedzialność fasady sceny; zachowuje kompatybilność z powiązaniami Unity Inspector.
/// </summary>
public partial class MazeAppController
{
    private void PaintBestPathsFromMetrics(AlgorithmComparisonResult result)
    {
        if (result == null || benchmarkRunner == null || currentMaze == null)
        {
            return;
        }

        if (pathReplayCoroutine != null)
        {
            StopCoroutine(pathReplayCoroutine);
        }

        pathReplayCoroutine = StartCoroutine(AnimateBestPathsFromMetrics(result));
    }

    /// <summary>
    /// Odtwarza algorytmy w dwóch różnych, czytelnych modelach prezentacji:
    /// genetyczny pokazuje kolejne nowe najlepsze potomstwa, natomiast mrówkowy
    /// pokazuje najlepszą mrówkę każdej iteracji i narastanie śladu feromonowego.
    /// Dopiero po sukcesie rysowana jest trasa BFS ograniczona do odkryć algorytmu.
    /// Czas tej animacji nie jest doliczany do czasu benchmarku.
    /// </summary>
    private IEnumerator AnimateBestPathsFromMetrics(AlgorithmComparisonResult result)
    {
        activeVisualizationTarget = VisualizationTarget.None;
        ResetRunnerTraversalVisualization();
        ResetOptimalPathOverlays();

        AlgorithmMetrics geneticMetrics =
            FindBestMetricsForAlgorithm(result.firstAlgorithmSummary.algorithmName);
        AlgorithmMetrics antMetrics =
            FindBestMetricsForAlgorithm(result.secondAlgorithmSummary.algorithmName);

        IReadOnlyList<AlgorithmReplaySegment> geneticSegments = geneticMetrics != null
            ? geneticMetrics.replaySegments
            : null;
        IReadOnlyList<AlgorithmReplaySegment> antSegments = antMetrics != null
            ? antMetrics.replaySegments
            : null;

        int geneticSegmentCount = geneticSegments != null ? geneticSegments.Count : 0;
        int antSegmentCount = antSegments != null ? antSegments.Count : 0;
        int maximumSegmentCount = Mathf.Max(geneticSegmentCount, antSegmentCount);
        float motionDelay = Mathf.Clamp(stepDelaySeconds * 0.55f, 0.006f, 0.035f);
        float bfsDelay = Mathf.Clamp(stepDelaySeconds, 0.012f, 0.08f);

        var geneticHistory = new HashSet<Vector2Int>();
        var antHeat = new Dictionary<Vector2Int, int>();
        Vector2Int? geneticMarker = null;
        Vector2Int? antMarker = null;

        string geneticRunInfo = BuildReplayRunInfo(geneticMetrics);
        string antRunInfo = BuildReplayRunInfo(antMetrics);

        UpdateInfo(currentLanguage == AppLanguage.Polski
            ? $"SYMULACJA WYBRANYCH PRZEBIEGÓW\nGenetyczny: {geneticRunInfo}. Mrówkowy: {antRunInfo}.\nGdy algorytm nie osiągnął mety, animowana jest jego najlepsza próba bez fioletowej trasy końcowej."
            : $"SELECTED RUN REPLAY\nGenetic: {geneticRunInfo}. Ant Colony: {antRunInfo}.\nWhen an algorithm failed to reach the goal, its best failed attempt is replayed without a violet final route.");

        for (int segmentIndex = 0; segmentIndex < maximumSegmentCount; segmentIndex++)
        {
            AlgorithmReplaySegment geneticSegment = segmentIndex < geneticSegmentCount
                ? geneticSegments[segmentIndex]
                : null;
            AlgorithmReplaySegment antSegment = segmentIndex < antSegmentCount
                ? antSegments[segmentIndex]
                : null;

            if (geneticSegment != null)
            {
                FadeGeneticHistory(geneticHistory);
                SetGeneticReplayText(geneticSegment);
            }

            if (antSegment != null)
            {
                SetAntReplayText(antSegment);
            }

            int geneticPathLength = geneticSegment != null && geneticSegment.path != null
                ? geneticSegment.path.Count
                : 0;
            int antPathLength = antSegment != null && antSegment.path != null
                ? antSegment.path.Count
                : 0;
            int maximumPathLength = Mathf.Max(geneticPathLength, antPathLength);
            int animationStride = Mathf.Max(1, Mathf.CeilToInt(maximumPathLength / 250f));

            for (int pathIndex = 0; pathIndex < maximumPathLength; pathIndex++)
            {
                if (pathIndex < geneticPathLength)
                {
                    Vector2Int position = geneticSegment.path[pathIndex];
                    PaintRunnerCell(algorithmATileImages, position, algorithmAPathColor);
                    geneticHistory.Add(position);
                    MoveReplayMarker(
                        algorithmAOptimalOverlayImages,
                        ref geneticMarker,
                        position,
                        geneticAgentMarkerColor);
                }

                if (pathIndex < antPathLength)
                {
                    Vector2Int position = antSegment.path[pathIndex];
                    PaintAntPheromoneStep(position, antHeat);
                    MoveReplayMarker(
                        algorithmBOptimalOverlayImages,
                        ref antMarker,
                        position,
                        antAgentMarkerColor);
                }

                if (pathIndex % animationStride == 0 || pathIndex == maximumPathLength - 1)
                {
                    yield return new WaitForSeconds(motionDelay);
                }
            }

            ClearReplayMarker(algorithmAOptimalOverlayImages, ref geneticMarker);
            ClearReplayMarker(algorithmBOptimalOverlayImages, ref antMarker);
            yield return new WaitForSeconds(0.10f);
        }

        ResetOptimalPathOverlays();
        yield return new WaitForSeconds(0.25f);

        IReadOnlyList<Vector2Int> geneticPath = geneticMetrics != null ? geneticMetrics.finalPath : null;
        IReadOnlyList<Vector2Int> antPath = antMetrics != null ? antMetrics.finalPath : null;
        int geneticPathCount = geneticPath != null ? geneticPath.Count : 0;
        int antPathCount = antPath != null ? antPath.Count : 0;
        bool hasOptimizedPath = geneticPathCount > 0 || antPathCount > 0;

        UpdateInfo(currentLanguage == AppLanguage.Polski
            ? (hasOptimizedPath
                ? "OPTYMALIZACJA PO SUKCESIE\nFioletowy środek = BFS po wszystkich polach rzeczywiście odwiedzonych przez dany algorytm w jego budżecie pracy."
                : "BRAK TRASY KOŃCOWEJ\nŻaden algorytm nie osiągnął mety; pokazano wyłącznie najlepsze próby eksploracji.")
            : (hasOptimizedPath
                ? "POST-SUCCESS OPTIMIZATION\nViolet centre = BFS over all cells genuinely visited by the given algorithm within its work budget."
                : "NO FINAL ROUTE\nNeither algorithm reached the goal; only the best exploration attempts were replayed."));
        int maximumFinalPathCount = Mathf.Max(geneticPathCount, antPathCount);

        for (int pathIndex = 0; pathIndex < maximumFinalPathCount; pathIndex++)
        {
            if (pathIndex < geneticPathCount)
            {
                PaintAlgorithmBfsStep(
                    algorithmATileImages,
                    algorithmAOptimalOverlayImages,
                    geneticPath[pathIndex],
                    algorithmAPathColor);
            }

            if (pathIndex < antPathCount)
            {
                PaintAlgorithmBfsStep(
                    algorithmBTileImages,
                    algorithmBOptimalOverlayImages,
                    antPath[pathIndex],
                    algorithmBPathColor);
            }

            yield return new WaitForSeconds(bfsDelay);
        }

        DisplayBenchmarkResults(result);
        pathReplayCoroutine = null;
    }

    private string BuildReplayRunInfo(AlgorithmMetrics metrics)
    {
        if (metrics == null)
        {
            return currentLanguage == AppLanguage.Polski ? "brak przebiegu" : "no run";
        }

        string status = metrics.reachedGoal
            ? (currentLanguage == AppLanguage.Polski ? "sukces" : "success")
            : (currentLanguage == AppLanguage.Polski ? "najlepsza próba bez sukcesu" : "best failed attempt");

        return $"run {metrics.runIndex + 1}, seed {metrics.randomSeed}, {status}";
    }

    private void SetGeneticReplayText(AlgorithmReplaySegment segment)
    {
        if (wynikAText == null || segment == null)
        {
            return;
        }

        wynikAText.text = currentLanguage == AppLanguage.Polski
            ? $"ALGORYTM GENETYCZNY\nPokolenie: {segment.iteration}\n{(segment.reachedGoal ? "Potomstwo dotarło do mety." : "Nowe najlepsze potomstwo.")}"
            : $"GENETIC ALGORITHM\nGeneration: {segment.iteration}\n{(segment.reachedGoal ? "Offspring reached the goal." : "New best offspring.")}";
    }

    private void SetAntReplayText(AlgorithmReplaySegment segment)
    {
        if (wynikBText == null || segment == null)
        {
            return;
        }

        string antDescriptionPl = segment.reachedGoal
            ? $"Mrówka {Mathf.Max(1, segment.agentIndex)}/{40} dotarła do mety."
            : "Najlepsza mrówka iteracji — wzmacnianie feromonu.";
        string antDescriptionEn = segment.reachedGoal
            ? $"Ant {Mathf.Max(1, segment.agentIndex)}/{40} reached the goal."
            : "Best ant of iteration — pheromone reinforcement.";

        wynikBText.text = currentLanguage == AppLanguage.Polski
            ? $"ALGORYTM MRÓWKOWY\nIteracja: {segment.iteration}\n{antDescriptionPl}"
            : $"ANT COLONY ALGORITHM\nIteration: {segment.iteration}\n{antDescriptionEn}";
    }

    private void FadeGeneticHistory(IEnumerable<Vector2Int> positions)
    {
        if (positions == null)
        {
            return;
        }

        foreach (Vector2Int position in positions)
        {
            PaintRunnerCell(algorithmATileImages, position, geneticPreviousBestColor);
        }
    }

    private void PaintAntPheromoneStep(Vector2Int position, Dictionary<Vector2Int, int> heat)
    {
        if (heat == null || position == startPosition || position == finishPosition)
        {
            return;
        }

        int value = heat.TryGetValue(position, out int previousValue)
            ? previousValue + 1
            : 1;
        heat[position] = value;

        float strength = Mathf.Clamp01(0.25f + value * 0.16f);
        Color pheromoneColor = Color.Lerp(antPheromoneBaseColor, algorithmBPathColor, strength);
        PaintRunnerCell(algorithmBTileImages, position, pheromoneColor);
    }

    private void MoveReplayMarker(
        Image[,] overlays,
        ref Vector2Int? previousPosition,
        Vector2Int position,
        Color markerColor)
    {
        ClearReplayMarker(overlays, ref previousPosition);

        if (overlays == null || currentMaze == null || !currentMaze.IsInside(position) ||
            position == startPosition || position == finishPosition)
        {
            previousPosition = position;
            return;
        }

        Image marker = overlays[position.x, position.y];
        if (marker != null)
        {
            marker.color = markerColor;
            marker.gameObject.SetActive(true);
        }

        previousPosition = position;
    }

    private void ClearReplayMarker(Image[,] overlays, ref Vector2Int? previousPosition)
    {
        if (overlays != null && currentMaze != null && previousPosition.HasValue &&
            currentMaze.IsInside(previousPosition.Value))
        {
            Image marker = overlays[previousPosition.Value.x, previousPosition.Value.y];
            if (marker != null)
            {
                marker.gameObject.SetActive(false);
            }
        }

        previousPosition = null;
    }

    private void PaintAlgorithmBfsStep(
        Image[,] targetTiles,
        Image[,] overlays,
        Vector2Int position,
        Color pathColor)
    {
        PaintRunnerCell(targetTiles, position, pathColor);

        if (overlays == null || currentMaze == null || !currentMaze.IsInside(position) ||
            position == startPosition || position == finishPosition)
        {
            return;
        }

        Image overlay = overlays[position.x, position.y];
        if (overlay != null)
        {
            overlay.color = optimalPathColor;
            overlay.gameObject.SetActive(true);
        }
    }

    private AlgorithmMetrics FindBestMetricsForAlgorithm(string algorithmName)
    {
        if (benchmarkRunner == null || benchmarkRunner.AllMetrics == null)
        {
            return null;
        }

        AlgorithmMetrics bestSuccessful = null;
        AlgorithmMetrics bestFailedAttempt = null;

        foreach (AlgorithmMetrics metrics in benchmarkRunner.AllMetrics)
        {
            if (metrics == null || metrics.algorithmName != algorithmName)
            {
                continue;
            }

            if (metrics.reachedGoal && metrics.finalPath != null && metrics.finalPath.Count > 0)
            {
                if (bestSuccessful == null ||
                    metrics.pathEfficiency > bestSuccessful.pathEfficiency ||
                    (Mathf.Approximately(metrics.pathEfficiency, bestSuccessful.pathEfficiency) &&
                     metrics.logicTimeMs < bestSuccessful.logicTimeMs))
                {
                    bestSuccessful = metrics;
                }

                continue;
            }

            if (metrics.replaySegments == null || metrics.replaySegments.Count == 0)
            {
                continue;
            }

            if (bestFailedAttempt == null ||
                metrics.bestFitness > bestFailedAttempt.bestFitness ||
                (Mathf.Approximately(metrics.bestFitness, bestFailedAttempt.bestFitness) &&
                 metrics.bestDistanceToGoal < bestFailedAttempt.bestDistanceToGoal))
            {
                bestFailedAttempt = metrics;
            }
        }

        return bestSuccessful ?? bestFailedAttempt;
    }

    private void PaintPath(Image[,] targetTiles, IReadOnlyList<Vector2Int> path, Color pathColor)
    {
        if (targetTiles == null || path == null || currentMaze == null)
        {
            return;
        }

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int position = path[i];

            if (!currentMaze.IsInside(position))
            {
                continue;
            }

            if (position == startPosition || position == finishPosition)
            {
                continue;
            }

            Image tileImage = targetTiles[position.x, position.y];
            if (tileImage == null)
            {
                continue;
            }

            tileImage.color = pathColor;
        }
    }
    private void PaintOptimalPathOverlay(Image[,] overlays, IReadOnlyList<Vector2Int> path)
    {
        if (overlays == null || path == null || currentMaze == null)
        {
            return;
        }

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int position = path[i];

            if (!currentMaze.IsInside(position) ||
                position == startPosition ||
                position == finishPosition)
            {
                continue;
            }

            Image overlay = overlays[position.x, position.y];
            if (overlay != null)
            {
                overlay.gameObject.SetActive(true);
            }
        }
    }

}
