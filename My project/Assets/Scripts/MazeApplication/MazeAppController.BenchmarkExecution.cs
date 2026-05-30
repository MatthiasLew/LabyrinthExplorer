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
/// Start benchmarku, formatowanie podsumowania i zapis historii.
/// Oddzielna odpowiedzialność fasady sceny; zachowuje kompatybilność z powiązaniami Unity Inspector.
/// </summary>
public partial class MazeAppController
{
    public void RunComparison()
    {
        if (runningComparisonCoroutine != null)
        {
            UpdateInfo("Benchmark już działa.");
            return;
        }

        if (benchmarkRunner == null)
        {
            UpdateInfo("Brak BenchmarkRunner w scenie.");
            return;
        }

        if (!EnsureMazeExists())
        {
            UpdateInfo("Najpierw utwórz labirynt.");
            return;
        }

        if (!currentMaze.IsWalkable(startPosition) || !currentMaze.IsWalkable(finishPosition))
        {
            UpdateInfo("Start i meta muszą być na polach przechodnich.");
            return;
        }

        if (startPosition == finishPosition)
        {
            UpdateInfo("Start i meta nie mogą być na tym samym polu.");
            return;
        }

        // Przed startem sprawdzamy wyłącznie osiągalność metodą DFS.
        // BFS dla prezentowanej trasy uruchomi dopiero dany algorytm po dotarciu do mety.
        if (!currentMaze.HasReachablePath(startPosition, finishPosition))
        {
            UpdateInfo("Brak poprawnej ścieżki od startu do mety.");
            return;
        }

        int seedToUse = randomSeed == 0 ? Environment.TickCount : randomSeed;

        if (pathReplayCoroutine != null)
        {
            StopCoroutine(pathReplayCoroutine);
            pathReplayCoroutine = null;
        }

        // Pomiary wykonujemy bez opóźnień UI, aby animacja nie zakłamywała czasu algorytmów.
        // Algorytmy zapisują kolejność odkrywania pól; po pomiarze kontroler odtwarza
        // eksplorację, a następnie wynik BFS wyznaczony wyłącznie z ich odkryć.
        var context = new MazeAlgorithmContext
        {
            mazeName = GetActiveMazeDisplayName(),
            mazeType = "Manual / MapEditor",
            mazeWidth = currentMaze.Width,
            mazeHeight = currentMaze.Height,
            startPosition = startPosition,
            finishPosition = finishPosition,
            randomSeed = seedToUse,
            maxIterations = maxAlgorithmIterations > 0 ? maxAlgorithmIterations : 500,
            maxRuntimeMs = maxAlgorithmRuntimeSeconds > 0f ? maxAlgorithmRuntimeSeconds * 1000d : 10000d,
            enableVisualization = false,
            stepDelaySeconds = 0f,
            mazeData = currentMaze,
            coroutineHost = this,
            fpsTracker = null,
            onAlgorithmRunStarted = null,
            onAlgorithmRunCompleted = null,
            onVisualizationStep = null
        };

        var genetic = new GeneticMazeAlgorithm();
        var ant = new AntColonyMazeAlgorithm();

        UpdateAlgorithmTitles(genetic.AlgorithmName, ant.AlgorithmName);

        lastComparisonResult = null;
        ResetResultsText();
        RebuildRunnerGrids();
        ResetRunnerTraversalVisualization();
        ResetOptimalPathOverlays();
        UpdateInfo(currentLanguage == AppLanguage.Polski
            ? $"Trwa pomiar... Seed: {seedToUse}"
            : $"Benchmark in progress... Seed: {seedToUse}");
        runningComparisonCoroutine = StartCoroutine(RunComparisonCoroutine(genetic, ant, context));
    }

    private void UpdateAlgorithmTitles(string firstAlgorithmName, string secondAlgorithmName)
    {
        if (algorithmATitleText != null)
        {
            algorithmATitleText.text = ShortAlgorithmName(firstAlgorithmName);
            algorithmATitleText.color = Color.white;
            algorithmATitleText.fontSize = 24f;
            algorithmATitleText.enableAutoSizing = false;
            algorithmATitleText.enableWordWrapping = false;
            algorithmATitleText.overflowMode = TextOverflowModes.Overflow;
            algorithmATitleText.alignment = TextAlignmentOptions.Left;
            algorithmATitleText.raycastTarget = false;

            RectTransform rect = algorithmATitleText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
            }
        }

