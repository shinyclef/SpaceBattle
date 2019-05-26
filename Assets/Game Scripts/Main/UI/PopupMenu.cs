using UnityEngine;

public class PopupMenu : MonoBehaviour
{
    [SerializeField] private RectTransform bounds = default;
    [SerializeField] private RectTransform popupObject = default;
    [SerializeField] private Vector3 positionOffset = default;

    private GameObject[] menuItems;
    private bool listenersRegistered = false;
    private int toggledFrame;

    public RectTransform PopupObject { get { return popupObject; } }

    private void Awake()
    {
        menuItems = new GameObject[popupObject.childCount];
        toggledFrame = -1;
        for (int i = 0; i < popupObject.childCount; i++)
        {
            menuItems[i] = popupObject.GetChild(i).gameObject;
        }
    }

    public void Activate(int menuDisplayMask)
    {
        if (toggledFrame == Time.frameCount)
        {
            return;
        }

        RegisterListeners();
        SetActiveElements(menuDisplayMask);
        Vector3 pos = DeterminePosition();
        popupObject.gameObject.SetActive(true);
        popupObject.position = pos;
        popupObject.ForceUpdateRectTransforms();
        toggledFrame = Time.frameCount;
    }

    public void Deactivate()
    {
        if (toggledFrame == Time.frameCount)
        {
            return;
        }

        Scheduler.InvokeAtEndOfFrame(() => UnregisterListeners());
        popupObject.gameObject.SetActive(false);
        toggledFrame = Time.frameCount;
    }

    private void RegisterListeners()
    {
        if (listenersRegistered)
        {
            return;
        }

        listenersRegistered = true;
        Messenger.Global.AddListener<int>(Msg.MouseUpQuick, OnMouseUp);
        Messenger.Global.AddListener<int>(Msg.MouseUpLong, OnMouseUp);
    }

    private void UnregisterListeners()
    {
        if (!listenersRegistered)
        {
            return;
        }

        listenersRegistered = false;
        Messenger.Global.RemoveListener<int>(Msg.MouseUpQuick, OnMouseUp);
        Messenger.Global.RemoveListener<int>(Msg.MouseUpLong, OnMouseUp);
    }

    private void OnMouseUp(int mouseButton)
    {
        Deactivate();
    }

    private Vector3 DeterminePosition()
    {
        Vector3 origin = Input.mousePosition;
        if (bounds == null)
        {
            return origin + positionOffset;
        }

        // get the corners of the bounds object in screen space
        Vector3[] corners = new Vector3[4];
        bounds.GetWorldCorners(corners);
        for (int i = 0; i < corners.Length; i++)
        {
            corners[i] = RectTransformUtility.WorldToScreenPoint(Game.MainCam, corners[i]);
        }

        // adjustment for the popup object
        Vector4 adjustment = new Vector4(
            positionOffset.x - (popupObject.rect.width  / 2f * popupObject.lossyScale.x),
            positionOffset.y - (popupObject.rect.height / 2f * popupObject.lossyScale.y),
            positionOffset.x + (popupObject.rect.width  / 2f * popupObject.lossyScale.x),
            positionOffset.y + (popupObject.rect.height / 2f * popupObject.lossyScale.y));

        // get the margins from the edges of the popup object to the edges of the bounds
        Vector4 margin = new Vector4(
            origin.x - corners[0].x + adjustment.x,
            origin.y - corners[0].y + adjustment.y,
            corners[2].x - origin.x - adjustment.z,
            corners[2].y - origin.y - adjustment.w);

        if (margin.x < 0)
        {
            origin.x -= margin.x;
        }

        if (margin.y < 0)
        {
            origin.y -= margin.y;
        }

        if (margin.z < 0)
        {
            origin.x += margin.z;
        }

        if (margin.w < 0)
        {
            origin.y += margin.w;
        }

        return origin + positionOffset;
    }

    private void SetActiveElements(int menuDisplayMask)
    {
        int mask = 1;
        for (int i = 0; i < menuItems.Length; i++)
        {
            // shift to get the mask, and check if this MenuItem should be displayed
            mask = 1 << i;
            menuItems[i].SetActive((mask & menuDisplayMask) == mask);
        }
    }
}