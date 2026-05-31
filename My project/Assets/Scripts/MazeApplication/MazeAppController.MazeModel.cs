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
/// Model bieżącej mapy, generowanie labiryntu oraz publiczne akcje edytora.
/// Oddzielna odpowiedzialność fasady sceny; zachowuje kompatybilność z powiązaniami Unity Inspector.
/// </summary>
public partial class MazeAppController
{
    public void CreateDemoMaze()
    {
        CreateEditableMaze(mazeWidth, mazeHeight);
    }

    public void CreateMazeFromSize(int width, int height)
    {
        mazeWidth = Mathf.Clamp(width, minMazeSize, maxMazeSize);
        mazeHeight = Mathf.Clamp(height, minMazeSize, maxMazeSize);
        CreateEditableMaze(mazeWidth, mazeHeight);
    }

    public void SetMazeWidth(int width)
    {
        mazeWidth = Mathf.Clamp(width, minMazeSize, maxMazeSize);
    }

    public void SetMazeHeight(int height)
    {
        mazeHeight = Mathf.Clamp(height, minMazeSize, maxMazeSize);
    }

    public void SetMazeWidthFromString(string widthValue)
    {
        if (int.TryParse(widthValue, out int parsed))
        {
            SetMazeWidth(parsed);
        }
    }

    public void SetMazeHeightFromString(string heightValue)
    {
        if (int.TryParse(heightValue, out int parsed))
        {
            SetMazeHeight(parsed);
        }
    }

    public void ClearMaze()
    {
        currentMaze = null;
        tileImages = null;
        activeTool = EditorTool.None;
        SetCurrentMazeName(string.Empty);
        currentMazeSource = "ManualOrEdited";
        currentMazeSeed = 0;

        if (runningComparisonCoroutine != null)
        {
            StopCoroutine(runningComparisonCoroutine);
            runningComparisonCoroutine = null;
        }

        if (pathReplayCoroutine != null)
        {
            StopCoroutine(pathReplayCoroutine);
            pathReplayCoroutine = null;
        }

        ClearEditorGridVisuals();
        ClearGridVisuals(algorithmAGrid);
        ClearGridVisuals(algorithmBGrid);

        InvalidateDisplayedBenchmark();
        UpdateInfo("Labirynt usunięty.");
    }

    public void DeleteMazeFromEditor()
    {
        if (!EnsureMazeExists())
        {
            return;
        }

        for (int x = 0; x < currentMaze.Width; x++)
        {
            for (int y = 0; y < currentMaze.Height; y++)
            {
                currentMaze.SetWalkable(new Vector2Int(x, y), true);
            }
        }

        startPosition = new Vector2Int(0, 0);
        finishPosition = new Vector2Int(currentMaze.Width - 1, currentMaze.Height - 1);
        currentMaze.SetWalkable(startPosition, true);
        currentMaze.SetWalkable(finishPosition, true);

        placeStartNext = true;
        SetCurrentMazeName(string.Empty);
        MarkCurrentMazeAsEdited();
        RefreshAllTiles();
        RebuildRunnerGrids();
        InvalidateDisplayedBenchmark();
        UpdateInfo("Wyczyszczono labirynt.");
    }

    public void SaveMaze()
    {
        if (!EnsureMazeExists())
        {
            UpdateInfo("Brak labiryntu do zapisu.");
            return;
        }

        ShowSaveDialog();
    }

    public void ToggleDrawMode()
    {
        SetTool(EditorTool.DrawWalls, "Tryb rysowania ścian.");
    }

    public void EnableDeleteMode()
    {
        SetTool(EditorTool.DeleteWalls, "Tryb usuwania ścian.");
    }

    public void ToggleStartFinishMode()
    {
        string next = placeStartNext ? "START" : "FINISH";
        SetTool(EditorTool.SetStartFinish, $"Tryb start/meta. Następny punkt: {next}.");
    }

