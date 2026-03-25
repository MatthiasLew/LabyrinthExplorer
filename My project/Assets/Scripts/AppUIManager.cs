using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Panels")]
    [SerializeField] private GameObject mazeRunnerPanel;
    [SerializeField] private GameObject mapEditorPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        OpenPanel(panelToOpen);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadSceneAsync("MainMenuScene");
    }

    public void GoToAppScene()
    {
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
                if (statsPanel != null) statsPanel.SetActive(true);
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
}