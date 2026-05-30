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
/// Dialog wyboru ustawień oraz aktualizacja elementów sterujących.
/// Oddzielna odpowiedzialność fasady sceny; zachowuje kompatybilność z powiązaniami Unity Inspector.
/// </summary>
public partial class MazeAppController
{
    private void ConfigureAllCanvasScalers()
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
    }

    private void ScheduleUiRefreshAfterResolutionChange()
    {
        if (pendingResolutionRefreshCoroutine != null)
        {
            StopCoroutine(pendingResolutionRefreshCoroutine);
        }

        pendingResolutionRefreshCoroutine = StartCoroutine(RefreshUiAfterResolutionChange());
    }

    private IEnumerator RefreshUiAfterResolutionChange()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        ConfigureAllCanvasScalers();
        Canvas.ForceUpdateCanvases();

        if (currentMaze != null)
        {
            RebuildEditorGridVisuals();
            RebuildRunnerGrids();
        }

        if (settingsPanel != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(settingsPanel);
        }

        if (mapEditorPanel != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(mapEditorPanel);
        }

        if (mazeRunnerPanel != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(mazeRunnerPanel);
        }

        pendingResolutionRefreshCoroutine = null;
    }

    private void ToggleFullscreenMode()
    {
        ApplyResolution(selectedResolutionIndex, !isFullscreen, true);
    }

    private void ShowDisplayModeSelectionDialog()
    {
        string[] labels;
        if (currentLanguage == AppLanguage.Polski)
        {
            labels = new[] { "Tryb okienkowy", "Pełny ekran" };
        }
        else
        {
            labels = new[] { "Windowed", "Fullscreen" };
        }

        ShowSettingsSelectionDialog(
            currentLanguage == AppLanguage.Polski ? "Wybierz tryb wyświetlania" : "Choose display mode",
            labels,
            isFullscreen ? 1 : 0,
            index => ApplyResolution(selectedResolutionIndex, index == 1, true));
    }

    private void ShowResolutionSelectionDialog()
    {
        string[] labels = new string[ResolutionOptions.Length];
        for (int i = 0; i < ResolutionOptions.Length; i++)
        {
            labels[i] = ResolutionOptions[i].Label;
        }

        ShowSettingsSelectionDialog(
            currentLanguage == AppLanguage.Polski ? "Wybierz rozdzielczość" : "Choose resolution",
            labels,
            selectedResolutionIndex,
            index => ApplyResolution(index, isFullscreen, true));
    }

    private void ShowLanguageSelectionDialog()
    {
        string[] labels = { "Polski", "English" };

        ShowSettingsSelectionDialog(
            currentLanguage == AppLanguage.Polski ? "Wybierz język" : "Choose language",
            labels,
            (int)currentLanguage,
            OnLanguageSelected);
    }

    private void OnLanguageSelected(int index)
    {
        currentLanguage = index == 0 ? AppLanguage.Polski : AppLanguage.English;
        PlayerPrefs.SetInt(LanguagePrefKey, (int)currentLanguage);
        PlayerPrefs.Save();

        UpdateSettingsControlsText();
        ApplyLanguageToAllTexts();
    }

    private void EnsureSettingsSelectionDialogBuilt()
    {
        if (settingsSelectionDialogOverlay != null || settingsPanel == null)
        {
            return;
        }

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null)
        {
            return;
        }

        settingsSelectionDialogOverlay = new GameObject("SettingsSelectionDialog", typeof(RectTransform), typeof(Image));
        RectTransform overlayRect = settingsSelectionDialogOverlay.GetComponent<RectTransform>();
        overlayRect.SetParent(settingsPanel, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = settingsSelectionDialogOverlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.6f);
        overlayImage.raycastTarget = true;

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(overlayRect, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(700f, 560f);
        panelRect.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = saveDialogPanelColor;

        settingsSelectionTitleText = CreateDialogLabel(
            panelRect,
            string.Empty,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -26f),
            new Vector2(640f, 46f),
            defaultFont,
            30f,
            TextAlignmentOptions.Center,
            Color.white);

        GameObject listRoot = new GameObject("ListRoot", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        RectTransform listRect = listRoot.GetComponent<RectTransform>();
        listRect.SetParent(panelRect, false);
        listRect.anchorMin = new Vector2(0.5f, 0.5f);
        listRect.anchorMax = new Vector2(0.5f, 0.5f);
        listRect.pivot = new Vector2(0.5f, 0.5f);
        listRect.sizeDelta = new Vector2(620f, 380f);
        listRect.anchoredPosition = new Vector2(0f, -16f);
        listRoot.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 1f);

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.SetParent(listRect, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(8f, 8f);
        viewportRect.offsetMax = new Vector2(-8f, -8f);
        viewportObject.GetComponent<Mask>().showMaskGraphic = false;
        viewportObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.04f);

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
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = listRoot.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;

        CreateDialogButton(
            panelRect,
            currentLanguage == AppLanguage.Polski ? "Zamknij" : "Close",
            new Vector2(0f, -238f),
            HideSettingsSelectionDialog,
            defaultFont);

        settingsSelectionListContent = contentRect;
        settingsSelectionDialogOverlay.SetActive(false);
    }

    private void ShowSettingsSelectionDialog(string title, string[] options, int selectedIndex, Action<int> onSelected)
    {
        EnsureSettingsSelectionDialogBuilt();
        if (settingsSelectionDialogOverlay == null || settingsSelectionListContent == null)
        {
            return;
        }

        onSettingsOptionSelected = onSelected;
        settingsSelectionTitleText.text = title;

        ClearGridVisuals(settingsSelectionListContent);

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null)
        {
            return;
        }

        for (int i = 0; i < options.Length; i++)
        {
            int optionIndex = i;
            string optionLabel = options[i];
            bool isSelected = optionIndex == selectedIndex;

            GameObject buttonObject = new GameObject(
                "Option_" + optionLabel,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(settingsSelectionListContent, false);
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);
            buttonRect.sizeDelta = new Vector2(0f, 54f);

            LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 54f;
            layoutElement.flexibleWidth = 1f;

            Image image = buttonObject.GetComponent<Image>();
            image.color = isSelected
                ? new Color(0.28f, 0.36f, 0.28f, 1f)
                : new Color(0.22f, 0.22f, 0.22f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                HideSettingsSelectionDialog();
                onSettingsOptionSelected?.Invoke(optionIndex);
            });

            TextMeshProUGUI text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.SetParent(buttonRect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 0f);
            textRect.offsetMax = new Vector2(-14f, 0f);
            text.font = defaultFont;
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.Left;
            text.color = Color.white;
            text.text = optionLabel;
            text.raycastTarget = false;
        }

        settingsSelectionDialogOverlay.SetActive(true);
        settingsSelectionDialogOverlay.transform.SetAsLastSibling();
    }

    private void HideSettingsSelectionDialog()
    {
        if (settingsSelectionDialogOverlay != null)
        {
            settingsSelectionDialogOverlay.SetActive(false);
        }
    }

    private void UpdateSettingsControlsText()
    {
        if (resolutionButtonText != null)
        {
            resolutionButtonText.text = ResolutionOptions[selectedResolutionIndex].Label;
        }

        if (languageButtonText != null)
        {
            languageButtonText.text = currentLanguage == AppLanguage.Polski ? "Polski" : "English";
        }

        if (fullscreenButtonText != null)
        {
            fullscreenButtonText.text = currentLanguage == AppLanguage.Polski
                ? (isFullscreen ? "Pełny ekran" : "Tryb okienkowy")
                : (isFullscreen ? "Fullscreen" : "Windowed");
        }

        if (resolutionLabelText != null)
        {
            resolutionLabelText.text = currentLanguage == AppLanguage.Polski ? "Rozdzielczość" : "Resolution";
        }

        if (fullscreenLabelText != null)
        {
            fullscreenLabelText.text = currentLanguage == AppLanguage.Polski ? "Tryb Wyświetlania" : "Display Mode";
        }

        if (languageLabelText != null)
        {
            languageLabelText.text = currentLanguage == AppLanguage.Polski ? "Język" : "Language";
        }
    }

}
