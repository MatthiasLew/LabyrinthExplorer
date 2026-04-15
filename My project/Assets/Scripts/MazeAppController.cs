using System.Collections;
using Algorytm.Dane;
using Algorytm.Genetyczny;
using Algorytm.Mrówkowy;
using Algorytm.System;
using TMPro;
using UnityEngine;

public class MazeAppController : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private BenchmarkRunner benchmarkRunner;

    [Header("UI")]
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text wynikAText;
    [SerializeField] private TMP_Text wynikBText;

    [Header("Maze Settings")]
    [SerializeField] private int mazeWidth = 10;
    [SerializeField] private int mazeHeight = 10;
    [SerializeField] private int runCount = 3;
    [SerializeField] private bool enableVisualization = false;
    [SerializeField] private float stepDelaySeconds = 0.02f;

    private MazeGrid currentMaze;
    private Vector2Int startPosition = new Vector2Int(1, 1);
    private Vector2Int finishPosition = new Vector2Int(8, 8);

    private bool drawMode;
    private bool startFinishMode;

    private void Awake()
    {
        if (benchmarkRunner == null)
        {
            benchmarkRunner = GetComponent<BenchmarkRunner>();
        }
    }

    private void Start()
    {
        UpdateInfo("Gotowe. Kliknij 'Dodaj Labirynt', aby utworzyć dane testowe.");
    
        if (wynikAText != null)
        {
            wynikAText.text = "Algorytm A: brak wyniku";
        }

        if (wynikBText != null)
        {
            wynikBText.text = "Algorytm B: brak wyniku";
        }
    }

    public void CreateDemoMaze()
    {
        bool[,] map = new bool[mazeWidth, mazeHeight];

        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                map[x, y] = true;
            }
        }

        for (int x = 0; x < mazeWidth; x++)
        {
            map[x, 0] = false;
            map[x, mazeHeight - 1] = false;
        }

        for (int y = 0; y < mazeHeight; y++)
        {
            map[0, y] = false;
            map[mazeWidth - 1, y] = false;
        }

        if (mazeWidth > 5 && mazeHeight > 5)
        {
            map[3, 1] = false;
            map[3, 2] = false;
            map[3, 3] = false;
            map[4, 3] = false;
            map[5, 3] = false;
            map[6, 3] = false;
            map[6, 4] = false;
            map[6, 5] = false;
        }

        startPosition = new Vector2Int(1, 1);
        finishPosition = new Vector2Int(mazeWidth - 2, mazeHeight - 2);

        map[startPosition.x, startPosition.y] = true;
        map[finishPosition.x, finishPosition.y] = true;

        currentMaze = new MazeGrid(map);

        wynikAText.text = "Algorytm A: brak wyniku";
        wynikBText.text = "Algorytm B: brak wyniku";

        UpdateInfo($"Utworzono labirynt {mazeWidth}x{mazeHeight}. Start: {startPosition}, Meta: {finishPosition}");
    }

    public void ClearMaze()
    {
        currentMaze = null;
        wynikAText.text = "Algorytm A: brak wyniku";
        wynikBText.text = "Algorytm B: brak wyniku";
        UpdateInfo("Labirynt usunięty.");
    }

    public void SaveMaze()
    {
        if (currentMaze == null)
        {
            UpdateInfo("Brak labiryntu do zapisania.");
            return;
        }

        UpdateInfo("Na ten moment zapis jest tylko atrapą. Następny krok: zapis do JSON.");
    }

    public void ToggleDrawMode()
    {
        drawMode = !drawMode;
        startFinishMode = false;
        UpdateInfo($"Tryb rysowania ścian: {(drawMode ? "WŁĄCZONY" : "WYŁĄCZONY")}");
    }

    public void ToggleStartFinishMode()
    {
        startFinishMode = !startFinishMode;
        drawMode = false;
        UpdateInfo($"Tryb ustawiania start/meta: {(startFinishMode ? "WŁĄCZONY" : "WYŁĄCZONY")}");
    }

    public void RunComparison()
    {
        if (benchmarkRunner == null)
        {
            UpdateInfo("Brak BenchmarkRunner w scenie.");
            return;
        }

        if (currentMaze == null)
        {
            UpdateInfo("Najpierw utwórz labirynt.");
            return;
        }

        var context = new MazeAlgorithmContext
        {
            mazeName = "Demo Maze",
            mazeType = "Manual / Demo",
            mazeWidth = currentMaze.Width,
            mazeHeight = currentMaze.Height,
            startPosition = startPosition,
            finishPosition = finishPosition,
            randomSeed = 12345,
            enableVisualization = enableVisualization,
            stepDelaySeconds = stepDelaySeconds,
            mazeData = currentMaze,
            coroutineHost = this,
            fpsTracker = null
        };

        var genetic = new GeneticMazeAlgorithm();
        var ant = new AntColonyMazeAlgorithm();

        UpdateInfo("Trwa benchmark...");
        StartCoroutine(RunComparisonCoroutine(genetic, ant, context));
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
    }

    private void OnComparisonCompleted(AlgorithmComparisonResult result)
    {
        if (result == null)
        {
            UpdateInfo("Benchmark zakończony bez wyniku.");
            return;
        }

        wynikAText.text =
            $"{result.firstAlgorithmSummary.algorithmName}\n" +
            $"Success rate: {result.firstAlgorithmSummary.successRate:P0}\n" +
            $"Avg time: {result.firstAlgorithmSummary.averageTotalRuntimeMs:F2} ms\n" +
            $"Avg path: {result.firstAlgorithmSummary.averagePathLength:F2}\n" +
            $"Avg steps: {result.firstAlgorithmSummary.averageStepsTaken:F2}";

        wynikBText.text =
            $"{result.secondAlgorithmSummary.algorithmName}\n" +
            $"Success rate: {result.secondAlgorithmSummary.successRate:P0}\n" +
            $"Avg time: {result.secondAlgorithmSummary.averageTotalRuntimeMs:F2} ms\n" +
            $"Avg path: {result.secondAlgorithmSummary.averagePathLength:F2}\n" +
            $"Avg steps: {result.secondAlgorithmSummary.averageStepsTaken:F2}";

        UpdateInfo(
            $"Gotowe. Szybszy: {result.fasterAlgorithmName} | " +
            $"Skuteczniejszy: {result.moreReliableAlgorithmName} | " +
            $"Lepsza ścieżka: {result.betterPathAlgorithmName}");
    }

    private void UpdateInfo(string message)
    {
        if (infoText != null)
        {
            infoText.text = message;
        }

        Debug.Log(message);
    }
}