using System;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    public const string ScreenshotPrefix = "Space Battle ";

    public static Game I { get; private set; }
    public static Camera MainCam { get; private set; }
    public static bool IsQuitting{ get; private set; }

    private static List<Action> updateCallbacks;

    private void Awake()
    {
        IsQuitting = false;
        MainCam = Camera.main;
        I = this;

        Application.targetFrameRate = 50;

        if (updateCallbacks == null)
        {
            updateCallbacks = new List<Action>();
        }
    }

    public static void RegisterForUpdate(Action callback)
    {
        if (updateCallbacks == null)
        {
            updateCallbacks = new List<Action>();
        }

        if (!updateCallbacks.Contains(callback))
        {
            updateCallbacks.Add(callback);
        }
    }

    private void OnApplicationQuit()
    {
        IsQuitting = true;
        Messenger.Global.Post(Msg.ExitGameStart);
        Messenger.Global.Post(Msg.ExitGame);
    }

    private void Update()
    {
        for (int i = 0; i < updateCallbacks.Count; i++)
        {
            updateCallbacks[i]();
        }
    }
}