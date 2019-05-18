using System.IO;
using UnityEngine;

public class Config : MonoBehaviour
{
    //-- File Paths --//
    public static string RootPath { get; private set; }
    public static string ScreenshotsPath { get; private set; }
    public static string ScreenshotsPathRelative { get; private set; }
    public static string AiPath { get; private set; }
    public static string AiPathRelative { get; private set; }

    public static bool IsDev { get; private set; }

    [SerializeField]
    private bool DevelopmentBuild;

    private void Awake()
    {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
        IsDev = DevelopmentBuild;
        SetupPaths();
    }

    /// <summary>
    /// Setup the constructs and resources paths, and create the directories if they don't exist. 
    /// </summary>
    private static void SetupPaths()
    {
        // setup the paths
        RootPath = Directory.GetParent(Application.dataPath).FullName + @"\";

        ScreenshotsPathRelative = @"Screenshots\";
        ScreenshotsPath = RootPath + ScreenshotsPathRelative;

        AiPathRelative = @"AI.json";
        AiPath = RootPath + AiPathRelative;

        // create the folders if they don't exist
        if (!Directory.Exists(ScreenshotsPath))
        {
            Directory.CreateDirectory(ScreenshotsPath);
        }
    }
}