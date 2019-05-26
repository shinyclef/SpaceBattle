using System;
using UnityEngine;
using UnityEngine.Events;

public class MouseClickEvent : MonoBehaviour
{
    [Serializable] public class LeftClickEvent : UnityEvent { }
    [Serializable] public class RightClickEvent : UnityEvent { }
    [Serializable] public class MiddleClickEvent : UnityEvent { }

    [SerializeField] private bool allowAnyDepth = default;
    [SerializeField] private bool activateOnMouseUpQuick = default;
    [SerializeField] private bool activateOnMouseDownLong = default;
    [SerializeField] private bool useRawMouseEvents = default;
    [SerializeField] private bool disableWhenCursorLocked = default;
    [SerializeField] private bool overrideGameMouseEvents = default;
    [SerializeField] private LeftClickEvent onLeftClick = default;
    [SerializeField] private RightClickEvent onRightClick = default;
    [SerializeField] private MiddleClickEvent onMiddleClick = default;

    private bool listenersRegistered = false;

    private void OnEnable()
    {
        RegisterListeners();
    }

    private void OnDisable()
    {
        Scheduler.InvokeAtEndOfFrame(UnregisterListeners); // this is to prevent 'collection was modified while enumerating the collection' error.
    }

    private void RegisterListeners()
    {
        if (listenersRegistered)
        {
            return;
        }

        listenersRegistered = true;

        if (activateOnMouseUpQuick)
        {
            Messenger.Global.AddListener<int>(useRawMouseEvents ? Msg.MouseUpQuickRaw : Msg.MouseUpQuick, OnMouseUpQuick);
        }

        if (activateOnMouseDownLong)
        {
            Messenger.Global.AddListener<int>(useRawMouseEvents ? Msg.MouseDownLongRaw : Msg.MouseDownLong, OnMouseDownLong);
        }

        if (overrideGameMouseEvents)
        {
            Messenger.Global.AddListener<int>(Msg.MouseDownRaw, OnMouseDownRaw);
        }
    }

    private void UnregisterListeners()
    {
        if (!listenersRegistered)
        {
            return;
        }

        listenersRegistered = false;

        if (activateOnMouseUpQuick)
        {
            Messenger.Global.RemoveListener<int>(useRawMouseEvents ? Msg.MouseUpQuickRaw : Msg.MouseUpQuick, OnMouseUpQuick);
        }

        if (activateOnMouseDownLong)
        {
            Messenger.Global.RemoveListener<int>(useRawMouseEvents ? Msg.MouseDownLongRaw : Msg.MouseDownLong, OnMouseDownLong);
        }

        if (overrideGameMouseEvents)
        {
            Messenger.Global.RemoveListener<int>(Msg.MouseDownRaw, OnMouseDownRaw);
        }
    }

    public void AddOnLeftClickListener(UnityAction listener)
    {
        onLeftClick.AddListener(listener);
    }

    public void AddOnRightClickListener(UnityAction listener)
    {
        onRightClick.AddListener(listener);
    }

    public void AddOnMiddleClickListener(UnityAction listener)
    {
        onMiddleClick.AddListener(listener);
    }

    private void OnMouseDownRaw(int mouseButton)
    {
        if (!MouseIsOverGameUiElement())
        {
            return;
        }

        if (overrideGameMouseEvents)
        {
            GInput.I.IgnoreMouseActionsStartedThisFrame(mouseButton);
        }
    }

    private void OnMouseUpQuick(int mouseButton)
    {
        if (!MouseIsOverGameUiElement())
        {
            return;
        }

        InvokeEvent(mouseButton);
    }

    private void OnMouseDownLong(int mouseButton)
    {
        if (!MouseIsOverGameUiElement())
        {
            return;
        }

        InvokeEvent(mouseButton);
    }

    private bool MouseIsOverGameUiElement()
    {
        return !(disableWhenCursorLocked && Cursor.lockState != CursorLockMode.None) && 
            ((allowAnyDepth && GInput.MouseIsOverUiElement(gameObject)) || GInput.HitObjUiTop == gameObject);
    }

    private void InvokeEvent(int mouseButton)
    {
        switch (mouseButton)
        {
            case 0:
                onLeftClick.Invoke();
                break;

            case 1:
                onRightClick.Invoke();
                break;

            case 2:
                onMiddleClick.Invoke();
                break;
        }
    }
}