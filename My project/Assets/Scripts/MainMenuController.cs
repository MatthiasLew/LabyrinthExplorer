using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    private const float UiReferenceWidth = 2560f;
    private const float UiReferenceHeight = 1440f;
    private const float UiMatchWidthHeight = 0.5f;

    private struct ResolutionOption
    {
        public int width;
        public int height;

        public ResolutionOption(int width, int height)
        {
            this.width = width;
            this.height = height;
        }
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

    private void Start()
    {
        ApplySavedDisplaySettings();
        ConfigureAllCanvasScalers();
        ApplySavedLanguage();
    }

    private static void ApplySavedDisplaySettings()
    {
        int index = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionPrefKey, 2), 0, ResolutionOptions.Length - 1);
        bool fullscreen = PlayerPrefs.GetInt(FullscreenPrefKey, Screen.fullScreen ? 1 : 0) == 1;

        ResolutionOption option = ResolutionOptions[index];
        Screen.SetResolution(option.width, option.height, fullscreen);
    }

    private static void ConfigureAllCanvasScalers()
    {
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.gameObject.scene.IsValid() || canvas.renderMode == RenderMode.WorldSpace)
            {
                continue;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(UiReferenceWidth, UiReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = UiMatchWidthHeight;
        }

        Canvas.ForceUpdateCanvases();
    }

    private static void ApplySavedLanguage()
    {
        bool english = PlayerPrefs.GetInt(LanguagePrefKey, 0) == 1;
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || !text.gameObject.scene.IsValid())
            {
                continue;
            }

            text.text = TranslateMenuText(text.text, english);
        }
    }

    private static string TranslateMenuText(string input, bool english)
    {
        if (english)
        {
            switch (input)
            {
                case "Edytor Labiryntów": return "Maze Editor";
                case "Wyniki Pomiarów": return "Measurement Results";
                case "Wyjście": return "Exit";
                case "Ustawienia": return "Settings";
                case "Rozpocznij Pomiary": return "Start Measurements";
            }
        }
        else
        {
            switch (input)
            {
                case "Maze Editor": return "Edytor Labiryntów";
                case "Measurement Results": return "Wyniki Pomiarów";
                case "Exit": return "Wyjście";
                case "Settings": return "Ustawienia";
                case "Start Measurements": return "Rozpocznij Pomiary";
            }
        }

        return input;
    }

    public void OpenMeasurements()
    {
        AppUIManager.panelToOpen = AppUIManager.PanelType.MazeRunner;
        AppUIManager.openAppSceneFromMainMenu = true;
        SceneManager.LoadSceneAsync("AppScene");
    }

    public void OpenEditor()
    {
        AppUIManager.panelToOpen = AppUIManager.PanelType.MapEditor;
        AppUIManager.openAppSceneFromMainMenu = true;
        SceneManager.LoadSceneAsync("AppScene");
    }

    public void OpenResults()
    {
        AppUIManager.panelToOpen = AppUIManager.PanelType.Stats;
        AppUIManager.openAppSceneFromMainMenu = true;
        SceneManager.LoadSceneAsync("AppScene");
    }

    public void OpenSettings()
    {
        AppUIManager.panelToOpen = AppUIManager.PanelType.Settings;
        AppUIManager.openAppSceneFromMainMenu = true;
        SceneManager.LoadSceneAsync("AppScene");
    }

    public void QuitApp()
    {
        Application.Quit();
        Debug.Log(PlayerPrefs.GetInt(LanguagePrefKey, 0) == 1 ? "Application closed." : "Wyjście z aplikacji.");
    }
}
