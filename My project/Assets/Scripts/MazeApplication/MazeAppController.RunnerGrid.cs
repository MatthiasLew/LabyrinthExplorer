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
/// Budowa oraz podstawowe malowanie plansz porównania algorytmów.
/// Oddzielna odpowiedzialność fasady sceny; zachowuje kompatybilność z powiązaniami Unity Inspector.
/// </summary>
public partial class MazeAppController
{
    private void TrySetupRunnerUI()
    {
        ResolveRunnerReferences();
        ApplyRunnerGridBackgrounds();
        EnsureComparisonAreaLayout();
        BindRunnerButtons();
    }

    private void ResolveRunnerReferences()
    {
        if (mazeRunnerPanel == null)
        {
            mazeRunnerPanel = FindRectTransformInScene("MazeRunnerPanel");
        }

        if (mazeRunnerPanel == null)
        {
            return;
        }

        if (algorithmASection == null)
        {
            algorithmASection = FindRectTransformByName(mazeRunnerPanel, "AlgorithmASection");
        }

        if (algorithmBSection == null)
        {
            algorithmBSection = FindRectTransformByName(mazeRunnerPanel, "AlgorithmBSection");
        }

        if (algorithmAGrid == null)
        {
            algorithmAGrid = FindRectTransformByName(mazeRunnerPanel, "AlgorithmAGrid");
        }

        if (algorithmBGrid == null)
        {
            algorithmBGrid = FindRectTransformByName(mazeRunnerPanel, "AlgorithmBGrid");
        }

        if (algorithmATitleText == null)
        {
            algorithmATitleText = FindTMPTextByName(mazeRunnerPanel, "AlgorithmATitle");
        }

        if (algorithmBTitleText == null)
        {
            algorithmBTitleText = FindTMPTextByName(mazeRunnerPanel, "AlgorithmBTitle");
        }

        if (algorithmATitleText == null)
        {
            algorithmATitleText = FindTMPTextByTextValue(mazeRunnerPanel, "Algorytm A");
        }

        if (algorithmBTitleText == null)
        {
            algorithmBTitleText = FindTMPTextByTextValue(mazeRunnerPanel, "Algorytm B");
        }
    }

    private void ApplyRunnerGridBackgrounds()
    {
        ApplyGridBackground(algorithmAGrid, runnerGridBackgroundColor);
        ApplyGridBackground(algorithmBGrid, runnerGridBackgroundColor);
    }

    private static void ApplyGridBackground(RectTransform grid, Color color)
    {
        if (grid == null)
        {
            return;
        }

        Image backgroundImage = grid.GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.color = color;
        }
    }

    private void BindRunnerButtons()
    {
        if (mazeRunnerPanel == null)
        {
            return;
        }

        BindButton(mazeRunnerPanel, "BtnAddMaze", ShowLoadMazeDialog);
        BindButton(mazeRunnerPanel, "BtnStartMeasurements", RunComparison);
    }

    private void RebuildRunnerGrids()
    {
        if (currentMaze == null)
        {
            return;
        }

        TrySetupRunnerUI();

        BuildRunnerGrid(
            algorithmAGrid,
            out algorithmATileImages,
            out algorithmAOptimalOverlayImages);

        BuildRunnerGrid(
            algorithmBGrid,
            out algorithmBTileImages,
            out algorithmBOptimalOverlayImages);
    }

    private void BuildRunnerGrid(
        RectTransform targetGrid,
        out Image[,] targetTileImages,
        out Image[,] targetOptimalOverlayImages)
    {
        targetTileImages = null;
        targetOptimalOverlayImages = null;

        if (targetGrid == null || currentMaze == null)
        {
            return;
        }

        GridLayoutGroup layout = targetGrid.GetComponent<GridLayoutGroup>();
        if (layout == null)
        {
            layout = targetGrid.gameObject.AddComponent<GridLayoutGroup>();
        }

        ClearGridVisuals(targetGrid);

        Canvas.ForceUpdateCanvases();
        float tileSize = CalculateTileSize(targetGrid, currentMaze.Width, currentMaze.Height);

        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = currentMaze.Width;
        layout.cellSize = new Vector2(tileSize, tileSize);
        layout.spacing = new Vector2(TileSpacing, TileSpacing);
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.childAlignment = TextAnchor.UpperLeft;

        targetTileImages = new Image[currentMaze.Width, currentMaze.Height];
        targetOptimalOverlayImages = new Image[currentMaze.Width, currentMaze.Height];

        for (int y = currentMaze.Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < currentMaze.Width; x++)
            {
                Vector2Int position = new Vector2Int(x, y);
                Image tileImage = CreateRunnerTile(targetGrid, position, out Image optimalOverlay);
                tileImage.color = GetTileColor(position);
                targetTileImages[position.x, position.y] = tileImage;
                targetOptimalOverlayImages[position.x, position.y] = optimalOverlay;
            }
        }
    }

    private Image CreateRunnerTile(RectTransform parent, Vector2Int position, out Image optimalOverlay)
    {
        GameObject tileObject = new GameObject(
            $"RunnerTile_{position.x}_{position.y}",
            typeof(RectTransform),
            typeof(Image));

        tileObject.layer = parent.gameObject.layer;
        tileObject.transform.SetParent(parent, false);

        Image backgroundImage = tileObject.GetComponent<Image>();
        backgroundImage.raycastTarget = false;

        GameObject overlayObject = new GameObject(
            "OptimalPathOverlay",
            typeof(RectTransform),
            typeof(Image));

        overlayObject.layer = parent.gameObject.layer;
        overlayObject.transform.SetParent(tileObject.transform, false);

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0.28f, 0.28f);
        overlayRect.anchorMax = new Vector2(0.72f, 0.72f);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        optimalOverlay = overlayObject.GetComponent<Image>();
        optimalOverlay.color = optimalPathColor;
        optimalOverlay.raycastTarget = false;
        optimalOverlay.gameObject.SetActive(false);

        return backgroundImage;
    }

    private Color GetTileColor(Vector2Int position)
    {
        if (position == startPosition)
        {
            return startColor;
        }

        if (position == finishPosition)
        {
            return finishColor;
        }

        return currentMaze != null && currentMaze.IsWalkable(position) ? walkableColor : wallColor;
    }
    
    private void ResetRunnerGridColors(Image[,] grid)
{
    if (grid == null || currentMaze == null)
    {
        return;
    }

    for (int x = 0; x < currentMaze.Width; x++)
    {
        for (int y = 0; y < currentMaze.Height; y++)
        {
            Image tile = grid[x, y];

            if (tile == null)
            {
                continue;
            }

            Vector2Int position = new Vector2Int(x, y);
            tile.color = GetTileColor(position);
        }
    }
}

