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
/// Inicjalizacja panelu ustawień i zapis preferencji wyświetlania.
/// Oddzielna odpowiedzialność fasady sceny; zachowuje kompatybilność z powiązaniami Unity Inspector.
/// </summary>
public partial class MazeAppController
{
    private void TrySetupSettingsUI()
    {
        ResolveSettingsReferences();
        if (settingsPanel == null)
        {
            return;
        }

        EnsureSettingsRows();
        BindSettingsButtons();
        EnsureSettingsSelectionDialogBuilt();
        UpdateSettingsControlsText();
    }

    private void ResolveSettingsReferences()
    {
        if (settingsPanel == null)
        {
            settingsPanel = FindSettingsPanelWithControls();
        }

        if (settingsPanel == null)
        {
            return;
        }

        if (resolutionButton == null)
        {
            resolutionButton = FindButtonInSettings("BtnResolution", "Rozdzielczość", "Resolution");
        }

        if (fullscreenButton == null)
        {
            fullscreenButton = FindButtonInSettings("BtnDisplay", "Tryb", "Display");
        }

        if (fullscreenButton == null)
        {
            fullscreenButton = FindButtonInSettings("BtnDisplayMode", "Tryb", "Display");
        }

        if (fullscreenButton == null)
        {
            fullscreenButton = FindButtonInSettings("BtnFullscreen", "Tryb", "Fullscreen");
            if (fullscreenButton == null)
            {
                fullscreenButton = FindButtonInSettings("BtnDeleteMaze", "Tryb", "Fullscreen");
            }
        }

        if (languageButton == null)
        {
            languageButton = FindButtonInSettings("BtnLanguage", "Język", "Language");
        }

        if (resolutionButton == null)
        {
            resolutionButton = FindButtonInSettings("BtnStartMeasurements", "Rozdzielczość", "Resolution");
        }

        if (languageButton == null)
        {
            languageButton = FindButtonInSettings("BtnAddMaze", "Język", "Language");
        }

        if (resolutionButton != null)
        {
            resolutionButtonText = resolutionButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (languageButton != null)
        {
            languageButtonText = languageButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (fullscreenButton != null)
        {
            fullscreenButtonText = fullscreenButton.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private RectTransform FindSettingsPanelWithControls()
    {
        RectTransform[] candidates = Resources.FindObjectsOfTypeAll<RectTransform>();
        foreach (RectTransform candidate in candidates)
        {
            if (candidate == null || candidate.name != "SettingsPanel")
            {
                continue;
            }

            if (!candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            if (FindChildByName(candidate, "BtnStartMeasurements") != null ||
                FindChildByName(candidate, "BtnResolution") != null)
            {
                return candidate;
            }
        }

        return FindRectTransformInScene("SettingsPanel");
    }

    private Button FindButtonInSettings(string buttonName, string fallbackLabelPl, string fallbackLabelEn)
    {
        if (settingsPanel == null)
        {
            return null;
        }

        Transform byName = FindChildByName(settingsPanel, buttonName);
        if (byName != null)
        {
            Button namedButton = byName.GetComponent<Button>();
            if (namedButton != null)
            {
                return namedButton;
            }
        }

        Button[] allButtons = settingsPanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < allButtons.Length; i++)
        {
            Button button = allButtons[i];
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text == null || string.IsNullOrWhiteSpace(text.text))
            {
                continue;
            }

            if (text.text.IndexOf(fallbackLabelPl, StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.text.IndexOf(fallbackLabelEn, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return button;
            }
        }

        return null;
    }

    private void EnsureSettingsRows()
    {
        if (settingsPanel == null)
        {
            return;
        }

        if (resolutionButton != null)
        {
            resolutionLabelText = EnsureLabeledRowForButton(resolutionButton, "ResolutionRow", resolutionLabelText);
        }

        if (fullscreenButton != null)
        {
            fullscreenLabelText = EnsureLabeledRowForButton(fullscreenButton, "DisplayModeRow", fullscreenLabelText);
        }

        if (languageButton != null)
        {
            languageLabelText = EnsureLabeledRowForButton(languageButton, "LanguageRow", languageLabelText);
        }

        RectTransform resolutionRow = resolutionButton != null ? resolutionButton.transform.parent as RectTransform : null;
        RectTransform fullscreenRow = fullscreenButton != null ? fullscreenButton.transform.parent as RectTransform : null;
        RectTransform languageRow = languageButton != null ? languageButton.transform.parent as RectTransform : null;

        if (resolutionRow != null && fullscreenRow != null)
        {
            fullscreenRow.SetSiblingIndex(resolutionRow.GetSiblingIndex() + 1);
        }

        if (fullscreenRow != null && languageRow != null)
        {
            languageRow.SetSiblingIndex(fullscreenRow.GetSiblingIndex() + 1);
        }
    }

    private TMP_Text EnsureLabeledRowForButton(Button button, string rowName, TMP_Text existingLabel)
    {
        if (button == null)
        {
            return existingLabel;
        }

        RectTransform buttonRect = button.transform as RectTransform;
        if (buttonRect == null)
        {
            return existingLabel;
        }

        RectTransform row = buttonRect.parent as RectTransform;
        if (row == null || row.name != rowName)
        {
            int originalSibling = buttonRect.GetSiblingIndex();
            RectTransform originalParent = buttonRect.parent as RectTransform;

            GameObject rowObject = new GameObject(
                rowName,
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(HorizontalLayoutGroup));
            row = rowObject.GetComponent<RectTransform>();
            row.SetParent(originalParent, false);
            row.SetSiblingIndex(originalSibling);
            row.sizeDelta = new Vector2(1100f, 100f);

            LayoutElement rowLayout = rowObject.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = 100f;
            rowLayout.preferredWidth = 1100f;
            rowLayout.flexibleWidth = 1f;

            HorizontalLayoutGroup hLayout = rowObject.GetComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(24, 24, 0, 0);
            hLayout.spacing = 24f;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            buttonRect.SetParent(row, false);

            LayoutElement buttonLayout = button.gameObject.GetComponent<LayoutElement>();
            if (buttonLayout == null)
            {
                buttonLayout = button.gameObject.AddComponent<LayoutElement>();
            }

            buttonLayout.preferredWidth = 520f;
            buttonLayout.preferredHeight = 100f;
            buttonLayout.flexibleWidth = 0f;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(row, false);
            labelRect.SetSiblingIndex(0);
            labelRect.sizeDelta = new Vector2(340f, 100f);

            LayoutElement labelLayout = labelObject.GetComponent<LayoutElement>();
            labelLayout.preferredWidth = 340f;
            labelLayout.preferredHeight = 100f;
            labelLayout.flexibleWidth = 0f;

            TextMeshProUGUI labelText = labelObject.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null)
            {
                labelText.font = defaultFont;
            }

            labelText.fontSize = 30f;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            labelText.raycastTarget = false;

            return labelText;
        }

        if (existingLabel != null)
        {
            return existingLabel;
        }

        return row.GetComponentInChildren<TMP_Text>(true);
    }

    private void BindSettingsButtons()
    {
        BindButtonAction(resolutionButton, ShowResolutionSelectionDialog);
        BindButtonAction(languageButton, ShowLanguageSelectionDialog);
        BindButtonAction(fullscreenButton, ShowDisplayModeSelectionDialog);
    }

    private static void BindButtonAction(Button button, UnityAction action)
    {
        if (button == null || action == null)
        {
            return;
        }

        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
    }

    private void InitializeSettingsState()
    {
        if (settingsInitialized)
        {
            return;
        }

        if (PlayerPrefs.HasKey(LanguagePrefKey))
        {
            int languageValue = Mathf.Clamp(PlayerPrefs.GetInt(LanguagePrefKey, 0), 0, 1);
            currentLanguage = (AppLanguage)languageValue;
        }

        selectedResolutionIndex = Mathf.Clamp(
            PlayerPrefs.GetInt(ResolutionPrefKey, FindNearestResolutionIndex(Screen.width, Screen.height)),
            0,
            ResolutionOptions.Length - 1);

        isFullscreen = PlayerPrefs.GetInt(FullscreenPrefKey, Screen.fullScreen ? 1 : 0) == 1;

        ApplyResolution(selectedResolutionIndex, isFullscreen, false);
        settingsInitialized = true;
    }

    private static int FindNearestResolutionIndex(int width, int height)
    {
        int bestIndex = 0;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < ResolutionOptions.Length; i++)
        {
            int distance = Mathf.Abs(ResolutionOptions[i].width - width) + Mathf.Abs(ResolutionOptions[i].height - height);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void ApplyResolution(int index, bool fullscreen, bool persist)
    {
        selectedResolutionIndex = Mathf.Clamp(index, 0, ResolutionOptions.Length - 1);
        isFullscreen = fullscreen;

        ResolutionOption option = ResolutionOptions[selectedResolutionIndex];
        Screen.SetResolution(option.width, option.height, isFullscreen);
        ConfigureAllCanvasScalers();
        ScheduleUiRefreshAfterResolutionChange();

        if (persist)
        {
            PlayerPrefs.SetInt(ResolutionPrefKey, selectedResolutionIndex);
            PlayerPrefs.SetInt(FullscreenPrefKey, isFullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        UpdateSettingsControlsText();
    }

}
