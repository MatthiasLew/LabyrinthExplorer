using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void OpenMeasurements()
    {
        AppUIManager.panelToOpen = AppUIManager.PanelType.MazeRunner;
        SceneManager.LoadSceneAsync("AppScene");
    }

    public void OpenEditor()
    {
        AppUIManager.panelToOpen = AppUIManager.PanelType.MapEditor;
        SceneManager.LoadSceneAsync("AppScene");
    }

    public void OpenResults()
    {
        AppUIManager.panelToOpen = AppUIManager.PanelType.Stats;
        SceneManager.LoadSceneAsync("AppScene");
    }

    public void OpenSettings()
    {
        AppUIManager.panelToOpen = AppUIManager.PanelType.Settings;
        SceneManager.LoadSceneAsync("AppScene");
    }

    public void QuitApp()
    {
        Application.Quit();
        Debug.Log("Wyjście z aplikacji.");
    }
}