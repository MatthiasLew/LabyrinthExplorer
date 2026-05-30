using Statistics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AppUIManager : MonoBehaviour
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

    private void Awake()
    {
        BindBackButtonsAtRuntime();
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

    /// <summary>
    /// Awaryjnie podpina przyciski powrotu w runtime. Dzięki temu ekran pomiarów
    /// nie zostaje bez wyjścia nawet wtedy, gdy referencja OnClick w scenie zniknie.
    /// </summary>
    private void BindBackButtonsAtRuntime()
    {
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null ||
                !button.gameObject.scene.IsValid() ||
                button.gameObject.scene != gameObject.scene ||
                button.name != "BtnBack")
            {
                continue;
            }

            button.onClick.RemoveListener(GoToMainMenu);
            button.onClick.AddListener(GoToMainMenu);
        }
    }

    public void GoToAppScene()
    {
        openAppSceneFromMainMenu = true;
        SceneManager.LoadSceneAsync("AppScene");
    }

    private void HideAll()
    {
        if (mazeRunnerPanel != null) mazeRunnerPanel.SetActive(false);
        if (mapEditorPanel != null) mapEditorPanel.SetActive(false);
        if (statsPanel != null) statsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void OpenPanel(PanelType panel)
    {
        HideAll();

        switch (panel)
        {
            case PanelType.MazeRunner:
                if (mazeRunnerPanel != null) mazeRunnerPanel.SetActive(true);
                break;

            case PanelType.MapEditor:
                if (mapEditorPanel != null) mapEditorPanel.SetActive(true);
                break;

            case PanelType.Stats:
                if (statsPanel != null)
                {
                    statsPanel.SetActive(true);
                    RefreshStatsPanel();
                }
                break;

            case PanelType.Settings:
                if (settingsPanel != null) settingsPanel.SetActive(true);
                break;
        }
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
            Debug.LogError("Nie znaleziono obiektu ResultsPanel w panelu Wyniki Pomiarów.");
            return;
        }

        statsPanelController.Initialize(resultsPanel);
        statsPanelController.RefreshDisplay();
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
