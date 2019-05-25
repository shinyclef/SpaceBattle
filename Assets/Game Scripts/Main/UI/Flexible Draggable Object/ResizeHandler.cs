using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EventTrigger))]
public class ResizeHandler : MonoBehaviour
{
    [SerializeField] private HandlerType Type = default;

    private Vector2 minSize = new Vector2(50, 50);
    private Vector2 maxSize = new Vector2(800, 800);
    private RectTransform target;
    private EventTrigger eventTrigger;
    
	private void Start()
	{
	    eventTrigger = GetComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        entry.callback.AddListener(OnDrag);
        eventTrigger.triggers.Add(entry);

        FlexibleDraggableObject flex = transform.parent.gameObject.GetComponent<FlexibleDraggableObject>();
        target = flex.Target;
        minSize = flex.MinSize;
        maxSize = flex.MaxSize;
    }

    private void OnDrag(BaseEventData data)
    {
        PointerEventData ped = (PointerEventData) data;
        RectTransform.Edge? horizontalEdge = null;
        RectTransform.Edge? verticalEdge = null;

        switch (Type)
        {
            case HandlerType.BottomLeft:
                verticalEdge = RectTransform.Edge.Bottom;
                horizontalEdge = RectTransform.Edge.Left;
                break;
            case HandlerType.Left:
                horizontalEdge = RectTransform.Edge.Left;
                break;
            case HandlerType.TopLeft:
                verticalEdge = RectTransform.Edge.Top;
                horizontalEdge = RectTransform.Edge.Left;
                break;
            case HandlerType.Top:
                verticalEdge = RectTransform.Edge.Top;
                break;
            case HandlerType.TopRight:
                verticalEdge = RectTransform.Edge.Top;
                horizontalEdge = RectTransform.Edge.Right;
                break;
            case HandlerType.Right:
                horizontalEdge = RectTransform.Edge.Right;
                break;
            case HandlerType.BottomRight:
                verticalEdge = RectTransform.Edge.Bottom;
                horizontalEdge = RectTransform.Edge.Right;
                break;
            case HandlerType.Bottom:
                verticalEdge = RectTransform.Edge.Bottom;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        target.ForceUpdateRectTransforms();
        Vector2 pivot = target.pivot;
        target.SetPivot(Vector2.zero);
        target.ForceUpdateRectTransforms();
        Vector3 pos = target.localPosition;
        Vector2 size = target.sizeDelta;

        if (horizontalEdge != null)
        {
            if (horizontalEdge == RectTransform.Edge.Right)
            {
                size = new Vector2(Mathf.Clamp(target.rect.width + ped.delta.x, minSize.x, maxSize.x), size.y);
            }
            else
            {
                float attemptedSizeX = target.rect.width - ped.delta.x;
                float actualSizeX = Mathf.Clamp(attemptedSizeX, minSize.x, maxSize.x);
                size = new Vector2(actualSizeX, size.y);
                pos.x += ped.delta.x - (actualSizeX - attemptedSizeX);
            } 
        }

        if (verticalEdge != null)
        {
            if (verticalEdge == RectTransform.Edge.Top)
            {
                size = new Vector2(size.x, Mathf.Clamp(target.rect.height + ped.delta.y, minSize.y, maxSize.y));
            }
            else
            {
                float attemptedSizeY = target.rect.height - ped.delta.y;
                float actualSizeY = Mathf.Clamp(attemptedSizeY, minSize.y, maxSize.y);
                size = new Vector2(size.x, actualSizeY);
                pos.y += ped.delta.y - (actualSizeY - attemptedSizeY);
            }
        }

        target.localPosition = pos;
        target.ForceUpdateRectTransforms();
        target.sizeDelta = size;
        target.SetPivot(pivot);
    }

    private enum HandlerType
    {
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left,
        TopLeft,
        Top
    }
}
