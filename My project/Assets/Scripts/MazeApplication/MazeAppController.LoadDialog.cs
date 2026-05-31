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
/// Dialog wyboru i wczytywanie zapisanych map.
/// Oddzielna odpowiedzialność fasady sceny; zachowuje kompatybilność z powiązaniami Unity Inspector.
/// </summary>
public partial class MazeAppController
{
    private TMP_Text loadMazeDialogTitleText;
    private TMP_Text loadMazeDialogCloseButtonText;
    private int loadMazeDialogFileCount;

    private void ShowLoadMazeDialog()
    {
        TrySetupRunnerUI();

        if (mazeRunnerPanel == null)
        {
            UpdateInfo(TextByLanguage("Nie znaleziono panelu labiryntu.", "Maze panel not found."));
            return;
        }

        string[] files = GetSavedMazeFiles();
        if (files.Length == 0)
        {
            UpdateInfo(TextByLanguage("Brak zapisanych labiryntów.", "No saved mazes found."));
            return;
        }

        EnsureLoadMazeDialogBuilt();
        UpdateLoadMazeDialogLanguage();
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
            UpdateInfo(TextByLanguage("Nie można utworzyć okna wczytywania.", "Unable to create load dialog."));
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

        loadMazeDialogTitleText = CreateDialogLabel(
            panelRect,
            TextByLanguage("Wczytaj labirynt", "Load Maze"),
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
            TextByLanguage("Wybierz zapisany labirynt:", "Choose a saved maze:"),
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

        Button closeButton = CreateDialogButton(
            panelRect,
            TextByLanguage("Zamknij", "Close"),
            new Vector2(0f, -284f),
            HideLoadMazeDialog,
            defaultFont);
        loadMazeDialogCloseButtonText = closeButton.GetComponentInChildren<TMP_Text>(true);

        loadMazeListContent = contentRect;
        loadMazeDialogOverlay.SetActive(false);

        if (loadMazeDialogTitleText != null)
        {
            loadMazeDialogTitleText.raycastTarget = false;
        }
    }

    private void UpdateLoadMazeDialogLanguage()
    {
        if (loadMazeDialogTitleText != null)
        {
            loadMazeDialogTitleText.text = TextByLanguage("Wczytaj labirynt", "Load Maze");
        }

        if (loadMazeDialogCloseButtonText != null)
        {
            loadMazeDialogCloseButtonText.text = TextByLanguage("Zamknij", "Close");
        }

        if (loadMazeDialogInfoText != null && loadMazeDialogFileCount > 0)
        {
            loadMazeDialogInfoText.text = TextByLanguage(
                $"Wybierz zapisany labirynt ({loadMazeDialogFileCount}):",
                $"Choose a saved maze ({loadMazeDialogFileCount}):");
            loadMazeDialogInfoText.color = Color.white;
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

        loadMazeDialogFileCount = files.Length;
        if (loadMazeDialogInfoText != null)
        {
            loadMazeDialogInfoText.text = TextByLanguage(
                $"Wybierz zapisany labirynt ({files.Length}):",
                $"Choose a saved maze ({files.Length}):");
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
                loadMazeDialogInfoText.text = TextByLanguage("Wybrany plik nie istnieje.", "Selected file does not exist.");
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
                throw new InvalidDataException(TextByLanguage("Nieprawidłowe dane labiryntu.", "Invalid maze data."));
            }

            if (data.width < minMazeSize || data.width > maxMazeSize ||
                data.height < minMazeSize || data.height > maxMazeSize)
            {
                throw new InvalidDataException(
                    TextByLanguage(
                        $"Rozmiar labiryntu {data.width}x{data.height} jest poza obsługiwanym zakresem {minMazeSize}-{maxMazeSize}.",
                        $"Maze size {data.width}x{data.height} is outside supported range {minMazeSize}-{maxMazeSize}."));
            }

            int requiredCellCount = data.width * data.height;
            if (data.walkableCells.Length != requiredCellCount)
            {
                throw new InvalidDataException(TextByLanguage("Liczba pól labiryntu jest niezgodna z rozmiarem.", "Maze cell count mismatch."));
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
            SyncMazeSizeDropdownSelection();
            SetCurrentMazeName(Path.GetFileNameWithoutExtension(filePath));
            currentMazeSource = string.IsNullOrWhiteSpace(data.mazeSource)
                ? "LoadedOrLegacy"
                : data.mazeSource;
            currentMazeSeed = data.mazeSeed;

            HideLoadMazeDialog();
            InvalidateDisplayedBenchmark();
            UpdateInfo(currentLanguage == AppLanguage.Polski
                ? $"Wczytano labirynt: {GetActiveMazeDisplayName()}"
                : $"Loaded maze: {GetActiveMazeDisplayName()}");
        }
        catch (Exception ex)
        {
            if (loadMazeDialogInfoText != null)
            {
                loadMazeDialogInfoText.text = TextByLanguage($"Błąd wczytywania: {ex.Message}", $"Failed to load: {ex.Message}");
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

}