    public void GenerateRandomMaze()
    {
        if (!EnsureMazeExists())
        {
            return;
        }

        int seedToUse = randomSeed == 0 ? Environment.TickCount : randomSeed;
        var generator = new MazeGenerator();

        try
        {
            // Generate maze using DFS algorithm
            bool[,] mazeLayout = generator.GenerateMaze(mazeWidth, mazeHeight, seedToUse);

            // Apply the generated layout to the current maze
            for (int x = 0; x < mazeWidth; x++)
            {
                for (int y = 0; y < mazeHeight; y++)
                {
                    currentMaze.SetWalkable(new Vector2Int(x, y), mazeLayout[x, y]);
                }
            }

            // Extract start and finish positions from the generated maze
            // The generator returns them at distant corners
            Vector2Int[] corners = FindDistantCornersInMaze(mazeLayout);
            startPosition = corners[0];
            finishPosition = corners[1];
            currentMazeSource = "GeneratedDFS";
            currentMazeSeed = seedToUse;

            RefreshAllTiles();
            RebuildRunnerGrids();
            InvalidateDisplayedBenchmark();

            UpdateInfo($"Wygenerowano labirynt (algorytm DFS).\nSeed: {seedToUse}\nStart: {startPosition}, Meta: {finishPosition}");
        }
        catch (Exception ex)
        {
            UpdateInfo($"Błąd przy generowaniu labiryntu: {ex.Message}");
            Debug.LogError($"Maze generation failed: {ex}");
        }
    }

    private Vector2Int[] FindDistantCornersInMaze(bool[,] mazeLayout)
    {
        int width = mazeLayout.GetLength(0);
        int height = mazeLayout.GetLength(1);

        // Find first walkable cell
        Vector2Int start = new Vector2Int(1, 1);
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                if (mazeLayout[x, y])
                {
                    start = new Vector2Int(x, y);
                    break;
                }
            }
        }

        // BFS to find farthest cell from start
        var farthest1 = BFSFarthestCell(mazeLayout, start);

        // BFS to find farthest cell from farthest1
        var farthest2 = BFSFarthestCell(mazeLayout, farthest1);

        return new Vector2Int[] { farthest1, farthest2 };
    }

    private Vector2Int BFSFarthestCell(bool[,] mazeLayout, Vector2Int start)
    {
        int width = mazeLayout.GetLength(0);
        int height = mazeLayout.GetLength(1);

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        bool[,] visited = new bool[width, height];

        queue.Enqueue(start);
        visited[start.x, start.y] = true;

        Vector2Int farthest = start;
        int maxDistance = 0;
        var distances = new Dictionary<Vector2Int, int> { { start, 0 } };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int distance = distances[current];

            if (distance > maxDistance)
            {
                maxDistance = distance;
                farthest = current;
            }

            // Check all 4 neighbors
            Vector2Int[] neighbors = new Vector2Int[]
            {
                new Vector2Int(current.x - 1, current.y),
                new Vector2Int(current.x + 1, current.y),
                new Vector2Int(current.x, current.y - 1),
                new Vector2Int(current.x, current.y + 1)
            };

            foreach (Vector2Int neighbor in neighbors)
            {
                if (neighbor.x > 0 && neighbor.x < width - 1 &&
                    neighbor.y > 0 && neighbor.y < height - 1 &&
                    !visited[neighbor.x, neighbor.y] &&
                    mazeLayout[neighbor.x, neighbor.y])
                {
                    visited[neighbor.x, neighbor.y] = true;
                    distances[neighbor] = distance + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return farthest;
    }

    private void CreateEditableMaze(int width, int height)
    {
        width = Mathf.Clamp(width, minMazeSize, maxMazeSize);
        height = Mathf.Clamp(height, minMazeSize, maxMazeSize);

        mazeWidth = width;
        mazeHeight = height;

        currentMaze = new MazeGrid(mazeWidth, mazeHeight);

        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                currentMaze.SetWalkable(new Vector2Int(x, y), true);
            }
        }

        startPosition = new Vector2Int(0, 0);
        finishPosition = new Vector2Int(mazeWidth - 1, mazeHeight - 1);
        currentMaze.SetWalkable(startPosition, true);
        currentMaze.SetWalkable(finishPosition, true);

        placeStartNext = true;
        activeTool = EditorTool.DrawWalls;
        MarkCurrentMazeAsEdited();

        RebuildEditorGridVisuals();
        RebuildRunnerGrids();
        InvalidateDisplayedBenchmark();
        SyncMazeSizeDropdownSelection();
        SetCurrentMazeName(string.Empty);

        UpdateInfo($"Utworzono labirynt {mazeWidth}x{mazeHeight}.");
    }

    private bool EnsureMazeExists()
    {
        if (currentMaze != null)
        {
            return true;
        }

        CreateEditableMaze(mazeWidth, mazeHeight);
        return currentMaze != null;
    }

    private void MarkCurrentMazeAsEdited()
    {
        currentMazeSource = "ManualOrEdited";
        currentMazeSeed = 0;
    }

    private void SetTool(EditorTool tool, string infoMessage)
    {
        activeTool = tool;
        UpdateInfo(infoMessage);
    }

}
