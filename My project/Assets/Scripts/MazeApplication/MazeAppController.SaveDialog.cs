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
/// Dialog zapisu i serializacja mapy.
/// Oddzielna odpowiedzialność fasady sceny; zachowuje kompatybilność z powiązaniami Unity Inspector.
/// </summary>
public partial class MazeAppController
{
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
            SetCurrentMazeName(mazeName);

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

}
