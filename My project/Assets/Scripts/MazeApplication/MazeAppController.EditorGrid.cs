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
/// Interakcje kafelków i renderowanie planszy edytora.
/// Oddzielna odpowiedzialność fasady sceny; zachowuje kompatybilność z powiązaniami Unity Inspector.
/// </summary>
public partial class MazeAppController
{
    private void RebuildEditorGridVisuals()
    {
        TrySetupMapEditorUI();

        if (editorGrid == null)
        {
            return;
        }

        EnsureEditorGridLayout();
        ClearEditorGridVisuals();

        tileImages = new Image[mazeWidth, mazeHeight];

        Canvas.ForceUpdateCanvases();
        float tileSize = CalculateTileSize();

        editorGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        editorGridLayout.constraintCount = mazeWidth;
        editorGridLayout.cellSize = new Vector2(tileSize, tileSize);
        editorGridLayout.spacing = new Vector2(TileSpacing, TileSpacing);
        editorGridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        editorGridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        editorGridLayout.childAlignment = TextAnchor.UpperLeft;

        for (int y = mazeHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < mazeWidth; x++)
            {
                Vector2Int tilePosition = new Vector2Int(x, y);
                CreateTile(tilePosition);
            }
        }

        RefreshAllTiles();
    }

    private void CreateTile(Vector2Int position)
    {
        GameObject tileObject = new GameObject(
            $"Tile_{position.x}_{position.y}",
            typeof(RectTransform),
            typeof(Image));

        tileObject.layer = editorGrid.gameObject.layer;
        tileObject.transform.SetParent(editorGrid, false);

        Image tileImage = tileObject.GetComponent<Image>();
        tileImage.color = walkableColor;

        MazeTileDragHandler dragHandler = tileObject.AddComponent<MazeTileDragHandler>();
        dragHandler.Initialize(position, OnTileDragAction);

        tileImages[position.x, position.y] = tileImage;
    }

    private void OnTileClicked(Vector2Int position)
    {
        if (currentMaze == null || !currentMaze.IsInside(position))
        {
            return;
        }

        switch (activeTool)
        {
            case EditorTool.DrawWalls:
                SetWallAt(position, true);
                break;

            case EditorTool.DeleteWalls:
                SetWallAt(position, false);
                break;

            case EditorTool.SetStartFinish:
                PlaceStartOrFinish(position);
                break;

            default:
                UpdateInfo("Najpierw wybierz narzędzie.");
                break;
        }
    }

    private void OnTileDragAction(Vector2Int position, bool isDragging)
    {
        if (currentMaze == null || !currentMaze.IsInside(position))
        {
            return;
        }

        // Start/meta ma działać pojedynczym naciśnięciem, a nie malowaniem po wielu polach.
        if (activeTool == EditorTool.SetStartFinish)
        {
            if (isDragging && !isCurrentlyDraggingTiles)
            {
                PlaceStartOrFinish(position);
                MazeTileDragHandler.EndGlobalDrag();
            }

            return;
        }

        if (!isDragging)
        {
            FinishTileDragEditing();
            return;
        }

        if (activeTool != EditorTool.DrawWalls && activeTool != EditorTool.DeleteWalls)
        {
            UpdateInfo("Najpierw wybierz narzędzie.");
            return;
        }

        if (!isCurrentlyDraggingTiles)
        {
            isCurrentlyDraggingTiles = true;
            mazeModifiedDuringDrag = false;
            MazeTileDragHandler.StartGlobalDrag(activeTool == EditorTool.DrawWalls);
        }

        bool makeWall = activeTool == EditorTool.DrawWalls;
        mazeModifiedDuringDrag |= ModifyWallWithoutRebuild(position, makeWall);
    }

    private void FinishTileDragEditing()
    {
        if (!isCurrentlyDraggingTiles)
        {
            return;
        }

        isCurrentlyDraggingTiles = false;
        MazeTileDragHandler.EndGlobalDrag();

        if (!mazeModifiedDuringDrag)
        {
            return;
        }

        mazeModifiedDuringDrag = false;
        RebuildRunnerGrids();
        InvalidateDisplayedBenchmark();
    }

    private void SetWallAt(Vector2Int position, bool makeWall)
    {
        if (position == startPosition || position == finishPosition)
        {
            UpdateInfo("Nie można ustawić ściany na starcie lub mecie.");
            return;
        }

        bool shouldBeWalkable = !makeWall;
        if (currentMaze.IsWalkable(position) == shouldBeWalkable)
        {
            return;
        }

        currentMaze.SetWalkable(position, shouldBeWalkable);
        MarkCurrentMazeAsEdited();
        RefreshTile(position);
        RebuildRunnerGrids();
        InvalidateDisplayedBenchmark();
    }

    private bool ModifyWallWithoutRebuild(Vector2Int position, bool makeWall)
    {
        if (position == startPosition || position == finishPosition)
        {
            return false;
        }

        bool shouldBeWalkable = !makeWall;
        if (currentMaze.IsWalkable(position) == shouldBeWalkable)
        {
            return false;
        }

        currentMaze.SetWalkable(position, shouldBeWalkable);
        MarkCurrentMazeAsEdited();
        RefreshTile(position);
        return true;
    }

    private void PlaceStartOrFinish(Vector2Int position)
    {
        if (currentMaze == null)
        {
            return;
        }

        Vector2Int previousMarker;

        if (placeStartNext)
        {
            if (position == finishPosition)
            {
                UpdateInfo("Start nie może być na mecie.");
                return;
            }

            previousMarker = startPosition;
            startPosition = position;
            currentMaze.SetWalkable(position, true);
            MarkCurrentMazeAsEdited();
            placeStartNext = false;

            RefreshTile(previousMarker);
            RefreshTile(startPosition);
            RebuildRunnerGrids();
            InvalidateDisplayedBenchmark();

            UpdateInfo($"Start ustawiony: {startPosition}\nNastępnie ustaw metę.");
            return;
        }

        if (position == startPosition)
        {
            UpdateInfo("Meta nie może być na starcie.");
            return;
        }

        previousMarker = finishPosition;
        finishPosition = position;
        currentMaze.SetWalkable(position, true);
        MarkCurrentMazeAsEdited();
        placeStartNext = true;

        RefreshTile(previousMarker);
        RefreshTile(finishPosition);
        RebuildRunnerGrids();
        InvalidateDisplayedBenchmark();

        UpdateInfo($"Meta ustawiona: {finishPosition}\nNastępnie ustaw start.");
    }

    private void RefreshAllTiles()
    {
        if (tileImages == null)
        {
            return;
        }

        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                RefreshTile(new Vector2Int(x, y));
            }
        }
    }

    private void RefreshTile(Vector2Int position)
    {
        if (tileImages == null || currentMaze == null)
        {
            return;
        }

        if (position.x < 0 || position.x >= mazeWidth || position.y < 0 || position.y >= mazeHeight)
        {
            return;
        }

        Image tileImage = tileImages[position.x, position.y];
        if (tileImage == null)
        {
            return;
        }

        if (position == startPosition)
        {
            tileImage.color = startColor;
            return;
        }

        if (position == finishPosition)
        {
            tileImage.color = finishColor;
            return;
        }

        tileImage.color = currentMaze.IsWalkable(position) ? walkableColor : wallColor;
    }

    private void TrySetupMapEditorUI()
    {
        ResolveEditorReferences();
        EnsureEditorGridLayout();
        ApplyEditorGridBackground();
        EnsureMazeSizeDropdown();
        BindEditorButtons();
    }

    private void ResolveEditorReferences()
    {
        if (mapEditorPanel == null)
        {
            mapEditorPanel = FindRectTransformInScene("MapEditorPanel");
        }

        if (mapEditorPanel == null)
        {
            return;
        }

        if (gridSection == null)
        {
            gridSection = FindRectTransformByName(mapEditorPanel, "GridSection");
        }

        if (gridSection == null)
        {
            gridSection = FindRectTransformByName(mapEditorPanel, "EditorArea");
        }

        if (gridSection == null)
        {
            gridSection = FindRectTransformByName(mapEditorPanel, "EdtiorArea");
        }

        if (buttonsSection == null)
        {
            buttonsSection = FindRectTransformByName(mapEditorPanel, "ButtonsSection");
        }

        if (buttonsSection == null)
        {
            buttonsSection = FindRectTransformByName(mapEditorPanel, "ButtonSection");
        }

        if (editorGrid == null)
        {
            editorGrid = FindRectTransformByName(mapEditorPanel, "EditorGrid");
        }

        if (editorGrid == null && gridSection != null)
        {
            editorGrid = FindRectTransformByName(gridSection, "EditorGrid");
        }

        if (editorGrid == null)
        {
            editorGrid = gridSection;
        }
    }

    private void EnsureEditorGridLayout()
    {
        if (editorGrid == null)
        {
            return;
        }

        editorGridLayout = editorGrid.GetComponent<GridLayoutGroup>();
        if (editorGridLayout == null)
        {
            editorGridLayout = editorGrid.gameObject.AddComponent<GridLayoutGroup>();
        }
    }

    private void ApplyEditorGridBackground()
    {
        if (editorGrid == null)
        {
            return;
        }

        Image backgroundImage = editorGrid.GetComponent<Image>();
        if (backgroundImage == null)
        {
            return;
        }

        backgroundImage.color = editorGridBackgroundColor;
    }

    private void BindEditorButtons()
    {
        if (mapEditorPanel == null)
        {
            return;
        }

        Transform buttonsRoot = buttonsSection != null ? buttonsSection : mapEditorPanel;

        BindButton(buttonsRoot, "BtnDraw", ToggleDrawMode);
        BindButton(buttonsRoot, "BtnDelete", EnableDeleteMode);
        BindButton(buttonsRoot, "BtnDeleteMaze", DeleteMazeFromEditor);
        BindButton(buttonsRoot, "BtnStartEnd", ToggleStartFinishMode);
        BindButton(buttonsRoot, "BtnRandomGen", GenerateRandomMaze);
        BindButton(buttonsRoot, "BtnSave", SaveMaze);

        BindButton(buttonsRoot, "BtnAddMaze", CreateDemoMaze);
        BindButton(mapEditorPanel, "BtnStartMeasurements", RunComparison);
    }

}
