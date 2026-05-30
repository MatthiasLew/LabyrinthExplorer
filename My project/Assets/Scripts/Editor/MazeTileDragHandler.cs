using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Przekazuje do MazeAppController zdarzenia malowania kafelków myszką.
/// Tryb narzędzia jest rozstrzygany przez kontroler, a nie przez komponent pola.
/// </summary>
public class MazeTileDragHandler : MonoBehaviour,
    IPointerDownHandler,
    IPointerEnterHandler,
    IPointerUpHandler
{
    private static bool globalDragActive;

    private Vector2Int tilePosition;
    private Action<Vector2Int, bool> onTileDragAction;

    public void Initialize(Vector2Int position, Action<Vector2Int, bool> dragCallback)
    {
        tilePosition = position;
        onTileDragAction = dragCallback;
    }

    public static void StartGlobalDrag(bool drawMode)
    {
        globalDragActive = true;
    }

    public static void EndGlobalDrag()
    {
        globalDragActive = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        globalDragActive = true;
        onTileDragAction?.Invoke(tilePosition, true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!globalDragActive || !Input.GetMouseButton(0))
        {
            return;
        }

        onTileDragAction?.Invoke(tilePosition, true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        globalDragActive = false;
        onTileDragAction?.Invoke(tilePosition, false);
    }
}
