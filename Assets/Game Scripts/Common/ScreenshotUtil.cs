using System;
using UnityEngine;

public class ScreenshotUtil : MonoBehaviour
{
    public string ScreenshotPath;

    public static ScreenshotUtil I { get; private set; }

    private void Awake()
    {
        I = this;
    }

    private void LateUpdate()
    {
        if (GInput.GetButtonDown(Cmd.ScreenShot))
        {
            ScreenCapture.CaptureScreenshot(ScreenshotPath + Game.ScreenshotPrefix + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png");
        }
    }
}