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
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Stabilna fasada sceny labiryntu. Przechowuje serializowane referencje i lifecycle Unity,
/// a funkcje domenowe są podzielone według odpowiedzialności w Scripts/MazeApplication.
/// </summary>
public partial class MazeAppController : MonoBehaviour
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
    [SerializeField] [Min(2)] private int maxMazeSize = 40;

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
    [SerializeField] private Color traversalColor = new Color(0.95f, 0.82f, 0.22f, 1f);

    [Header("Settings Panel")]
    [SerializeField] private RectTransform settingsPanel;


    [SerializeField] private Color algorithmAPathColor = new Color(0.20f, 0.48f, 1f, 1f);
    [SerializeField] private Color algorithmBPathColor = new Color(1f, 0.72f, 0.18f, 1f);
    [SerializeField] private Color optimalPathColor = new Color(0.65f, 0.18f, 1f, 1f);
    [SerializeField] private Color visitedCellColor = new Color(0.45f, 0.65f, 1f, 1f);
    [SerializeField] private Color currentCellColor = new Color(1f, 1f, 0.25f, 1f);
    [SerializeField] private Color geneticPreviousBestColor = new Color(0.54f, 0.75f, 0.98f, 1f);
    [SerializeField] private Color geneticAgentMarkerColor = new Color(0.12f, 0.90f, 1f, 1f);
    [SerializeField] private Color antPheromoneBaseColor = new Color(1f, 0.84f, 0.48f, 1f);
    [SerializeField] private Color antAgentMarkerColor = new Color(1f, 0.42f, 0.04f, 1f);
    
    private MazeGrid currentMaze;
    private Vector2Int startPosition = new Vector2Int(0, 0);
    private Vector2Int finishPosition = new Vector2Int(9, 9);

    private Coroutine runningComparisonCoroutine;
    private Coroutine pathReplayCoroutine;
    private AlgorithmComparisonResult lastComparisonResult;
    private BenchmarkHistoryStore benchmarkHistoryStore;
    private StatsPanelController statsPanelController;

    private bool placeStartNext = true;
    private GridLayoutGroup editorGridLayout;
    private Image[,] tileImages;
    private Image[,] algorithmATileImages;
    private Image[,] algorithmBTileImages;
    private Image[,] algorithmAOptimalOverlayImages;
    private Image[,] algorithmBOptimalOverlayImages;

    private const float TileSpacing = 2f;
    private const float MinTileSize = 8f;
    private const float FallbackTileSize = 32f;
    private const float UiReferenceWidth = 2560f;
    private const float UiReferenceHeight = 1440f;
    private const float UiMatchWidthHeight = 0.5f;

    private enum EditorTool
    {
        None,
        DrawWalls,
        DeleteWalls,
        SetStartFinish
    }

    private EditorTool activeTool = EditorTool.None;
    private bool isCurrentlyDraggingTiles = false;
    private bool mazeModifiedDuringDrag = false;

    private GameObject saveDialogOverlay;
    private TMP_InputField saveNameInputField;
    private TMP_Text saveDialogInfoText;
    private Button saveDialogConfirmButton;
    private Button saveDialogCancelButton;

    private GameObject loadMazeDialogOverlay;
    private RectTransform loadMazeListContent;
    private TMP_Text loadMazeDialogInfoText;
    private RectTransform mazeSizeSelectorRoot;
    private TMP_Dropdown mazeSizeDropdown;
    private bool isSyncingMazeSizeDropdown;
    private Button resolutionButton;
    private Button languageButton;
    private Button fullscreenButton;
    private TMP_Text resolutionButtonText;
    private TMP_Text languageButtonText;
    private TMP_Text fullscreenButtonText;
    private TMP_Text resolutionLabelText;
    private TMP_Text fullscreenLabelText;
    private TMP_Text languageLabelText;
    private TMP_Text measurementHeaderText;
    private bool settingsInitialized;

    private GameObject settingsSelectionDialogOverlay;
    private RectTransform settingsSelectionListContent;
    private TMP_Text settingsSelectionTitleText;
    private Action<int> onSettingsOptionSelected;

    private enum AppLanguage
    {
        Polski = 0,
        English = 1
    }

    private struct ResolutionOption
    {
        public int width;
        public int height;

        public ResolutionOption(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public string Label => width + "x" + height;
    }

    private static readonly ResolutionOption[] ResolutionOptions =
    {
        new ResolutionOption(1280, 720),
        new ResolutionOption(1440, 810),
        new ResolutionOption(1920, 1080),
        new ResolutionOption(2560, 1440)
    };

    private const string ResolutionPrefKey = "settings_resolution_index";
    private const string FullscreenPrefKey = "settings_fullscreen";
    private const string LanguagePrefKey = "settings_language";

    private AppLanguage currentLanguage = AppLanguage.Polski;
    private int selectedResolutionIndex = 2;
    private bool isFullscreen = false;
    private string currentMazeName = string.Empty;

    private enum VisualizationTarget
    {
        None,
        AlgorithmA,
        AlgorithmB
    }

    private VisualizationTarget activeVisualizationTarget = VisualizationTarget.None;
    private readonly HashSet<Vector2Int> algorithmAVisitedTiles = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> algorithmBVisitedTiles = new HashSet<Vector2Int>();

    private const int MinSaveNameLength = 3;
    private static readonly int[] SupportedMazeSizes = { 10, 20, 30, 40 };
    private Coroutine pendingResolutionRefreshCoroutine;

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
        ConfigureAllCanvasScalers();
        TrySetupMapEditorUI();
        TrySetupRunnerUI();
        TrySetupSettingsUI();
        InitializeSettingsState();
        ArrangeResultTexts();
        ResetResultsText();
        ApplyLanguageToAllTexts();

        if (currentMaze == null)
        {
            CreateEditableMaze(mazeWidth, mazeHeight);
            return;
        }

        RebuildRunnerGrids();
        UpdateInfo("Mapa gotowa.");
    }

    private void Update()
    {
        if (isCurrentlyDraggingTiles && (Mouse.current == null || !Mouse.current.leftButton.isPressed))
        {
            FinishTileDragEditing();
        }
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