        if (algorithmBTitleText != null)
        {
            algorithmBTitleText.text = ShortAlgorithmName(secondAlgorithmName);
            algorithmBTitleText.color = Color.white;
            algorithmBTitleText.fontSize = 24f;
            algorithmBTitleText.enableAutoSizing = false;
            algorithmBTitleText.enableWordWrapping = false;
            algorithmBTitleText.overflowMode = TextOverflowModes.Overflow;
            algorithmBTitleText.alignment = TextAlignmentOptions.Left;
            algorithmBTitleText.raycastTarget = false;

            RectTransform rect = algorithmBTitleText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
            }
        }

        UpdateMeasurementsHeaderText();
    }

    private void SetCurrentMazeName(string mazeName)
    {
        currentMazeName = mazeName == null ? string.Empty : mazeName.Trim();
        UpdateMeasurementsHeaderText();
    }

    private string GetActiveMazeDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(currentMazeName))
        {
            return currentMazeName;
        }

        return currentLanguage == AppLanguage.Polski ? "Edytowany Labirynt" : "Edited Maze";
    }

    private void ResolveMeasurementsHeaderText()
    {
        if (measurementHeaderText != null && measurementHeaderText.gameObject.scene.IsValid())
        {
            return;
        }

        if (mazeRunnerPanel == null)
        {
            return;
        }

        TMP_Text fallbackByName = null;
        TMP_Text[] texts = mazeRunnerPanel.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || !text.gameObject.scene.IsValid())
            {
                continue;
            }

            if (text.name.Equals("TitleText", StringComparison.OrdinalIgnoreCase) && fallbackByName == null)
            {
                fallbackByName = text;
            }

            string value = text.text ?? string.Empty;
            if (value.IndexOf("Pomiary dla Labiryntu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("Measurements for Maze", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                measurementHeaderText = text;
                return;
            }
        }

        measurementHeaderText = fallbackByName;
    }

    private void UpdateMeasurementsHeaderText()
    {
        ResolveMeasurementsHeaderText();

        if (measurementHeaderText == null)
        {
            return;
        }

        string prefix = currentLanguage == AppLanguage.Polski
            ? "Pomiary dla Labiryntu - "
            : "Measurements for Maze - ";
        measurementHeaderText.text = prefix + GetActiveMazeDisplayName();
    }

    private static string ShortAlgorithmName(string algorithmName)
    {
        if (string.IsNullOrWhiteSpace(algorithmName))
        {
            return "-";
        }

        if (algorithmName.Contains("Genetic", StringComparison.OrdinalIgnoreCase))
        {
            return "Genetyczny";
        }

        if (algorithmName.Contains("Ant", StringComparison.OrdinalIgnoreCase) ||
            algorithmName.Contains("Colony", StringComparison.OrdinalIgnoreCase))
        {
            return "Mrówkowy";
        }

        return algorithmName;
    }

    private void DisplayBenchmarkResults(AlgorithmComparisonResult result)
    {
        if (result == null || currentMaze == null)
        {
            return;
        }

        lastComparisonResult = result;

        int optimalLength = currentMaze.GetShortestPathLength(startPosition, finishPosition);
        bool anySuccessfulPath =
            result.firstAlgorithmSummary.successfulRunCount > 0 ||
            result.secondAlgorithmSummary.successfulRunCount > 0;
        string betterPathText = string.IsNullOrWhiteSpace(result.betterPathAlgorithmName)
            ? (anySuccessfulPath
                ? (currentLanguage == AppLanguage.Polski ? "remis" : "tie")
                : (currentLanguage == AppLanguage.Polski ? "brak poprawnych rozwiązań" : "no successful solutions"))
            : ShortAlgorithmName(result.betterPathAlgorithmName);

        string reliabilityText = Mathf.Approximately(
            result.firstAlgorithmSummary.successRate,
            result.secondAlgorithmSummary.successRate)
                ? (currentLanguage == AppLanguage.Polski ? "remis" : "tie")
                : ShortAlgorithmName(result.moreReliableAlgorithmName);
        string speedText = string.IsNullOrWhiteSpace(result.fasterAlgorithmName)
            ? (currentLanguage == AppLanguage.Polski ? "remis" : "tie")
            : ShortAlgorithmName(result.fasterAlgorithmName);

        if (wynikAText != null)
        {
            wynikAText.text = FormatBenchmarkSummaryText(
                result.firstAlgorithmSummary,
                currentLanguage,
                "ALGORYTM GENETYCZNY",
                "GENETIC ALGORITHM");
            wynikAText.gameObject.SetActive(true);
        }

        if (wynikBText != null)
        {
            wynikBText.text = FormatBenchmarkSummaryText(
                result.secondAlgorithmSummary,
                currentLanguage,
                "ALGORYTM MRÓWKOWY",
                "ANT COLONY ALGORITHM");
            wynikBText.gameObject.SetActive(true);
        }

        if (currentLanguage == AppLanguage.Polski)
        {
            UpdateInfo(
                $"BENCHMARK — {GetActiveMazeDisplayName()} ({currentMaze.Width}x{currentMaze.Height})\n" +
                $"Próby: {result.firstAlgorithmSummary.runCount} | Kontrolne minimum całej mapy: {optimalLength}\n" +
                $"Szybszy: {speedText} | " +
                $"Niezawodny: {reliabilityText} | " +
                $"Lepsza ścieżka: {betterPathText}\n" +
                "Legenda: niebieski/jasnoniebieski = kolejne najlepsze potomstwa, pomarańczowy = ślad pokazywanych mrówek, fioletowy środek = BFS po sukcesie");
        }
        else
        {
            UpdateInfo(
                $"BENCHMARK — {GetActiveMazeDisplayName()} ({currentMaze.Width}x{currentMaze.Height})\n" +
                $"Runs: {result.firstAlgorithmSummary.runCount} | Full-maze control minimum: {optimalLength}\n" +
                $"Faster: {speedText} | " +
                $"Reliable: {reliabilityText} | " +
                $"Better path: {betterPathText}\n" +
                "Legend: blue/pale blue = successive best offspring, orange = shown ant paths, violet centre = post-success BFS");
        }

        EnsureComparisonAreaLayout();
    }

    private string FormatBenchmarkSummaryText(
        AlgorithmSummary summary,
        AppLanguage language,
        string titlePL,
        string titleEN)
    {
        if (summary == null)
        {
            return language == AppLanguage.Polski ? "Brak wyniku" : "No result";
        }

        string title = language == AppLanguage.Polski ? titlePL : titleEN;
        
        string successText = language == AppLanguage.Polski
            ? $"Skuteczność: {summary.successCount}/{summary.runCount} ({summary.successRate:P0})"
            : $"Success rate: {summary.successCount}/{summary.runCount} ({summary.successRate:P0})";

        string timeText = language == AppLanguage.Polski
            ? $"Średni czas logiki: {summary.averageLogicTimeMs:F2} ms\n" +
              $"Średni czas przebiegu: {summary.averageTotalRuntimeMs:F2} ms"
            : $"Average logic time: {summary.averageLogicTimeMs:F2} ms\n" +
              $"Average run wall time: {summary.averageTotalRuntimeMs:F2} ms";

        string pathLengthText;
        string pathEfficiencyText;

        if (summary.successfulRunCount > 0)
        {
            pathLengthText = language == AppLanguage.Polski
                ? $"Średnia surowa trasa algorytmu: {summary.averageSuccessfulPathLength:F2}\n" +
                  $"Średnia trasa BFS z odkryć: {summary.averageSuccessfulOptimizedPathLength:F2}"
                : $"Average raw algorithm route: {summary.averageSuccessfulPathLength:F2}\n" +
                  $"Average BFS route within discoveries: {summary.averageSuccessfulOptimizedPathLength:F2}";

            pathEfficiencyText = language == AppLanguage.Polski
                ? $"Efektywność surowej trasy: {summary.averageSuccessfulPathEfficiency:F2}\n" +
                  $"Efektywność po BFS z odkryć: {summary.averageSuccessfulOptimizedPathEfficiency:F2}"
                : $"Raw route efficiency: {summary.averageSuccessfulPathEfficiency:F2}\n" +
                  $"Efficiency after discovery-only BFS: {summary.averageSuccessfulOptimizedPathEfficiency:F2}";
        }
        else
        {
            pathLengthText = language == AppLanguage.Polski ? "Brak udanych przebiegów" : "No successful runs";
            pathEfficiencyText = "";
        }

        string visitedText = language == AppLanguage.Polski
            ? $"Średnia liczba odwiedzonych pól: {summary.averageVisitedCells:F1}"
            : $"Average visited cells: {summary.averageVisitedCells:F1}";

        string result = $"{title}\n" +
                        $"{successText}\n" +
                        $"{timeText}\n" +
                        $"{pathLengthText}";

        if (!string.IsNullOrEmpty(pathEfficiencyText))
        {
            result += $"\n{pathEfficiencyText}";
        }

        result += $"\n{visitedText}";

        return result;
    }

    private IEnumerator RunComparisonCoroutine(
        IMazeAlgorithm first,
        IMazeAlgorithm second,
        MazeAlgorithmContext context)
    {
        yield return benchmarkRunner.RunComparison(
            first,
            second,
            context,
            runCount,
            OnComparisonCompleted);

        runningComparisonCoroutine = null;
    }

    private void OnComparisonCompleted(AlgorithmComparisonResult result)
    {
        runningComparisonCoroutine = null;
        PaintBestPathsFromMetrics(result);

        // Save results to benchmark history
        if (benchmarkHistoryStore == null)
        {
            benchmarkHistoryStore = new BenchmarkHistoryStore();
        }

        string testId = DateTime.UtcNow.Ticks.ToString();
        benchmarkHistoryStore.AppendResults(testId, benchmarkRunner.AllMetrics);

        if (statsPanelController != null)
        {
            statsPanelController.RefreshDisplay();
        }
    }

}
