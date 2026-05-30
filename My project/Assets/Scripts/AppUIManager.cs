using Presentation;
using Statistics;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Odpowiada wyłącznie za nawigację między panelami aplikacji i inicjalizację
/// dedykowanych presenterów paneli. Logika labiryntu nie należy do tej klasy.
/// </summary>
public sealed class AppUIManager : MonoBehaviour
{
    public enum PanelType
    {
        MazeRunner,
        MapEditor,
        Stats,
        Settings
    }

    public static PanelType panelToOpen = PanelType.MazeRunner;
    public static bool openAppSceneFromMainMenu;

    [Header("Panels")]
    [SerializeField] private GameObject mazeRunnerPanel;
    [SerializeField] private GameObject mapEditorPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Statistics")]
    [SerializeField] private StatsPanelController statsPanelController;

    private UiReadabilityService readabilityService;

    private void Awake()
    {
        readabilityService = new UiReadabilityService();
        ResolvePanelReferencesIfNeeded();
        BindAllBackButtonsAtRuntime();
    }

    private void Start()
    {
        if (!openAppSceneFromMainMenu)
        {
            SceneManager.LoadSceneAsync("MainMenuScene");
            return;
        }

        openAppSceneFromMainMenu = false;
        OpenPanel(panelToOpen);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadSceneAsync("MainMenuScene");
    }

    public void GoToAppScene()
    {
        openAppSceneFromMainMenu = true;
        SceneManager.LoadSceneAsync("AppScene");
    }

    public void OpenPanel(PanelType panel)
    {
        ResolvePanelReferencesIfNeeded();
        HideAll();

        GameObject activePanel = GetPanel(panel);
        if (activePanel == null)
        {
            Debug.LogError($"Brak panelu UI dla widoku: {panel}.");
            return;
        }

        activePanel.SetActive(true);
        readabilityService.ApplyButtonTypography(activePanel);

        if (panel == PanelType.Stats)
        {
            RefreshStatsPanel();
            readabilityService.PinStatisticsBackButton(statsPanel, GoToMainMenu);
        }

        BindAllBackButtonsAtRuntime();
    }

    public void ShowRunner()
    {
        panelToOpen = PanelType.MazeRunner;
        OpenPanel(PanelType.MazeRunner);
    }

    public void ShowEditor()
    {
        panelToOpen = PanelType.MapEditor;
        OpenPanel(PanelType.MapEditor);
    }

    public void ShowStats()
    {
        panelToOpen = PanelType.Stats;
        OpenPanel(PanelType.Stats);
    }

    public void ShowSettings()
    {
        panelToOpen = PanelType.Settings;
        OpenPanel(PanelType.Settings);
    }

    private GameObject GetPanel(PanelType panel)
    {
        switch (panel)
        {
            case PanelType.MazeRunner: return mazeRunnerPanel;
            case PanelType.MapEditor: return mapEditorPanel;
            case PanelType.Stats: return statsPanel;
            case PanelType.Settings: return settingsPanel;
            default: return null;
        }
    }

    private void ResolvePanelReferencesIfNeeded()
    {
        if (mazeRunnerPanel == null) mazeRunnerPanel = FindSceneObject("MazeRunnerPanel");
        if (mapEditorPanel == null) mapEditorPanel = FindSceneObject("MapEditorPanel");
        if (statsPanel == null) statsPanel = FindSceneObject("StatsPanel");
        if (settingsPanel == null) settingsPanel = FindSceneObject("SettingsPanel");
    }

    private void HideAll()
    {
        if (mazeRunnerPanel != null) mazeRunnerPanel.SetActive(false);
        if (mapEditorPanel != null) mapEditorPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void RefreshStatsPanel()
    {
        if (statsPanel == null)
        {
            return;
        }

        if (statsPanelController == null)
        {
            statsPanelController = statsPanel.GetComponent<StatsPanelController>();
            if (statsPanelController == null)
            {
                statsPanelController = statsPanel.AddComponent<StatsPanelController>();
            }
        }

        RectTransform resultsPanel = FindChildByName(statsPanel.transform, "ResultsPanel") as RectTransform;
        if (resultsPanel == null)
        {
            Debug.LogError("Nie znaleziono ResultsPanel w panelu historii pomiarów.");
            return;
        }

        statsPanelController.Initialize(resultsPanel);
        statsPanelController.RefreshDisplay();
    }

    private void BindAllBackButtonsAtRuntime()
    {
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || !button.gameObject.scene.IsValid() || button.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            bool isBack = button.name == "BtnBack" ||
                          (text != null && (text.text.Contains("Wróć") || text.text.Contains("Back")));
            if (!isBack)
            {
                continue;
            }

            button.onClick.RemoveListener(GoToMainMenu);
            button.onClick.AddListener(GoToMainMenu);
        }
    }

    private static GameObject FindSceneObject(string objectName)
    {
        RectTransform[] transforms = Resources.FindObjectsOfTypeAll<RectTransform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            RectTransform item = transforms[i];
            if (item != null && item.name == objectName && item.gameObject.scene.IsValid())
            {
                return item.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
