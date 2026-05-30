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
/// Dynamiczny selektor rozmiaru labiryntu.
/// Oddzielna odpowiedzialność fasady sceny; zachowuje kompatybilność z powiązaniami Unity Inspector.
/// </summary>
public partial class MazeAppController
{
    private void EnsureMazeSizeDropdown()
    {
        if (mapEditorPanel == null)
        {
            return;
        }

        Transform buttonsRoot = buttonsSection != null ? buttonsSection : mapEditorPanel;
        if (buttonsRoot == null)
        {
            return;
        }

        if (mazeSizeDropdown != null)
        {
            ConfigureMazeSizeDropdownOptions();
            SyncMazeSizeDropdownSelection();
            return;
        }

        Transform drawButton = FindChildByName(buttonsRoot, "BtnDraw");
        Transform dropdownParent = drawButton != null && drawButton.parent != null
            ? drawButton.parent
            : buttonsRoot;

        Transform existingSelector = FindChildByName(dropdownParent, "MazeSizeSelector");
        if (existingSelector != null)
        {
            mazeSizeSelectorRoot = existingSelector as RectTransform;
            mazeSizeDropdown = existingSelector.GetComponentInChildren<TMP_Dropdown>(true);
            if (drawButton != null && mazeSizeSelectorRoot != null)
            {
                mazeSizeSelectorRoot.SetSiblingIndex(drawButton.GetSiblingIndex());
            }

            if (mazeSizeDropdown != null)
            {
                ConfigureMazeSizeDropdownOptions();
                SyncMazeSizeDropdownSelection();
            }

            return;
        }

        BuildMazeSizeDropdownUI(dropdownParent, drawButton);
        ConfigureMazeSizeDropdownOptions();
        SyncMazeSizeDropdownSelection();
    }

    private void BuildMazeSizeDropdownUI(Transform parent, Transform drawButton)
    {
        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null)
        {
            return;
        }

        GameObject selectorRootObject = new GameObject(
            "MazeSizeSelector",
            typeof(RectTransform),
            typeof(Image),
            typeof(LayoutElement),
            typeof(HorizontalLayoutGroup));
        RectTransform selectorRootRect = selectorRootObject.GetComponent<RectTransform>();
        selectorRootRect.SetParent(parent, false);
        selectorRootRect.anchorMin = new Vector2(0f, 0f);
        selectorRootRect.anchorMax = new Vector2(0f, 0f);
        selectorRootRect.pivot = new Vector2(0.5f, 0.5f);
        selectorRootRect.sizeDelta = new Vector2(500f, 120f);

        if (drawButton != null)
        {
            selectorRootRect.SetSiblingIndex(drawButton.GetSiblingIndex());
        }

        Image selectorRootImage = selectorRootObject.GetComponent<Image>();
        selectorRootImage.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        LayoutElement selectorLayout = selectorRootObject.GetComponent<LayoutElement>();
        selectorLayout.preferredHeight = 120f;
        selectorLayout.preferredWidth = 500f;
        selectorLayout.flexibleWidth = 1f;

        HorizontalLayoutGroup horizontalLayout = selectorRootObject.GetComponent<HorizontalLayoutGroup>();
        horizontalLayout.padding = new RectOffset(16, 16, 14, 14);
        horizontalLayout.spacing = 12f;
        horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
        horizontalLayout.childControlWidth = true;
        horizontalLayout.childControlHeight = true;
        horizontalLayout.childForceExpandWidth = false;
        horizontalLayout.childForceExpandHeight = false;

