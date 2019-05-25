using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EventTrigger))]
public class DragHandler : MonoBehaviour
{
    private RectTransform target;
    private EventTrigger eventTrigger;

    private void Start ()
    {
        eventTrigger = GetComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        entry.callback.AddListener(OnDrag);
        eventTrigger.triggers.Add(entry);
        target = transform.parent.gameObject.GetComponent<FlexibleDraggableObject>().Target;
    }

    private void OnDrag(BaseEventData data)
    {
        PointerEventData ped = (PointerEventData) data;
        target.transform.Translate(ped.delta);
    }
}