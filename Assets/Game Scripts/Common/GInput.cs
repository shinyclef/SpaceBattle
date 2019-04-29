using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class GInput : MonoBehaviour
{
    private const float RayDistance = 200f;
    private const float MouseHeldTriggerTime = 0.25f;

    private static Dictionary<Cmd, Bindings.Control> KeyCodes { get { return Bindings.KeyCodes; } }


    // 'game' mouse variables

    private static bool[] mouseButtonLong = new bool[3] { false, false, false };
    private static bool[] mouseButtonDownLong = new bool[3] { false, false, false };
    private static bool[] mouseButtonUpLong = new bool[3] { false, false, false };
    private static bool[] mouseButtonUpQuick = new bool[3] { false, false, false };
    private static bool[] mouseLongTriggered = new bool[3] { false, false, false };
    private static float[] mouseHeldDuration = new float[3] { 0f, 0f, 0f };
    private static int[] mouseDownFrame = new int[3];
    private static int[] ignoreGameMouseEventsStartedOnFrame = new int[3]; // mouse events will be ignored until the next mouse down

    // 'raw' mouse variables

    private static bool[] mouseButtonLongRaw = new bool[3] { false, false, false };
    private static bool[] mouseButtonDownLongRaw = new bool[3] { false, false, false };
    private static bool[] mouseButtonUpLongRaw = new bool[3] { false, false, false };
    private static bool[] mouseButtonUpQuickRaw = new bool[3] { false, false, false };
    private static bool[] mouseLongTriggeredRaw = new bool[3] { false, false, false };
    private static float[] mouseHeldDurationRaw = new float[3] { 0f, 0f, 0f };

    private int allExceptIgnoreRaycast;
    private bool anyKeyWasDown2ndFrame = false;
    private bool anyKeyWasDown3ndFrame = false;
    private float scrollWheel = 0f;

    private static bool mouseMovementInactiveFlag;
    private static bool keyboardMovementInactiveFlag;
    private static List<RaycastResult> raycastResultList;
    private static RaycastHit[] raycastHitBuffer = new RaycastHit[20];

    public static GInput I { get; private set; }
    public static Ray Ray { get; private set; }
    public static bool RayHit { get; private set; }
    public static RaycastHit HitInfo { get; private set; }
    public static RaycastHit[] HitsInfo { get { return raycastHitBuffer; } } // ALWAYS use in combination with HitsInfoCount!
    public static int HitsInfoCount { get; private set; }
    public static GameObject HitObj { get; private set; }
    public static bool AnyKeyActivity { get; private set; }
    public static bool SlowModifier { get; private set; }

    public static bool GameControlsInactive { get; private set; }
    public static bool MouseMovementInactive { get { return GameControlsInactive || mouseMovementInactiveFlag; } }
    public static bool KeyboardMovementInactive { get { return GameControlsInactive || keyboardMovementInactiveFlag; } }

    public static RaycastHit HitInfoUi { get; private set; }
    public static GameObject HitObjUiTop { get; private set; }

    /// <summary>
    /// Gets the screen position of the mouse in pixels. If Cursor.lockState == CursorLockMode.Locked, returns true center.
    /// </summary>
    public static Vector2 MousePos
    {
        get
        {
            return Cursor.lockState == CursorLockMode.Locked ? Game.MainCam.pixelRect.center : (Vector2)Input.mousePosition;
        }
    }


    /* ----- */
    /* Setup */
    /* ----- */

    static GInput()
    {
        for (int i = 0; i < 3; i++)
        {
            mouseDownFrame[i] = int.MinValue;
            ignoreGameMouseEventsStartedOnFrame[i] = int.MinValue;
        }

        raycastResultList = new List<RaycastResult>();
        GameControlsInactive = false;
        mouseMovementInactiveFlag = false;
        keyboardMovementInactiveFlag = false;
        RegisterListeners();
    }

    private static void RegisterListeners()
    {
    }

    private void Awake()
    {
        I = this;
        allExceptIgnoreRaycast = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        ScreenshotUtil.I.ScreenshotPath = Config.ScreenshotsPath;
    }


    /* ------- */
    /* General */
    /* ------- */

    public void IgnoreMouseActionsStartedThisFrame()
    {
        for (int i = 0; i < 3; i++)
        {
            if (Input.GetMouseButtonDown(i))
            {
                ignoreGameMouseEventsStartedOnFrame[i] = Time.frameCount;
            }
        }
    }

    public void IgnoreMouseActionsStartedThisFrame(int mouseButton)
    {
        ignoreGameMouseEventsStartedOnFrame[mouseButton] = Time.frameCount;
    }


    /* ------------ */
    /* Input Checks */
    /* ------------ */

    public static bool GetKey(KeyCode key)
    {
        return Input.GetKey(key);
    }

    public static bool GetKeyDown(KeyCode key)
    {
        return Input.GetKeyDown(key);
    }

    public static bool GetKeyAlreadyDown(KeyCode key)
    {
        return Input.GetKey(key) && !Input.GetKeyDown(key);
    }

    public static bool GetKeyUp(KeyCode key)
    {
        return Input.GetKeyUp(key);
    }

    public static bool GetButton(Cmd cmd)
    {
        if (!KeyCodes.ContainsKey(cmd) ||
            (GameControlsInactive && KeyCodes[cmd].DisableWithGameControls))
        {
            return false;
        }

        return Input.GetKey(KeyCodes[cmd].Key) &&
            ((KeyCodes[cmd].Modifier == KeyCode.None && KeyCodes[cmd].NoExcludedModifiersArePressed()) || Input.GetKey(KeyCodes[cmd].Modifier));
    }

    public static bool GetButtonDown(Cmd cmd)
    {
        if (!KeyCodes.ContainsKey(cmd) ||
            (GameControlsInactive && KeyCodes[cmd].DisableWithGameControls))
        {
            return false;
        }

        return Input.GetKeyDown(KeyCodes[cmd].Key) &&
            ((KeyCodes[cmd].Modifier == KeyCode.None && KeyCodes[cmd].NoExcludedModifiersArePressed()) || Input.GetKey(KeyCodes[cmd].Modifier));
    }

    public static bool GetButtonUp(Cmd cmd)
    {
        if (!KeyCodes.ContainsKey(cmd) ||
            (GameControlsInactive && KeyCodes[cmd].DisableWithGameControls))
        {
            return false;
        }

        return Input.GetKeyUp(KeyCodes[cmd].Key) &&
            ((KeyCodes[cmd].Modifier == KeyCode.None && KeyCodes[cmd].NoExcludedModifiersArePressed()) || Input.GetKey(KeyCodes[cmd].Modifier));
    }

    public static bool GetButtonRaw(Cmd cmd)
    {
        return KeyCodes.ContainsKey(cmd) && Input.GetKey(KeyCodes[cmd].Key) &&
            (KeyCodes[cmd].Modifier == KeyCode.None || Input.GetKey(KeyCodes[cmd].Modifier));
    }

    public static bool GetButtonDownRaw(Cmd cmd)
    {
        return KeyCodes.ContainsKey(cmd) && Input.GetKeyDown(KeyCodes[cmd].Key) &&
            (KeyCodes[cmd].Modifier == KeyCode.None || Input.GetKey(KeyCodes[cmd].Modifier));
    }

    public static bool GetButtonUpRaw(Cmd cmd)
    {
        return KeyCodes.ContainsKey(cmd) && Input.GetKeyUp(KeyCodes[cmd].Key) &&
            (KeyCodes[cmd].Modifier == KeyCode.None || Input.GetKey(KeyCodes[cmd].Modifier));
    }

    public static bool GetMouseButtonLong(int mouseButton)
    {
        return mouseButtonLong[mouseButton];
    }

    public static bool GetMouseButtonDownLong(int mouseButton)
    {
        return mouseButtonDownLong[mouseButton];
    }

    public static bool GetMouseButtonUpLong(int mouseButton)
    {
        return mouseButtonUpLong[mouseButton];
    }

    public static bool GetMouseButtonUpQuick(int mouseButton)
    {
        return mouseButtonUpQuick[mouseButton];
    }

    public static float GetAxis(InputAxis axis)
    {
        return Input.GetAxis(axis.ToString());
    }

    public static float GetAxisRaw(InputAxis axis)
    {
        return Input.GetAxisRaw(axis.ToString());
    }

    public static bool GetAxisDown(InputAxis axis)
    {
        if (!Input.anyKeyDown)
        {
            return false;
        }

        switch (axis)
        {
            case InputAxis.Vertical:
                return (Input.GetKeyDown(KeyCode.W) && !GetKeyAlreadyDown(KeyCode.D)) ||
                       (Input.GetKeyDown(KeyCode.S) && !GetKeyAlreadyDown(KeyCode.S));

            case InputAxis.Horizontal:
                return (Input.GetKeyDown(KeyCode.D) && !GetKeyAlreadyDown(KeyCode.A)) ||
                       (Input.GetKeyDown(KeyCode.A) && !GetKeyAlreadyDown(KeyCode.D));

            case InputAxis.Tertiary:
                return (Input.GetKeyDown(KeyCode.E) && !GetKeyAlreadyDown(KeyCode.Q)) ||
                       (Input.GetKeyDown(KeyCode.Q) && !GetKeyAlreadyDown(KeyCode.E));

            default:
                return false;
        }
    }

    public static bool GetAxisUp(InputAxis axis)
    {
        if (!AnyKeyActivity)
        {
            return false;
        }

        switch (axis)
        {
            case InputAxis.Vertical:
                return (Input.GetKeyUp(KeyCode.W) && !Input.GetKey(KeyCode.D)) ||
                       (Input.GetKeyUp(KeyCode.S) && !Input.GetKey(KeyCode.S));

            case InputAxis.Horizontal:
                return (Input.GetKeyUp(KeyCode.D) && !Input.GetKey(KeyCode.A)) ||
                       (Input.GetKeyUp(KeyCode.A) && !Input.GetKey(KeyCode.D));

            case InputAxis.Tertiary:
                return (Input.GetKeyUp(KeyCode.E) && !Input.GetKey(KeyCode.Q)) ||
                       (Input.GetKeyUp(KeyCode.Q) && !Input.GetKey(KeyCode.E));

            default:
                return false;
        }
    }

    public static bool GetAllAxisDown(bool vertical = true, bool horizontal = true)
    {
        if (!Input.anyKeyDown)
        {
            return false;
        }

        return (
            vertical && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S)) ||
            horizontal && (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.A)))
            && !(
            vertical && (GetKeyAlreadyDown(KeyCode.W) || GetKeyAlreadyDown(KeyCode.S)) ||
            horizontal && (GetKeyAlreadyDown(KeyCode.D) || GetKeyAlreadyDown(KeyCode.A)));
    }

    public static bool GetAllAxisUp(bool vertical = true, bool horizontal = true)
    {
        if (!AnyKeyActivity)
        {
            return false;
        }

        return (
            vertical && (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.S)) ||
            horizontal && (Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.A)))
            && !(
            vertical && (GetKey(KeyCode.W) || GetKey(KeyCode.S)) ||
            horizontal && (GetKey(KeyCode.D) || GetKey(KeyCode.A)));
    }

    public static float GetVerticalMouseAxis()
    {
        return Input.GetAxis("Mouse Y");
    }

    public static float GetHorizontalMouseAxis()
    {
        return Input.GetAxis("Mouse X");
    }

    public static float GetMouseWheel()
    {
        return I.scrollWheel;
    }

    public static float GetMouseWheelRaw()
    {
        if (I.scrollWheel == 0f)
        {
            return 0f;
        }
        else if (I.scrollWheel > 0f)
        {
            return 1f;
        }
        else
        {
            return -1f;
        }
    }

    public static bool GetMouseWheelUp()
    {
        return I.scrollWheel > 0f;
    }

    public static bool GetMouseWheelDown()
    {
        return I.scrollWheel < 0f;
    }

    public static bool MouseIsOverUiElement(GameObject uiObj)
    {
        return raycastResultList.Any(r => r.gameObject == uiObj);
    }


    /* ------ */
    /* Update */
    /* ------ */

    private void Update()
    {
        AnyKeyActivity = Input.anyKey || anyKeyWasDown2ndFrame || anyKeyWasDown3ndFrame;
        if (AnyKeyActivity)
        {
            anyKeyWasDown3ndFrame = anyKeyWasDown2ndFrame;
            anyKeyWasDown2ndFrame = Input.anyKey;

            if (Input.anyKeyDown)
            {
                Messenger.Global.Post(Msg.AnyKeyDown);
            }
        }

        UpdateRawMouseInput();
        if (!GameControlsInactive)
        {
            UpdateGameControlInput();
        }
    }

    private void UpdateGameControlInput()
    {
        GameRayCasts();
        if (AnyKeyActivity)
        {
            /* ------------ */
            /* 'Game' Mouse */
            /* ------------ */
            for (int i = 0; i < 3; i++)
            {
                mouseButtonDownLong[i] = false;
                mouseButtonUpLong[i] = false;
                mouseButtonUpQuick[i] = false;

                if (Input.GetMouseButton(i) && !MouseActionsIgnored(i))
                {
                    mouseHeldDuration[i] += Time.deltaTime;

                    if (Input.GetMouseButtonDown(i))
                    {
                        Messenger.Global.Post(Msg.MouseDown, i);
                        mouseHeldDuration[i] = 0f; // purposefully happens after adding deltaTime    
                    }

                    // check for held down triggers
                    if (!mouseLongTriggered[i] && mouseHeldDuration[i] >= MouseHeldTriggerTime)
                    {
                        mouseLongTriggered[i] = true;
                        mouseButtonDownLong[i] = true;
                        mouseButtonLong[i] = true;
                        Messenger.Global.Post(Msg.MouseDownLong, i);
                    }
                }
                else if (Input.GetMouseButtonUp(i))
                {
                    if (MouseActionsIgnored(i))
                    {
                        return;
                    }

                    if (mouseLongTriggered[i])
                    {
                        mouseButtonUpLong[i] = true;
                        mouseButtonLong[i] = false;
                        mouseLongTriggered[i] = false;
                        Messenger.Global.Post(Msg.MouseUpLong, i);
                    }
                    else
                    {
                        mouseButtonUpQuick[i] = true;
                        Messenger.Global.Post(Msg.MouseUpQuick, i);
                    }
                }
            }
        }

        /* ------------ */
        /* Scroll Wheel */
        /* ------------ */
        scrollWheel = Input.GetAxis("Mouse ScrollWheel");
        if (scrollWheel != 0f)
        {
            if (scrollWheel > 0f)
            {
                Messenger.Global.Post(Msg.MouseWheel, 1);
            }
            else
            {
                Messenger.Global.Post(Msg.MouseWheel, -1);
            }
        }
    }

    private void GameRayCasts()
    {
        Ray = Game.MainCam.ViewportPointToRay(GlobalVars.ViewportMid);
        RaycastHit hitInfo;
        RayHit = Physics.Raycast(Ray, out hitInfo, RayDistance, GlobalVars.LayerAllExceptIgnoreRaycast);

        if (RayHit)
        {
            HitsInfoCount = Physics.RaycastNonAlloc(Ray, raycastHitBuffer, RayDistance, GlobalVars.LayerAllExceptIgnoreRaycast);
            while (HitsInfoCount == raycastHitBuffer.Length)
            {
                const int growthAmount = 20;
                //Logger.LogVerbose(string.Format("RaycastHit Buffer resizing from {0} to {1}", raycastHitCount, raycastHitCount + growthAmount));
                Array.Resize(ref raycastHitBuffer, raycastHitBuffer.Length + growthAmount);
                HitsInfoCount = Physics.RaycastNonAlloc(Ray, raycastHitBuffer, RayDistance, GlobalVars.LayerAllExceptIgnoreRaycast);
            }

            HitInfo = hitInfo;
            HitObj = hitInfo.collider.gameObject;

            Messenger.Global.Post(Msg.ObjectTargeted, HitObj, hitInfo);

            if (Input.GetMouseButtonDown(0))
            {
                Messenger.Global.Post(Msg.ObjectTargetedLeftMouseDown, HitObj, hitInfo);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                Messenger.Global.Post(Msg.ObjectTargetedLeftMouseUp, HitObj, hitInfo);
            }

            if (Input.GetMouseButtonDown(1))
            {
                Messenger.Global.Post(Msg.ObjectTargetedRightMouseDown, HitObj, hitInfo);
            }
            else if (Input.GetMouseButtonUp(1))
            {
                Messenger.Global.Post(Msg.ObjectTargetedRightMouseUp, HitObj, hitInfo);
            }
        }
        else if (HitObj != null)
        {
            Messenger.Global.Post(Msg.ObjectUntargeted, HitObj);
            HitObj = null;
        }
    }

    private void UpdateRawMouseInput()
    {
        UiRayCasts();

        for (int i = 0; i < 3; i++)
        {
            mouseButtonDownLongRaw[i] = false;
            mouseButtonUpLongRaw[i] = false;
            mouseButtonUpQuickRaw[i] = false;

            if (Input.GetMouseButton(i))
            {
                mouseHeldDurationRaw[i] += Time.deltaTime;

                if (Input.GetMouseButtonDown(i))
                {
                    mouseDownFrame[i] = Time.frameCount;
                    Messenger.Global.Post(Msg.MouseDownRaw, i);
                    mouseHeldDurationRaw[i] = 0f; // purposefully happens after adding deltaTime    
                }

                // check for held down triggers
                if (!mouseLongTriggeredRaw[i] && mouseHeldDurationRaw[i] >= MouseHeldTriggerTime)
                {
                    mouseLongTriggeredRaw[i] = true;
                    mouseButtonDownLongRaw[i] = true;
                    mouseButtonLongRaw[i] = true;
                    Messenger.Global.Post(Msg.MouseDownLongRaw, i);
                }
            }
            else if (Input.GetMouseButtonUp(i))
            {
                if (mouseLongTriggeredRaw[i])
                {
                    mouseButtonUpLongRaw[i] = true;
                    mouseButtonLongRaw[i] = false;
                    mouseLongTriggeredRaw[i] = false;
                    Messenger.Global.Post(Msg.MouseUpLongRaw, i);
                }
                else
                {
                    mouseButtonUpQuickRaw[i] = true;
                    Messenger.Global.Post(Msg.MouseUpQuickRaw, i);
                }
            }
        }
    }

    private void UiRayCasts()
    {
        if (Cursor.lockState != CursorLockMode.None)
        {
            HitObjUiTop = null;
            return;
        }

        var pointer = new PointerEventData(EventSystem.current);
        pointer.position = MousePos;
        raycastResultList.Clear();
        EventSystem.current.RaycastAll(pointer, raycastResultList);
        if (raycastResultList.Count > 0)
        {
            HitObjUiTop = raycastResultList[0].gameObject;

            //Logger.LogVerbose("----------- Hits -----------");
            //foreach (var obj in raycastResultList)
            //{
            //    Logger.LogVerbose(obj.gameObject.name);
            //}
        }
        else
        {
            HitObjUiTop = null;
        }
    }


    /* ----- */
    /* Utils */
    /* ----- */

    private static bool MouseActionsIgnored(int mouseButton)
    {
        return ignoreGameMouseEventsStartedOnFrame[mouseButton] == mouseDownFrame[mouseButton];
    }
}