        TextMeshProUGUI label = new GameObject("Label", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.SetParent(selectorRootRect, false);
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.sizeDelta = new Vector2(230f, 0f);

        LayoutElement labelLayout = label.GetComponent<LayoutElement>();
        labelLayout.preferredWidth = 230f;
        labelLayout.flexibleWidth = 0f;

        label.font = defaultFont;
        label.text = "Wybierz rozmiar";
        label.fontSize = 28f;
        label.alignment = TextAlignmentOptions.Left;
        label.color = Color.white;
        label.raycastTarget = false;

        GameObject dropdownObject = new GameObject(
            "SizeDropdown",
            typeof(RectTransform),
            typeof(Image),
            typeof(TMP_Dropdown),
            typeof(LayoutElement));
        RectTransform dropdownRect = dropdownObject.GetComponent<RectTransform>();
        dropdownRect.SetParent(selectorRootRect, false);
        dropdownRect.anchorMin = new Vector2(0f, 0.5f);
        dropdownRect.anchorMax = new Vector2(0f, 0.5f);
        dropdownRect.pivot = new Vector2(0f, 0.5f);
        dropdownRect.sizeDelta = new Vector2(230f, 92f);

        LayoutElement dropdownLayout = dropdownObject.GetComponent<LayoutElement>();
        dropdownLayout.preferredWidth = 230f;
        dropdownLayout.preferredHeight = 92f;
        dropdownLayout.flexibleWidth = 0f;

        Image dropdownImage = dropdownObject.GetComponent<Image>();
        dropdownImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        TMP_Dropdown dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
        dropdown.targetGraphic = dropdownImage;

        TextMeshProUGUI captionText = CreateDropdownText(
            dropdownRect,
            "CaptionText",
            defaultFont,
            TextAlignmentOptions.Left,
            26f,
            Color.white);
        captionText.rectTransform.offsetMin = new Vector2(14f, 0f);
        captionText.rectTransform.offsetMax = new Vector2(-36f, 0f);
        captionText.raycastTarget = false;

        TextMeshProUGUI arrowText = CreateDropdownText(
            dropdownRect,
            "Arrow",
            defaultFont,
            TextAlignmentOptions.Center,
            30f,
            Color.white);
        arrowText.text = "v";
        arrowText.rectTransform.anchorMin = new Vector2(1f, 0f);
        arrowText.rectTransform.anchorMax = new Vector2(1f, 1f);
        arrowText.rectTransform.pivot = new Vector2(1f, 0.5f);
        arrowText.rectTransform.offsetMin = new Vector2(-32f, 0f);
        arrowText.rectTransform.offsetMax = new Vector2(-6f, 0f);
        arrowText.raycastTarget = false;

        GameObject templateObject = new GameObject(
            "Template",
            typeof(RectTransform),
            typeof(Image),
            typeof(ScrollRect));
        RectTransform templateRect = templateObject.GetComponent<RectTransform>();
        templateRect.SetParent(dropdownRect, false);
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = new Vector2(0f, 2f);
        templateRect.sizeDelta = new Vector2(0f, 260f);

        Image templateImage = templateObject.GetComponent<Image>();
        templateImage.color = new Color(0.14f, 0.14f, 0.14f, 1f);

        GameObject viewportObject = new GameObject(
            "Viewport",
            typeof(RectTransform),
            typeof(Image),
            typeof(Mask));
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.SetParent(templateRect, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(2f, 2f);
        viewportRect.offsetMax = new Vector2(-2f, -2f);

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.05f);
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
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 2f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentFitter = contentObject.GetComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject itemObject = new GameObject(
            "Item",
            typeof(RectTransform),
            typeof(Image),
            typeof(Toggle),
            typeof(LayoutElement));
        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        itemRect.SetParent(contentRect, false);
        itemRect.anchorMin = new Vector2(0f, 1f);
        itemRect.anchorMax = new Vector2(1f, 1f);
        itemRect.pivot = new Vector2(0.5f, 1f);
        itemRect.sizeDelta = new Vector2(0f, 46f);

        LayoutElement itemLayout = itemObject.GetComponent<LayoutElement>();
        itemLayout.preferredHeight = 46f;

        Image itemImage = itemObject.GetComponent<Image>();
        itemImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        Toggle itemToggle = itemObject.GetComponent<Toggle>();
        itemToggle.targetGraphic = itemImage;

        TextMeshProUGUI checkmark = CreateDropdownText(
            itemRect,
            "Checkmark",
            defaultFont,
            TextAlignmentOptions.Center,
            22f,
            new Color(0.15f, 0.78f, 0.45f, 1f));
        checkmark.text = "x";
        checkmark.rectTransform.anchorMin = new Vector2(0f, 0f);
        checkmark.rectTransform.anchorMax = new Vector2(0f, 1f);
        checkmark.rectTransform.pivot = new Vector2(0f, 0.5f);
        checkmark.rectTransform.offsetMin = new Vector2(10f, 0f);
        checkmark.rectTransform.offsetMax = new Vector2(34f, 0f);
        checkmark.raycastTarget = false;
        itemToggle.graphic = checkmark;

        TextMeshProUGUI itemLabel = CreateDropdownText(
            itemRect,
            "ItemLabel",
            defaultFont,
            TextAlignmentOptions.Left,
            24f,
            Color.white);
        itemLabel.rectTransform.offsetMin = new Vector2(44f, 0f);
        itemLabel.rectTransform.offsetMax = new Vector2(-10f, 0f);
        itemLabel.raycastTarget = false;

        ScrollRect scrollRect = templateObject.GetComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 14f;

        dropdown.template = templateRect;
        dropdown.captionText = captionText;
        dropdown.itemText = itemLabel;
        dropdown.alphaFadeSpeed = 0.1f;
        templateObject.SetActive(false);

        mazeSizeSelectorRoot = selectorRootRect;
        mazeSizeDropdown = dropdown;
    }