private void PaintRunnerCell(Image[,] grid, Vector2Int position, Color color)
{
    if (grid == null || currentMaze == null)
    {
        return;
    }

    if (!currentMaze.IsInside(position))
    {
        return;
    }

    if (position == startPosition || position == finishPosition)
    {
        return;
    }

    Image tile = grid[position.x, position.y];

    if (tile == null)
    {
        return;
    }

    tile.color = color;
}

private void DrawPath(Image[,] grid, IReadOnlyList<Vector2Int> path, Color color)
{
    if (grid == null || path == null)
    {
        return;
    }

    foreach (Vector2Int position in path)
    {
        PaintRunnerCell(grid, position, color);
    }
}

private void DrawBenchmarkPaths(string algorithmAName, string algorithmBName)
{
    if (currentMaze == null || benchmarkRunner == null)
    {
        return;
    }

    ResetRunnerGridColors(algorithmATileImages);
    ResetRunnerGridColors(algorithmBTileImages);

    AlgorithmMetrics bestAlgorithmAResult = GetBestMetricsForAlgorithm(algorithmAName);
    AlgorithmMetrics bestAlgorithmBResult = GetBestMetricsForAlgorithm(algorithmBName);

    if (bestAlgorithmAResult != null)
    {
        DrawPath(algorithmATileImages, bestAlgorithmAResult.finalPath, algorithmAPathColor);
    }

    if (bestAlgorithmBResult != null)
    {
        DrawPath(algorithmBTileImages, bestAlgorithmBResult.finalPath, algorithmBPathColor);
    }

    // Nie rysujemy globalnej trasy BFS na obu planszach. finalPath każdego wyniku
    // jest jego własną trasą BFS ograniczoną do komórek odkrytych przez ten algorytm.
    if (bestAlgorithmAResult != null)
    {
        PaintOptimalPathOverlay(algorithmAOptimalOverlayImages, bestAlgorithmAResult.finalPath);
    }

    if (bestAlgorithmBResult != null)
    {
        PaintOptimalPathOverlay(algorithmBOptimalOverlayImages, bestAlgorithmBResult.finalPath);
    }
}

private AlgorithmMetrics GetBestMetricsForAlgorithm(string algorithmName)
{
    if (benchmarkRunner == null)
    {
        return null;
    }

    AlgorithmMetrics bestMetrics = null;

    foreach (AlgorithmMetrics metrics in benchmarkRunner.AllMetrics)
    {
        if (metrics == null)
        {
            continue;
        }

        if (metrics.algorithmName != algorithmName)
        {
            continue;
        }

        if (metrics.finalPath == null || metrics.finalPath.Count == 0)
        {
            continue;
        }

        if (bestMetrics == null || IsBetterMetrics(metrics, bestMetrics))
        {
            bestMetrics = metrics;
        }
    }

    return bestMetrics;
}

private static bool IsBetterMetrics(AlgorithmMetrics candidate, AlgorithmMetrics currentBest)
{
    if (candidate.reachedGoal != currentBest.reachedGoal)
    {
        return candidate.reachedGoal;
    }

    if (!Mathf.Approximately(candidate.pathEfficiency, currentBest.pathEfficiency))
    {
        return candidate.pathEfficiency > currentBest.pathEfficiency;
    }

    if (candidate.pathLength > 0 && currentBest.pathLength > 0 && candidate.pathLength != currentBest.pathLength)
    {
        return candidate.pathLength < currentBest.pathLength;
    }

    return candidate.totalRuntimeMs < currentBest.totalRuntimeMs;
}

}
