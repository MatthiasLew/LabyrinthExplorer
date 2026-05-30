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
/// Układ tekstów wyniku oraz reset i stan prezentacji plansz.
/// Oddzielna odpowiedzialność fasady sceny; zachowuje kompatybilność z powiązaniami Unity Inspector.
/// </summary>
public partial class MazeAppController
{
    private void ArrangeResultTexts()
    {
        if (wynikAText != null)
        {
            wynikAText.gameObject.SetActive(true);
        }

        if (wynikBText != null)
        {
            wynikBText.gameObject.SetActive(true);
        }

        EnsureComparisonAreaLayout();
    }

    private static void HideSecondaryResultText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        text.text = string.Empty;
        text.gameObject.SetActive(false);
    }

    private static void ConfigureResultText(TMP_Text text, float fontSize)
    {
        if (text == null)
        {
            return;
        }

        text.gameObject.SetActive(true);
        text.color = Color.white;
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        text.lineSpacing = -10f;

        RectTransform rect = text.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
        }
    }

    private static void ForceReadableResultTextRect(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rect = text.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(18f, -16f);
        rect.sizeDelta = new Vector2(520f, 520f);
        rect.localScale = Vector3.one;
        rect.SetAsLastSibling();
    }

    private void EnsureComparisonAreaLayout()
    {
        RectTransform infoPanel = null;

        if (wynikAText != null)
        {
            infoPanel = wynikAText.rectTransform.parent as RectTransform;
        }

        if (infoPanel == null && wynikBText != null)
        {
            infoPanel = wynikBText.rectTransform.parent as RectTransform;
        }

        if (infoPanel == null && mazeRunnerPanel != null)
        {
            infoPanel = FindRectTransformByName(mazeRunnerPanel, "InfoPanel");
        }

        if (infoPanel == null)
        {
            return;
        }

        VerticalLayoutGroup layout = infoPanel.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = infoPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ConfigureComparisonText(infoText, 174f, 22f);
        ConfigureComparisonText(wynikAText, 198f, 21f);
        ConfigureComparisonText(wynikBText, 198f, 21f);

        LayoutRebuilder.ForceRebuildLayoutImmediate(infoPanel);
    }

    private static void ConfigureComparisonText(TMP_Text text, float preferredHeight, float fontSize)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        LayoutElement layoutElement = text.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = text.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minHeight = preferredHeight;
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleHeight = 0f;
        layoutElement.flexibleWidth = 1f;

        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Truncate;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.fontSize = fontSize;
        text.raycastTarget = false;
    }

    private void OnAlgorithmRunStarted(string algorithmName, int runIndex)
    {
        activeVisualizationTarget = ResolveVisualizationTarget(algorithmName);
        ResetTraversalForTarget(activeVisualizationTarget);
    }

    private void OnAlgorithmRunCompleted(string algorithmName, int runIndex)
    {
        activeVisualizationTarget = VisualizationTarget.None;
    }

    private VisualizationTarget ResolveVisualizationTarget(string algorithmName)
    {
        if (!string.IsNullOrWhiteSpace(algorithmName) &&
            algorithmName.IndexOf("genetic", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return VisualizationTarget.AlgorithmA;
        }

        if (!string.IsNullOrWhiteSpace(algorithmName) &&
            algorithmName.IndexOf("ant", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return VisualizationTarget.AlgorithmB;
        }

        return VisualizationTarget.None;
    }

    private void OnAlgorithmVisualizationStep(Vector2Int position)
    {
        if (activeVisualizationTarget == VisualizationTarget.None || currentMaze == null)
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

        if (!currentMaze.IsWalkable(position))
        {
            return;
        }

        Image[,] targetTiles;
        HashSet<Vector2Int> visitedSet;

        if (activeVisualizationTarget == VisualizationTarget.AlgorithmA)
        {
            targetTiles = algorithmATileImages;
            visitedSet = algorithmAVisitedTiles;
        }
        else
        {
            targetTiles = algorithmBTileImages;
            visitedSet = algorithmBVisitedTiles;
        }

        if (targetTiles == null || !visitedSet.Add(position))
        {
            return;
        }

        if (position.x < 0 || position.x >= targetTiles.GetLength(0) ||
            position.y < 0 || position.y >= targetTiles.GetLength(1))
        {
            return;
        }

        Image tile = targetTiles[position.x, position.y];
        if (tile != null)
        {
            tile.color = traversalColor;
        }
    }

    private void ResetRunnerTraversalVisualization()
    {
        ResetTraversalForTarget(VisualizationTarget.AlgorithmA);
        ResetTraversalForTarget(VisualizationTarget.AlgorithmB);
    }

    private void ResetOptimalPathOverlays()
    {
        ClearOptimalPathOverlays(algorithmAOptimalOverlayImages);
        ClearOptimalPathOverlays(algorithmBOptimalOverlayImages);
    }

    private static void ClearOptimalPathOverlays(Image[,] overlays)
    {
        if (overlays == null)
        {
            return;
        }

        for (int x = 0; x < overlays.GetLength(0); x++)
        {
            for (int y = 0; y < overlays.GetLength(1); y++)
            {
                Image overlay = overlays[x, y];
                if (overlay != null)
                {
                    overlay.gameObject.SetActive(false);
                }
            }
        }
    }

    private void ResetTraversalForTarget(VisualizationTarget target)
    {
        Image[,] targetTiles;
        HashSet<Vector2Int> visitedSet;

        if (target == VisualizationTarget.AlgorithmA)
        {
            targetTiles = algorithmATileImages;
            visitedSet = algorithmAVisitedTiles;
        }
        else if (target == VisualizationTarget.AlgorithmB)
        {
            targetTiles = algorithmBTileImages;
            visitedSet = algorithmBVisitedTiles;
        }
        else
        {
            return;
        }

        visitedSet.Clear();

        if (targetTiles == null || currentMaze == null)
        {
            return;
        }

        for (int x = 0; x < currentMaze.Width; x++)
        {
            for (int y = 0; y < currentMaze.Height; y++)
            {
                Image tile = targetTiles[x, y];
                if (tile == null)
                {
                    continue;
                }

                tile.color = GetTileColor(new Vector2Int(x, y));
            }
        }
    }
    private void InvalidateDisplayedBenchmark()
    {
        lastComparisonResult = null;
        activeVisualizationTarget = VisualizationTarget.None;

        if (pathReplayCoroutine != null)
        {
            StopCoroutine(pathReplayCoroutine);
            pathReplayCoroutine = null;
        }

        ResetRunnerTraversalVisualization();
        ResetOptimalPathOverlays();
        ResetResultsText();
    }
    

    private void ResetResultsText()
    {
        if (wynikAText != null)
        {
            wynikAText.text = currentLanguage == AppLanguage.Polski
                ? "ALGORYTM GENETYCZNY\nBrak wykonanego pomiaru."
                : "GENETIC ALGORITHM\nNo benchmark result.";
            wynikAText.gameObject.SetActive(true);
        }

        if (wynikBText != null)
        {
            wynikBText.text = currentLanguage == AppLanguage.Polski
                ? "ALGORYTM MRÓWKOWY\nBrak wykonanego pomiaru."
                : "ANT COLONY ALGORITHM\nNo benchmark result.";
            wynikBText.gameObject.SetActive(true);
        }

        if (infoText != null)
        {
            infoText.text = currentLanguage == AppLanguage.Polski
                ? "BENCHMARK\nStatus: oczekiwanie na pomiar."
                : "BENCHMARK\nStatus: waiting for benchmark.";
        }

        EnsureComparisonAreaLayout();
        UpdateMeasurementsHeaderText();
    }

}
