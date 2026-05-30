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
/// Wspólne metody techniczne interfejsu i układu siatek.
/// Oddzielna odpowiedzialność fasady sceny; zachowuje kompatybilność z powiązaniami Unity Inspector.
/// </summary>
public partial class MazeAppController
{
    private static RectTransform FindRectTransformByName(Transform root, string nameToFind)
    {
        Transform child = FindChildByName(root, nameToFind);
        return child as RectTransform;
    }

    private static TMP_Text FindTMPTextByName(Transform root, string nameToFind)
    {
        Transform child = FindChildByName(root, nameToFind);
        if (child == null)
        {
            return null;
        }

        return child.GetComponent<TMP_Text>();
    }

    private static TMP_Text FindTMPTextByTextValue(Transform root, string textValue)
    {
        if (root == null)
        {
            return null;
        }

        TMP_Text text = root.GetComponent<TMP_Text>();
        if (text != null && text.text == textValue)
        {
            return text;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            TMP_Text found = FindTMPTextByTextValue(root.GetChild(i), textValue);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static Transform FindChildByName(Transform root, string nameToFind)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == nameToFind)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = FindChildByName(root.GetChild(i), nameToFind);
            if (child != null)
            {
                return child;
            }
        }

        return null;
    }

    private static RectTransform FindRectTransformInScene(string objectName)
    {
        RectTransform[] allRectTransforms = Resources.FindObjectsOfTypeAll<RectTransform>();

        foreach (RectTransform rectTransform in allRectTransforms)
        {
            if (rectTransform.name != objectName)
            {
                continue;
            }

            if (!rectTransform.gameObject.scene.IsValid())
            {
                continue;
            }

            return rectTransform;
        }

        return null;
    }

    private float CalculateTileSize()
    {
        return CalculateTileSize(editorGrid, mazeWidth, mazeHeight);
    }

    private float CalculateTileSize(RectTransform targetGrid, int width, int height)
    {
        if (targetGrid == null || width <= 0 || height <= 0)
        {
            return FallbackTileSize;
        }

        Rect rect = targetGrid.rect;

        if (rect.width <= 0f || rect.height <= 0f)
        {
            return FallbackTileSize;
        }

        float horizontalTileSize = (rect.width - (width - 1) * TileSpacing) / width;
        float verticalTileSize = (rect.height - (height - 1) * TileSpacing) / height;

        float size = Mathf.Min(horizontalTileSize, verticalTileSize);
        if (size <= 0f)
        {
            return FallbackTileSize;
        }

        return Mathf.Max(MinTileSize, size);
    }

    private void ClearEditorGridVisuals()
    {
        ClearGridVisuals(editorGrid);
    }

    private static void ClearGridVisuals(RectTransform grid)
    {
        if (grid == null)
        {
            return;
        }

        for (int i = grid.childCount - 1; i >= 0; i--)
        {
            Destroy(grid.GetChild(i).gameObject);
        }
    }

}
