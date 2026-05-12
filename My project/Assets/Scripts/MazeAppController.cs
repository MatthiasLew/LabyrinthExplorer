using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Algorytm.Dane;
using Algorytm.Genetyczny;
using Algorytm.Mrówkowy;
using Algorytm.System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MazeAppController : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private BenchmarkRunner benchmarkRunner;

    [Header("UI")]
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text wynikAText;
    [SerializeField] private TMP_Text wynikBText;
    [SerializeField] private TMP_Text algorithmATitleText;
    [SerializeField] private TMP_Text algorithmBTitleText;

    [Header("Maze Settings")]
    [SerializeField] private int mazeWidth = 10;
    [SerializeField] private int mazeHeight = 10;
    [SerializeField] private int runCount = 3;
    [SerializeField] private bool enableVisualization = false;
    [SerializeField] private float stepDelaySeconds = 0.02f;

    [Header("Map Editor References (optional)")]
    [SerializeField] private RectTransform mapEditorPanel;
    [SerializeField] private RectTransform gridSection;
    [SerializeField] private RectTransform buttonsSection;
    [SerializeField] private RectTransform editorGrid;

    [Header("Map Editor Limits")]
    [SerializeField] [Min(2)] private int minMazeSize = 2;
    [SerializeField] [Min(2)] private int maxMazeSize = 64;

    [Header("Random Maze")]
    [SerializeField] [Range(0f, 0.9f)] private float randomWallChance = 0.30f;
    [SerializeField] private bool randomizeBorderWalls = false;
    [SerializeField] private int randomSeed = 0;

    [Header("Map Editor Colors")]
    [SerializeField] private Color walkableColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color wallColor = new Color(0.14f, 0.14f, 0.14f, 1f);
    [SerializeField] private Color startColor = new Color(0.18f, 0.72f, 0.34f, 1f);
    [SerializeField] private Color finishColor = new Color(0.84f, 0.24f, 0.24f, 1f);
    [SerializeField] private Color editorGridBackgroundColor = new Color(0.42f, 0.42f, 0.42f, 1f);

    [Header("Save Dialog")]
    [SerializeField] private Color saveDialogOverlayColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color saveDialogPanelColor = new Color(0.16f, 0.16f, 0.16f, 1f);
    [SerializeField] private Color saveDialogInputColor = new Color(0.24f, 0.24f, 0.24f, 1f);

    [Header("Runner View")]
    [SerializeField] private RectTransform mazeRunnerPanel;
    [SerializeField] private RectTransform algorithmASection;
    [SerializeField] private RectTransform algorithmBSection;
    [SerializeField] private RectTransform algorithmAGrid;
    [SerializeField] private RectTransform algorithmBGrid;
    [SerializeField] private Color runnerGridBackgroundColor = new Color(0.34f, 0.34f, 0.34f, 1f);
    [SerializeField] private Color algorithmAPathColor = new Color(0.20f, 0.48f, 1f, 1f);
    [SerializeField] private Color algorithmBPathColor = new Color(1f, 0.72f, 0.18f, 1f);
    [SerializeField] private Color optimalPathColor = new Color(0.65f, 0.18f, 1f, 1f);
    [SerializeField] private Color visitedCellColor = new Color(0.45f, 0.65f, 1f, 1f);
    [SerializeField] private Color currentCellColor = new Color(1f, 1f, 0.25f, 1f);
    
    private MazeGrid currentMaze;
    private Vector2Int startPosition = new Vector2Int(0, 0);
    private Vector2Int finishPosition = new Vector2Int(9, 9);

    private Coroutine runningComparisonCoroutine;

    private bool placeStartNext = true;
    private GridLayoutGroup editorGridLayout;
    private Image[,] tileImages;
    private Image[,] algorithmATileImages;
    private Image[,] algorithmBTileImages;

    private const float TileSpacing = 2f;
    private const float MinTileSize = 8f;
    private const float FallbackTileSize = 32f;

    private enum EditorTool
    {
        None,
        DrawWalls,
        DeleteWalls,
        SetStartFinish
    }

    private EditorTool activeTool = EditorTool.None;

    private GameObject saveDialogOverlay;
    private TMP_InputField saveNameInputField;
    private TMP_Text saveDialogInfoText;
    private Button saveDialogConfirmButton;
    private Button saveDialogCancelButton;

    private GameObject loadMazeDialogOverlay;
    private RectTransform loadMazeListContent;
    private TMP_Text loadMazeDialogInfoText;

    private const int MinSaveNameLength = 3;

    [Serializable]
    private struct MazeSaveData
    {
        public string mazeName;
        public int width;
        public int height;
        public int startX;
        public int startY;
        public int finishX;
        public int finishY;
        public bool[] walkableCells;
        public string savedUtc;
    }

    private void Awake()
    {
        if (benchmarkRunner == null)
        {
            benchmarkRunner = GetComponent<BenchmarkRunner>();
        }
    }

    private void Start()
    {
        TrySetupMapEditorUI();
        TrySetupRunnerUI();
        ArrangeResultTexts();
        ResetResultsText();

        if (currentMaze == null)
        {
            CreateEditableMaze(mazeWidth, mazeHeight);
            return;
        }

        RebuildRunnerGrids();
        UpdateInfo("Mapa gotowa.");
    }

    public void CreateDemoMaze()
    {
        CreateEditableMaze(mazeWidth, mazeHeight);
    }

    public void CreateMazeFromSize(int width, int height)
    {
        mazeWidth = Mathf.Clamp(width, minMazeSize, maxMazeSize);
        mazeHeight = Mathf.Clamp(height, minMazeSize, maxMazeSize);
        CreateEditableMaze(mazeWidth, mazeHeight);
    }

    public void SetMazeWidth(int width)
    {
        mazeWidth = Mathf.Clamp(width, minMazeSize, maxMazeSize);
    }

    public void SetMazeHeight(int height)
    {
        mazeHeight = Mathf.Clamp(height, minMazeSize, maxMazeSize);
    }

    public void SetMazeWidthFromString(string widthValue)
    {
        if (int.TryParse(widthValue, out int parsed))
        {
            SetMazeWidth(parsed);
        }
    }

    public void SetMazeHeightFromString(string heightValue)
    {
        if (int.TryParse(heightValue, out int parsed))
        {
            SetMazeHeight(parsed);
        }
    }

    public void ClearMaze()
    {
        currentMaze = null;
        tileImages = null;
        activeTool = EditorTool.None;

        if (runningComparisonCoroutine != null)
        {
            StopCoroutine(runningComparisonCoroutine);
            runningComparisonCoroutine = null;
        }

        ClearEditorGridVisuals();
        ClearGridVisuals(algorithmAGrid);
        ClearGridVisuals(algorithmBGrid);

        ResetResultsText();
        UpdateInfo("Labirynt usunięty.");
    }

    public void SaveMaze()
    {
        if (!EnsureMazeExists())
        {
            UpdateInfo("Brak labiryntu do zapisu.");
            return;
        }

        ShowSaveDialog();
    }

    public void ToggleDrawMode()
    {
        SetTool(EditorTool.DrawWalls, "Tryb rysowania ścian.");
    }

    public void EnableDeleteMode()
    {
        SetTool(EditorTool.DeleteWalls, "Tryb usuwania ścian.");
    }

    public void ToggleStartFinishMode()
    {
        string next = placeStartNext ? "START" : "FINISH";
        SetTool(EditorTool.SetStartFinish, $"Tryb start/meta. Następny punkt: {next}.");
    }

    public void GenerateRandomMaze()
    {
        if (!EnsureMazeExists())
        {
            return;
        }

        int seedToUse = randomSeed == 0 ? Environment.TickCount : randomSeed;
        var rng = new System.Random(seedToUse);

        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                bool isBorder = x == 0 || y == 0 || x == mazeWidth - 1 || y == mazeHeight - 1;
                bool shouldCreateWall = rng.NextDouble() < randomWallChance;

                if (isBorder && !randomizeBorderWalls)
                {
                    shouldCreateWall = false;
                }

                currentMaze.SetWalkable(new Vector2Int(x, y), !shouldCreateWall);
            }
        }

        currentMaze.SetWalkable(startPosition, true);
        currentMaze.SetWalkable(finishPosition, true);

        RefreshAllTiles();
        RebuildRunnerGrids();
        ResetResultsText();

        UpdateInfo($"Wygenerowano labirynt.\nSeed: {seedToUse}");
    }

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

        int shortestPathLength = currentMaze.GetShortestPathLength(startPosition, finishPosition);
        if (shortestPathLength <= 0)
        {
            UpdateInfo("Brak poprawnej ścieżki od startu do mety.");
            return;
        }

        var context = new MazeAlgorithmContext
        {
            mazeName = "Edited Maze",
            mazeType = "Manual / MapEditor",
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

        UpdateAlgorithmTitles(genetic.AlgorithmName, ant.AlgorithmName);

        ResetResultsText();
        RebuildRunnerGrids();

        UpdateInfo(
            "Status: pomiar w toku\n\n" +
            $"Najkrótsza ścieżka: {shortestPathLength}\n" +
            $"Liczba prób: {runCount}");

        runningComparisonCoroutine = StartCoroutine(RunComparisonCoroutine(genetic, ant, context));
    }

    private void CreateEditableMaze(int width, int height)
    {
        width = Mathf.Clamp(width, minMazeSize, maxMazeSize);
        height = Mathf.Clamp(height, minMazeSize, maxMazeSize);

        mazeWidth = width;
        mazeHeight = height;

        currentMaze = new MazeGrid(mazeWidth, mazeHeight);

        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                currentMaze.SetWalkable(new Vector2Int(x, y), true);
            }
        }

        startPosition = new Vector2Int(0, 0);
        finishPosition = new Vector2Int(mazeWidth - 1, mazeHeight - 1);
        currentMaze.SetWalkable(startPosition, true);
        currentMaze.SetWalkable(finishPosition, true);

        placeStartNext = true;
        activeTool = EditorTool.DrawWalls;

        RebuildEditorGridVisuals();
        RebuildRunnerGrids();
        ResetResultsText();

        UpdateInfo($"Utworzono labirynt {mazeWidth}x{mazeHeight}.");
    }

    private bool EnsureMazeExists()
    {
        if (currentMaze != null)
        {
            return true;
        }

        CreateEditableMaze(mazeWidth, mazeHeight);
        return currentMaze != null;
    }

    private void SetTool(EditorTool tool, string infoMessage)
    {
        activeTool = tool;
        UpdateInfo(infoMessage);
    }

    private void RebuildEditorGridVisuals()
    {
        TrySetupMapEditorUI();

        if (editorGrid == null)
        {
            return;
        }

        EnsureEditorGridLayout();
        ClearEditorGridVisuals();

        tileImages = new Image[mazeWidth, mazeHeight];

        Canvas.ForceUpdateCanvases();
        float tileSize = CalculateTileSize();

        editorGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        editorGridLayout.constraintCount = mazeWidth;
        editorGridLayout.cellSize = new Vector2(tileSize, tileSize);
        editorGridLayout.spacing = new Vector2(TileSpacing, TileSpacing);
        editorGridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        editorGridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        editorGridLayout.childAlignment = TextAnchor.UpperLeft;

        for (int y = mazeHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < mazeWidth; x++)
            {
                Vector2Int tilePosition = new Vector2Int(x, y);
                CreateTile(tilePosition);
            }
        }

        RefreshAllTiles();
    }

    private void CreateTile(Vector2Int position)
    {
        GameObject tileObject = new GameObject(
            $"Tile_{position.x}_{position.y}",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));

        tileObject.layer = editorGrid.gameObject.layer;
        tileObject.transform.SetParent(editorGrid, false);

        Image tileImage = tileObject.GetComponent<Image>();
        tileImage.color = walkableColor;

        Button tileButton = tileObject.GetComponent<Button>();
        Navigation navigation = tileButton.navigation;
        navigation.mode = Navigation.Mode.None;
        tileButton.navigation = navigation;
        tileButton.onClick.AddListener(() => OnTileClicked(position));

        tileImages[position.x, position.y] = tileImage;
    }

    private void OnTileClicked(Vector2Int position)
    {
        if (currentMaze == null || !currentMaze.IsInside(position))
        {
            return;
        }

        switch (activeTool)
        {
            case EditorTool.DrawWalls:
                SetWallAt(position, true);
                break;

            case EditorTool.DeleteWalls:
                SetWallAt(position, false);
                break;

            case EditorTool.SetStartFinish:
                PlaceStartOrFinish(position);
                break;

            default:
                UpdateInfo("Najpierw wybierz narzędzie.");
                break;
        }
    }

    private void SetWallAt(Vector2Int position, bool makeWall)
    {
        if (position == startPosition || position == finishPosition)
        {
            UpdateInfo("Nie można ustawić ściany na starcie lub mecie.");
            return;
        }

        bool shouldBeWalkable = !makeWall;
        if (currentMaze.IsWalkable(position) == shouldBeWalkable)
        {
            return;
        }

        currentMaze.SetWalkable(position, shouldBeWalkable);
        RefreshTile(position);
        RebuildRunnerGrids();
        ResetResultsText();
    }

    private void PlaceStartOrFinish(Vector2Int position)
    {
        if (currentMaze == null)
        {
            return;
        }

        Vector2Int previousMarker;

        if (placeStartNext)
        {
            if (position == finishPosition)
            {
                UpdateInfo("Start nie może być na mecie.");
                return;
            }

            previousMarker = startPosition;
            startPosition = position;
            currentMaze.SetWalkable(position, true);
            placeStartNext = false;

            RefreshTile(previousMarker);
            RefreshTile(startPosition);
            RebuildRunnerGrids();
            ResetResultsText();

            UpdateInfo($"Start ustawiony: {startPosition}\nNastępnie ustaw metę.");
            return;
        }

        if (position == startPosition)
        {
            UpdateInfo("Meta nie może być na starcie.");
            return;
        }

        previousMarker = finishPosition;
        finishPosition = position;
        currentMaze.SetWalkable(position, true);
        placeStartNext = true;

        RefreshTile(previousMarker);
        RefreshTile(finishPosition);
        RebuildRunnerGrids();
        ResetResultsText();

        UpdateInfo($"Meta ustawiona: {finishPosition}\nNastępnie ustaw start.");
    }

    private void RefreshAllTiles()
    {
        if (tileImages == null)
        {
            return;
        }

        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                RefreshTile(new Vector2Int(x, y));
            }
        }
    }

    private void RefreshTile(Vector2Int position)
    {
        if (tileImages == null || currentMaze == null)
        {
            return;
        }

        if (position.x < 0 || position.x >= mazeWidth || position.y < 0 || position.y >= mazeHeight)
        {
            return;
        }

        Image tileImage = tileImages[position.x, position.y];
        if (tileImage == null)
        {
            return;
        }

        if (position == startPosition)
        {
            tileImage.color = startColor;
            return;
        }

        if (position == finishPosition)
        {
            tileImage.color = finishColor;
            return;
        }

        tileImage.color = currentMaze.IsWalkable(position) ? walkableColor : wallColor;
    }

    private void TrySetupMapEditorUI()
    {
        ResolveEditorReferences();
        EnsureEditorGridLayout();
        ApplyEditorGridBackground();
        BindEditorButtons();
    }

    private void ResolveEditorReferences()
    {
        if (mapEditorPanel == null)
        {
            mapEditorPanel = FindRectTransformInScene("MapEditorPanel");
        }

        if (mapEditorPanel == null)
        {
            return;
        }

        if (gridSection == null)
        {
            gridSection = FindRectTransformByName(mapEditorPanel, "GridSection");
        }

        if (gridSection == null)
        {
            gridSection = FindRectTransformByName(mapEditorPanel, "EditorArea");
        }

        if (gridSection == null)
        {
            gridSection = FindRectTransformByName(mapEditorPanel, "EdtiorArea");
        }

        if (buttonsSection == null)
        {
            buttonsSection = FindRectTransformByName(mapEditorPanel, "ButtonsSection");
        }

        if (buttonsSection == null)
        {
            buttonsSection = FindRectTransformByName(mapEditorPanel, "ButtonSection");
        }

        if (editorGrid == null)
        {
            editorGrid = FindRectTransformByName(mapEditorPanel, "EditorGrid");
        }

        if (editorGrid == null && gridSection != null)
        {
            editorGrid = FindRectTransformByName(gridSection, "EditorGrid");
        }

        if (editorGrid == null)
        {
            editorGrid = gridSection;
        }
    }

    private void EnsureEditorGridLayout()
    {
        if (editorGrid == null)
        {
            return;
        }

        editorGridLayout = editorGrid.GetComponent<GridLayoutGroup>();
        if (editorGridLayout == null)
        {
            editorGridLayout = editorGrid.gameObject.AddComponent<GridLayoutGroup>();
        }
    }

    private void ApplyEditorGridBackground()
    {
        if (editorGrid == null)
        {
            return;
        }

        Image backgroundImage = editorGrid.GetComponent<Image>();
        if (backgroundImage == null)
        {
            return;
        }

        backgroundImage.color = editorGridBackgroundColor;
    }

    private void BindEditorButtons()
    {
        if (mapEditorPanel == null)
        {
            return;
        }

        Transform buttonsRoot = buttonsSection != null ? buttonsSection : mapEditorPanel;

        BindButton(buttonsRoot, "BtnDraw", ToggleDrawMode);
        BindButton(buttonsRoot, "BtnDelete", EnableDeleteMode);
        BindButton(buttonsRoot, "BtnDeleteMaze", EnableDeleteMode);
        BindButton(buttonsRoot, "BtnStartEnd", ToggleStartFinishMode);
        BindButton(buttonsRoot, "BtnRandomGen", GenerateRandomMaze);
        BindButton(buttonsRoot, "BtnSave", SaveMaze);

        BindButton(buttonsRoot, "BtnAddMaze", CreateDemoMaze);
        BindButton(mapEditorPanel, "BtnStartMeasurements", RunComparison);
    }

    private void TrySetupRunnerUI()
    {
        ResolveRunnerReferences();
        ApplyRunnerGridBackgrounds();
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

        BuildRunnerGrid(algorithmAGrid, out algorithmATileImages);
        BuildRunnerGrid(algorithmBGrid, out algorithmBTileImages);
    }

    private void BuildRunnerGrid(RectTransform targetGrid, out Image[,] runnerTiles)
    {
        runnerTiles = null;

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

        runnerTiles = new Image[currentMaze.Width, currentMaze.Height];

        for (int y = currentMaze.Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < currentMaze.Width; x++)
            {
                Vector2Int position = new Vector2Int(x, y);
                Image tileImage = CreateRunnerTile(targetGrid, position);
                tileImage.color = GetTileColor(position);

                runnerTiles[x, y] = tileImage;
            }
        }
    }

    private Image CreateRunnerTile(RectTransform parent, Vector2Int position)
    {
        GameObject tileObject = new GameObject(
            $"RunnerTile_{position.x}_{position.y}",
            typeof(RectTransform),
            typeof(Image));

        tileObject.layer = parent.gameObject.layer;
        tileObject.transform.SetParent(parent, false);

        Image image = tileObject.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
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

    List<Vector2Int> optimalPath = currentMaze.GetShortestPath(startPosition, finishPosition);

    DrawPath(algorithmATileImages, optimalPath, optimalPathColor);
    DrawPath(algorithmBTileImages, optimalPath, optimalPathColor);
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

    private void ShowLoadMazeDialog()
    {
        TrySetupRunnerUI();

        if (mazeRunnerPanel == null)
        {
            UpdateInfo("Nie znaleziono panelu labiryntu.");
            return;
        }

        string[] files = GetSavedMazeFiles();
        if (files.Length == 0)
        {
            UpdateInfo("Brak zapisanych labiryntów.");
            return;
        }

        EnsureLoadMazeDialogBuilt();
        PopulateLoadMazeDialog(files);

        if (loadMazeDialogOverlay != null)
        {
            loadMazeDialogOverlay.SetActive(true);
            loadMazeDialogOverlay.transform.SetAsLastSibling();
        }
    }

    private void HideLoadMazeDialog()
    {
        if (loadMazeDialogOverlay != null)
        {
            loadMazeDialogOverlay.SetActive(false);
        }
    }

    private void EnsureLoadMazeDialogBuilt()
    {
        if (loadMazeDialogOverlay != null)
        {
            return;
        }

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null || mazeRunnerPanel == null)
        {
            UpdateInfo("Nie można utworzyć okna ładowania.");
            return;
        }

        loadMazeDialogOverlay = new GameObject("LoadMazeDialog", typeof(RectTransform), typeof(Image));
        RectTransform overlayRect = loadMazeDialogOverlay.GetComponent<RectTransform>();
        overlayRect.SetParent(mazeRunnerPanel, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = loadMazeDialogOverlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.6f);
        overlayImage.raycastTarget = true;

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(overlayRect, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(760f, 640f);
        panelRect.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = saveDialogPanelColor;

        TMP_Text title = CreateDialogLabel(
            panelRect,
            "Load Maze",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -22f),
            new Vector2(680f, 48f),
            defaultFont,
            34f,
            TextAlignmentOptions.Center,
            Color.white);

        TMP_Text info = CreateDialogLabel(
            panelRect,
            "Choose a saved maze:",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -74f),
            new Vector2(680f, 36f),
            defaultFont,
            22f,
            TextAlignmentOptions.Center,
            Color.white);
        loadMazeDialogInfoText = info;

        GameObject scrollRoot = new GameObject("ListScrollRoot", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        RectTransform scrollRectTransform = scrollRoot.GetComponent<RectTransform>();
        scrollRectTransform.SetParent(panelRect, false);
        scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
        scrollRectTransform.sizeDelta = new Vector2(680f, 430f);
        scrollRectTransform.anchoredPosition = new Vector2(0f, -20f);
        scrollRoot.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 1f);

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.SetParent(scrollRectTransform, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(8f, 8f);
        viewportRect.offsetMax = new Vector2(-8f, -8f);
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.04f);
        viewportObject.GetComponent<Mask>().showMaskGraphic = false;

        GameObject contentObject = new GameObject(
            "Content",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.SetParent(viewportRect, false);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup listLayout = contentObject.GetComponent<VerticalLayoutGroup>();
        listLayout.padding = new RectOffset(0, 0, 0, 0);
        listLayout.spacing = 8f;
        listLayout.childAlignment = TextAnchor.UpperCenter;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect scrollRect = scrollRoot.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        CreateDialogButton(panelRect, "Close", new Vector2(0f, -284f), HideLoadMazeDialog, defaultFont);

        loadMazeListContent = contentRect;
        loadMazeDialogOverlay.SetActive(false);

        if (title != null)
        {
            title.raycastTarget = false;
        }
    }

    private void PopulateLoadMazeDialog(string[] files)
    {
        if (loadMazeListContent == null)
        {
            return;
        }

        ClearGridVisuals(loadMazeListContent);

        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null)
        {
            return;
        }

        for (int i = 0; i < files.Length; i++)
        {
            string path = files[i];
            CreateLoadListButton(loadMazeListContent, defaultFont, path);
        }

        if (loadMazeDialogInfoText != null)
        {
            loadMazeDialogInfoText.text = $"Choose a saved maze ({files.Length}):";
            loadMazeDialogInfoText.color = Color.white;
        }
    }

    private void CreateLoadListButton(RectTransform parent, TMP_FontAsset font, string filePath)
    {
        string label = Path.GetFileNameWithoutExtension(filePath);

        GameObject buttonObject = new GameObject(
            "MazeItem_" + label,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(parent, false);
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(0.5f, 1f);
        buttonRect.sizeDelta = new Vector2(0f, 56f);

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 56f;
        layoutElement.flexibleWidth = 1f;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.24f, 0.24f, 0.24f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => LoadMazeFromFile(filePath));

        TextMeshProUGUI text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.SetParent(buttonRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 0f);
        textRect.offsetMax = new Vector2(-14f, 0f);
        text.font = font;
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.Left;
        text.color = Color.white;
        text.text = label;
        text.raycastTarget = false;
    }

    private void LoadMazeFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            if (loadMazeDialogInfoText != null)
            {
                loadMazeDialogInfoText.text = "Selected file does not exist.";
                loadMazeDialogInfoText.color = new Color(1f, 0.45f, 0.45f, 1f);
            }

            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            MazeSaveData data = JsonUtility.FromJson<MazeSaveData>(json);

            if (data.width <= 0 || data.height <= 0 || data.walkableCells == null)
            {
                throw new InvalidDataException("Invalid maze data.");
            }

            if (data.width < minMazeSize || data.width > maxMazeSize ||
                data.height < minMazeSize || data.height > maxMazeSize)
            {
                throw new InvalidDataException(
                    $"Maze size {data.width}x{data.height} is outside supported range {minMazeSize}-{maxMazeSize}.");
            }

            int requiredCellCount = data.width * data.height;
            if (data.walkableCells.Length != requiredCellCount)
            {
                throw new InvalidDataException("Maze cell count mismatch.");
            }

            mazeWidth = data.width;
            mazeHeight = data.height;
            currentMaze = new MazeGrid(mazeWidth, mazeHeight);

            int index = 0;
            for (int y = 0; y < mazeHeight; y++)
            {
                for (int x = 0; x < mazeWidth; x++)
                {
                    bool walkable = data.walkableCells[index];
                    currentMaze.SetWalkable(new Vector2Int(x, y), walkable);
                    index++;
                }
            }

            startPosition = new Vector2Int(
                Mathf.Clamp(data.startX, 0, mazeWidth - 1),
                Mathf.Clamp(data.startY, 0, mazeHeight - 1));

            finishPosition = new Vector2Int(
                Mathf.Clamp(data.finishX, 0, mazeWidth - 1),
                Mathf.Clamp(data.finishY, 0, mazeHeight - 1));

            if (startPosition == finishPosition)
            {
                finishPosition = new Vector2Int(mazeWidth - 1, mazeHeight - 1);
                if (finishPosition == startPosition)
                {
                    finishPosition = new Vector2Int(0, mazeHeight - 1);
                }
            }

            currentMaze.SetWalkable(startPosition, true);
            currentMaze.SetWalkable(finishPosition, true);

            RebuildEditorGridVisuals();
            RebuildRunnerGrids();
            ResetResultsText();

            HideLoadMazeDialog();
            UpdateInfo($"Wczytano labirynt:\n{Path.GetFileNameWithoutExtension(filePath)}");
        }
        catch (Exception ex)
        {
            if (loadMazeDialogInfoText != null)
            {
                loadMazeDialogInfoText.text = $"Failed to load: {ex.Message}";
                loadMazeDialogInfoText.color = new Color(1f, 0.45f, 0.45f, 1f);
            }
        }
    }

    private static string[] GetSavedMazeFiles()
    {
        string directoryPath = GetSaveDirectoryPath();
        if (!Directory.Exists(directoryPath))
        {
            return Array.Empty<string>();
        }

        return Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly);
    }

    private static TMP_Text CreateDialogLabel(
        RectTransform parent,
        string textValue,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        TMP_FontAsset font,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color)
    {
        TextMeshProUGUI text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        text.font = font;
        text.text = textValue;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;

        return text;
    }

    private static void BindButton(Transform root, string buttonName, UnityAction action)
    {
        Transform buttonTransform = FindChildByName(root, buttonName);
        if (buttonTransform == null)
        {
            return;
        }

        Button button = buttonTransform.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
    }

    private static RectTransform FindRectTransformByName(Transform root, string nameToFind)
    {
        Transform child = FindChildByName(root, nameToFind);
        return child as RectTransform;
    }

    private static TMP_Text FindTMPTextByName(Transform root, string nameToFind)
    {
        Transform child = FindChildByName(root, nameToFind);
        if (child == null)
        {
            return null;
        }

        return child.GetComponent<TMP_Text>();
    }

    private static TMP_Text FindTMPTextByTextValue(Transform root, string textValue)
    {
        if (root == null)
        {
            return null;
        }

        TMP_Text text = root.GetComponent<TMP_Text>();
        if (text != null && text.text == textValue)
        {
            return text;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            TMP_Text found = FindTMPTextByTextValue(root.GetChild(i), textValue);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform root, string nameToFind)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == nameToFind)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = FindChildByName(root.GetChild(i), nameToFind);
            if (child != null)
            {
                return child;
            }
        }

        return null;
    }

    private static RectTransform FindRectTransformInScene(string objectName)
    {
        RectTransform[] allRectTransforms = Resources.FindObjectsOfTypeAll<RectTransform>();

        foreach (RectTransform rectTransform in allRectTransforms)
        {
            if (rectTransform.name != objectName)
            {
                continue;
            }

            if (!rectTransform.gameObject.scene.IsValid())
            {
                continue;
            }

            return rectTransform;
        }

        return null;
    }

    private float CalculateTileSize()
    {
        return CalculateTileSize(editorGrid, mazeWidth, mazeHeight);
    }

    private float CalculateTileSize(RectTransform targetGrid, int width, int height)
    {
        if (targetGrid == null || width <= 0 || height <= 0)
        {
            return FallbackTileSize;
        }

        Rect rect = targetGrid.rect;

        if (rect.width <= 0f || rect.height <= 0f)
        {
            return FallbackTileSize;
        }

        float horizontalTileSize = (rect.width - (width - 1) * TileSpacing) / width;
        float verticalTileSize = (rect.height - (height - 1) * TileSpacing) / height;

        float size = Mathf.Min(horizontalTileSize, verticalTileSize);
        if (size <= 0f)
        {
            return FallbackTileSize;
        }

        return Mathf.Max(MinTileSize, Mathf.Floor(size));
    }

    private void ClearEditorGridVisuals()
    {
        ClearGridVisuals(editorGrid);
    }

    private static void ClearGridVisuals(RectTransform grid)
    {
        if (grid == null)
        {
            return;
        }

        for (int i = grid.childCount - 1; i >= 0; i--)
        {
            Destroy(grid.GetChild(i).gameObject);
        }
    }

    private void ShowSaveDialog()
    {
        if (mapEditorPanel == null)
        {
            UpdateInfo("Nie znaleziono panelu edytora.");
            return;
        }

        EnsureSaveDialogBuilt();
        if (saveDialogOverlay == null)
        {
            UpdateInfo("Nie można utworzyć okna zapisu.");
            return;
        }

        saveDialogOverlay.SetActive(true);
        saveDialogOverlay.transform.SetAsLastSibling();

        if (saveNameInputField != null)
        {
            saveNameInputField.text = string.Empty;
            saveNameInputField.ActivateInputField();
        }

        if (saveDialogInfoText != null)
        {
            saveDialogInfoText.text = $"Enter maze name (minimum {MinSaveNameLength} characters).";
            saveDialogInfoText.color = Color.white;
        }
    }

    private void HideSaveDialog()
    {
        if (saveDialogOverlay != null)
        {
            saveDialogOverlay.SetActive(false);
        }
    }

    private void EnsureSaveDialogBuilt()
    {
        if (saveDialogOverlay != null)
        {
            return;
        }

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null)
        {
            UpdateInfo("Brakuje domyślnej czcionki TMP.");
            return;
        }

        saveDialogOverlay = new GameObject("SaveMazeDialog", typeof(RectTransform), typeof(Image));
        RectTransform overlayRect = saveDialogOverlay.GetComponent<RectTransform>();
        overlayRect.SetParent(mapEditorPanel, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = saveDialogOverlay.GetComponent<Image>();
        overlayImage.color = saveDialogOverlayColor;
        overlayImage.raycastTarget = true;

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(overlayRect, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(680f, 300f);
        panelRect.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = saveDialogPanelColor;

        GameObject titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.SetParent(panelRect, false);
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(620f, 48f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);

        TMP_Text titleText = titleObject.GetComponent<TextMeshProUGUI>();
        titleText.font = defaultFont;
        titleText.text = "Save Maze";
        titleText.fontSize = 34f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        GameObject inputRoot = new GameObject("NameInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        RectTransform inputRect = inputRoot.GetComponent<RectTransform>();
        inputRect.SetParent(panelRect, false);
        inputRect.anchorMin = new Vector2(0.5f, 0.5f);
        inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.pivot = new Vector2(0.5f, 0.5f);
        inputRect.sizeDelta = new Vector2(560f, 56f);
        inputRect.anchoredPosition = new Vector2(0f, 20f);

        Image inputBackground = inputRoot.GetComponent<Image>();
        inputBackground.color = saveDialogInputColor;

        TMP_InputField inputField = inputRoot.GetComponent<TMP_InputField>();
        inputField.targetGraphic = inputBackground;
        inputField.characterLimit = 64;

        RectTransform textArea = new GameObject("TextArea", typeof(RectTransform)).GetComponent<RectTransform>();
        textArea.SetParent(inputRect, false);
        textArea.anchorMin = Vector2.zero;
        textArea.anchorMax = Vector2.one;
        textArea.offsetMin = new Vector2(12f, 8f);
        textArea.offsetMax = new Vector2(-12f, -8f);

        TextMeshProUGUI textComponent = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        RectTransform textRect = textComponent.GetComponent<RectTransform>();
        textRect.SetParent(textArea, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textComponent.font = defaultFont;
        textComponent.fontSize = 28f;
        textComponent.alignment = TextAlignmentOptions.Left;
        textComponent.color = Color.white;
        textComponent.text = string.Empty;

        TextMeshProUGUI placeholder = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.SetParent(textArea, false);
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;
        placeholder.font = defaultFont;
        placeholder.fontSize = 24f;
        placeholder.alignment = TextAlignmentOptions.Left;
        placeholder.color = new Color(1f, 1f, 1f, 0.45f);
        placeholder.text = "Maze name...";

        inputField.textViewport = textArea;
        inputField.textComponent = textComponent;
        inputField.placeholder = placeholder;

        GameObject infoObject = new GameObject("InfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform infoRect = infoObject.GetComponent<RectTransform>();
        infoRect.SetParent(panelRect, false);
        infoRect.anchorMin = new Vector2(0.5f, 0.5f);
        infoRect.anchorMax = new Vector2(0.5f, 0.5f);
        infoRect.pivot = new Vector2(0.5f, 0.5f);
        infoRect.sizeDelta = new Vector2(600f, 40f);
        infoRect.anchoredPosition = new Vector2(0f, -26f);

        saveDialogInfoText = infoObject.GetComponent<TextMeshProUGUI>();
        saveDialogInfoText.font = defaultFont;
        saveDialogInfoText.fontSize = 20f;
        saveDialogInfoText.alignment = TextAlignmentOptions.Center;
        saveDialogInfoText.color = Color.white;
        saveDialogInfoText.text = string.Empty;

        saveDialogConfirmButton = CreateDialogButton(panelRect, "Save", new Vector2(130f, -100f), ConfirmSaveFromDialog, defaultFont);
        saveDialogCancelButton = CreateDialogButton(panelRect, "Cancel", new Vector2(-130f, -100f), HideSaveDialog, defaultFont);

        saveNameInputField = inputField;
        saveDialogOverlay.SetActive(false);
    }

    private Button CreateDialogButton(
        RectTransform parent,
        string label,
        Vector2 anchoredPosition,
        UnityAction onClick,
        TMP_FontAsset font)
    {
        GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(parent, false);
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(200f, 52f);
        buttonRect.anchoredPosition = anchoredPosition;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.28f, 0.28f, 0.28f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.SetParent(buttonRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.font = font;
        text.text = label;
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        return button;
    }

    private void ConfirmSaveFromDialog()
    {
        if (!EnsureMazeExists() || saveNameInputField == null)
        {
            return;
        }

        string mazeName = saveNameInputField.text == null ? string.Empty : saveNameInputField.text.Trim();

        if (mazeName.Length < MinSaveNameLength)
        {
            SetSaveDialogInfo($"Name must have at least {MinSaveNameLength} characters.", true);
            return;
        }

        if (ContainsInvalidFileNameChars(mazeName))
        {
            SetSaveDialogInfo("Name contains invalid characters for file name.", true);
            return;
        }

        string fullPath = GetMazeSavePath(mazeName);
        if (File.Exists(fullPath))
        {
            SetSaveDialogInfo("Maze with this name already exists. Choose another name.", true);
            return;
        }

        try
        {
            Directory.CreateDirectory(GetSaveDirectoryPath());
            MazeSaveData data = BuildSaveData(mazeName);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(fullPath, json);

            HideSaveDialog();
            UpdateInfo($"Zapisano labirynt:\n{mazeName}");
        }
        catch (Exception ex)
        {
            SetSaveDialogInfo($"Save failed: {ex.Message}", true);
        }
    }

    private void SetSaveDialogInfo(string message, bool isError)
    {
        if (saveDialogInfoText == null)
        {
            return;
        }

        saveDialogInfoText.text = message;
        saveDialogInfoText.color = isError ? new Color(1f, 0.45f, 0.45f, 1f) : Color.white;
    }

    private static bool ContainsInvalidFileNameChars(string name)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidChars.Length; i++)
        {
            if (name.IndexOf(invalidChars[i]) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetSaveDirectoryPath()
    {
        return Path.Combine(Application.persistentDataPath, "SavedMazes");
    }

    private static string GetMazeSavePath(string mazeName)
    {
        return Path.Combine(GetSaveDirectoryPath(), mazeName + ".json");
    }

    private MazeSaveData BuildSaveData(string mazeName)
    {
        var data = new MazeSaveData
        {
            mazeName = mazeName,
            width = currentMaze.Width,
            height = currentMaze.Height,
            startX = startPosition.x,
            startY = startPosition.y,
            finishX = finishPosition.x,
            finishY = finishPosition.y,
            walkableCells = new bool[currentMaze.Width * currentMaze.Height],
            savedUtc = DateTime.UtcNow.ToString("O")
        };

        int index = 0;
        for (int y = 0; y < currentMaze.Height; y++)
        {
            for (int x = 0; x < currentMaze.Width; x++)
            {
                data.walkableCells[index] = currentMaze.IsWalkable(new Vector2Int(x, y));
                index++;
            }
        }

        return data;
    }

    private void UpdateAlgorithmTitles(string firstAlgorithmName, string secondAlgorithmName)
    {
        if (algorithmATitleText != null)
        {
            algorithmATitleText.text = ShortAlgorithmName(firstAlgorithmName);
            algorithmATitleText.color = Color.white;
            algorithmATitleText.fontSize = 18f;
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
            algorithmBTitleText.fontSize = 18f;
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

    private void PaintBestPathsFromMetrics(AlgorithmComparisonResult result)
    {
        if (result == null || benchmarkRunner == null)
        {
            return;
        }

        AlgorithmMetrics firstMetrics = FindBestMetricsForAlgorithm(result.firstAlgorithmSummary.algorithmName);
        AlgorithmMetrics secondMetrics = FindBestMetricsForAlgorithm(result.secondAlgorithmSummary.algorithmName);

        PaintPath(algorithmATileImages, firstMetrics?.finalPath, algorithmAPathColor);
        PaintPath(algorithmBTileImages, secondMetrics?.finalPath, algorithmBPathColor);
    }

    private AlgorithmMetrics FindBestMetricsForAlgorithm(string algorithmName)
    {
        if (benchmarkRunner.AllMetrics == null)
        {
            return null;
        }

        AlgorithmMetrics best = null;

        foreach (AlgorithmMetrics metrics in benchmarkRunner.AllMetrics)
        {
            if (metrics == null || metrics.algorithmName != algorithmName)
            {
                continue;
            }

            if (metrics.finalPath == null || metrics.finalPath.Count == 0)
            {
                continue;
            }

            if (best == null)
            {
                best = metrics;
                continue;
            }

            if (metrics.reachedGoal && !best.reachedGoal)
            {
                best = metrics;
                continue;
            }

            if (metrics.reachedGoal == best.reachedGoal &&
                metrics.pathEfficiency > best.pathEfficiency)
            {
                best = metrics;
                continue;
            }

            if (metrics.reachedGoal == best.reachedGoal &&
                Mathf.Approximately(metrics.pathEfficiency, best.pathEfficiency) &&
                metrics.totalRuntimeMs < best.totalRuntimeMs)
            {
                best = metrics;
            }
        }

        return best;
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

   private void ArrangeResultTexts()
{
    HideSecondaryResultText(wynikAText);
    HideSecondaryResultText(wynikBText);

    if (infoText == null)
    {
        return;
    }

    ConfigureResultText(infoText, 20f);
    ForceReadableResultTextRect(infoText);
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

    // WAŻNE:
    // false, bo przy zbyt wąskim RectTransform TMP robi pionową kolumnę liter.
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

    // Nie używamy stretch na rodzicu, bo u Ciebie RectTransform najwyraźniej dostaje złą szerokość.
    // Wymuszamy duży, czytelny obszar tekstu.
    rect.anchorMin = new Vector2(0f, 1f);
    rect.anchorMax = new Vector2(0f, 1f);
    rect.pivot = new Vector2(0f, 1f);

    rect.anchoredPosition = new Vector2(18f, -16f);
    rect.sizeDelta = new Vector2(520f, 520f);
    rect.localScale = Vector3.one;

    rect.SetAsLastSibling();
}

private void ResetResultsText()
{
    ArrangeResultTexts();

    if (infoText != null)
    {
        infoText.text =
            "STATUS: oczekiwanie\n\n" +
            "Kliknij „Rozpocznij Pomiar”,\n" +
            "aby uruchomić benchmark.";
    }
}

private IEnumerator RunComparisonCoroutine(
    IMazeAlgorithm firstAlgorithm,
    IMazeAlgorithm secondAlgorithm,
    MazeAlgorithmContext context)
{
    yield return benchmarkRunner.RunComparison(
        firstAlgorithm,
        secondAlgorithm,
        context,
        runCount,
        OnComparisonCompleted);

    DrawBenchmarkPaths(firstAlgorithm.AlgorithmName, secondAlgorithm.AlgorithmName);

    runningComparisonCoroutine = null;
} 
private void OnComparisonCompleted(AlgorithmComparisonResult result)
{
    if (result == null)
    {
        UpdateInfo("Benchmark zakończył się bez wyniku.");
        return;
    }

    UpdateAlgorithmTitles(
        result.firstAlgorithmSummary.algorithmName,
        result.secondAlgorithmSummary.algorithmName);

    ArrangeResultTexts();

    if (infoText != null)
    {
        infoText.text = BuildBenchmarkReport(result);
    }

    PaintBestPathsFromMetrics(result);

    Debug.Log(
        $"Done. Faster: {result.fasterAlgorithmName} | " +
        $"More reliable: {result.moreReliableAlgorithmName} | " +
        $"Better path: {result.betterPathAlgorithmName}");
}

private string BuildBenchmarkReport(AlgorithmComparisonResult result)
{
    return
        $"STATUS: zakończono\n" +
        $"Szybszy: {ShortAlgorithmName(result.fasterAlgorithmName)}\n" +
        $"Stabil.: {ShortAlgorithmName(result.moreReliableAlgorithmName)}\n" +
        $"Ścieżka: {ShortAlgorithmName(result.betterPathAlgorithmName)}\n\n" +

        $"{ShortAlgorithmName(result.firstAlgorithmSummary.algorithmName).ToUpperInvariant()}\n" +
        $"Skut.: {result.firstAlgorithmSummary.successRate:P0}\n" +
        $"Czas: {result.firstAlgorithmSummary.averageTotalRuntimeMs:F2} ms\n" +
        $"Śr. droga: {result.firstAlgorithmSummary.averagePathLength:F2}\n" +
        $"Śr. kroki: {result.firstAlgorithmSummary.averageStepsTaken:F2}\n\n" +

        $"{ShortAlgorithmName(result.secondAlgorithmSummary.algorithmName).ToUpperInvariant()}\n" +
        $"Skut.: {result.secondAlgorithmSummary.successRate:P0}\n" +
        $"Czas: {result.secondAlgorithmSummary.averageTotalRuntimeMs:F2} ms\n" +
        $"Śr. droga: {result.secondAlgorithmSummary.averagePathLength:F2}\n" +
        $"Śr. kroki: {result.secondAlgorithmSummary.averageStepsTaken:F2}";
}

private void UpdateInfo(string message)
{
    ArrangeResultTexts();

    if (infoText != null)
    {
        infoText.text = message;
    }

    Debug.Log(message);
}

}