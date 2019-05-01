using UnityEngine;

public class Game : MonoBehaviour
{
    public const string ScreenshotPrefix = "Space Battle ";

    public static Camera MainCam { get; private set; }

    private void Awake()
    {
        MainCam = Camera.main;
    }
}