    private static TextMeshProUGUI CreateDropdownText(
        RectTransform parent,
        string objectName,
        TMP_FontAsset font,
        TextAlignmentOptions alignment,
        float fontSize,
        Color color)
    {
        TextMeshProUGUI text = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        text.font = font;
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.color = color;
        text.text = string.Empty;
        text.enableWordWrapping = false;

        return text;
    }

    private void ConfigureMazeSizeDropdownOptions()
    {
        if (mazeSizeDropdown == null)
        {
            return;
        }

        var options = new List<TMP_Dropdown.OptionData>(SupportedMazeSizes.Length);
        for (int i = 0; i < SupportedMazeSizes.Length; i++)
        {
            int size = SupportedMazeSizes[i];
            options.Add(new TMP_Dropdown.OptionData($"{size}x{size}"));
        }

        mazeSizeDropdown.ClearOptions();
        mazeSizeDropdown.AddOptions(options);
        mazeSizeDropdown.onValueChanged.RemoveListener(OnMazeSizeDropdownChanged);
        mazeSizeDropdown.onValueChanged.AddListener(OnMazeSizeDropdownChanged);
    }

    private void SyncMazeSizeDropdownSelection()
    {
        if (mazeSizeDropdown == null)
        {
            return;
        }

        int targetSize = Mathf.Clamp(Mathf.Min(mazeWidth, mazeHeight), minMazeSize, maxMazeSize);
        int selectedIndex = FindNearestMazeSizeIndex(targetSize);

        isSyncingMazeSizeDropdown = true;
        mazeSizeDropdown.SetValueWithoutNotify(selectedIndex);
        isSyncingMazeSizeDropdown = false;
    }

    private static int FindNearestMazeSizeIndex(int size)
    {
        int bestIndex = 0;
        int smallestDistance = Mathf.Abs(SupportedMazeSizes[0] - size);

        for (int i = 1; i < SupportedMazeSizes.Length; i++)
        {
            int distance = Mathf.Abs(SupportedMazeSizes[i] - size);
            if (distance < smallestDistance)
            {
                bestIndex = i;
                smallestDistance = distance;
            }
        }

        return bestIndex;
    }

    private void OnMazeSizeDropdownChanged(int index)
    {
        if (isSyncingMazeSizeDropdown)
        {
            return;
        }

        if (index < 0 || index >= SupportedMazeSizes.Length)
        {
            return;
        }

        int size = SupportedMazeSizes[index];
        CreateMazeFromSize(size, size);
    }

}
