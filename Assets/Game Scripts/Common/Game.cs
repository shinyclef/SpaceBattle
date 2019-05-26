using UnityEngine;

public class Game : MonoBehaviour
{
    public const string ScreenshotPrefix = "Space Battle ";

    public static Camera MainCam { get; private set; }
    public static bool IsQuitting{ get; private set; }

    private void Awake()
    {
        IsQuitting = false;
        MainCam = Camera.main;
    }

    private void OnApplicationQuit()
    {
        IsQuitting = true;
        Messenger.Global.Post(Msg.ExitGameStart);
        Messenger.Global.Post(Msg.ExitGame);
    }
}