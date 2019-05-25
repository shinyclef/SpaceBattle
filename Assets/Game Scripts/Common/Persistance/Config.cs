using System;
using System.IO;
using UnityEngine;

public class Config : MonoBehaviour
{
    //-- File Paths --//
    public static string RootPath { get; private set; }
    public static string ScreenshotsPath { get; private set; }
    public static string ScreenshotsPathRelative { get; private set; }
    public static string SettingsPath { get; private set; }
    public static string SettingsPathRelative { get; private set; }
    public static string AiPath { get; private set; }
    public static string AiPathRelative { get; private set; }
    public static string PlayerSettingsPath { get; private set; }
    public static string PlayerSettingsPathRelative { get; private set; }

    public static PlayerSettingsDto PlayerSettings { get; private set; }

    private void Awake()
    {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
        SetupPaths();
        LoadPlayerSettings();
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

        SettingsPathRelative = @"Settings\";
        SettingsPath = RootPath + SettingsPathRelative;

        AiPathRelative = @"AI.json";
        AiPath = RootPath + SettingsPathRelative + AiPathRelative;

        PlayerSettingsPathRelative = @"PlayerSettings.json";
        PlayerSettingsPath = RootPath + SettingsPathRelative + PlayerSettingsPathRelative;

        // create the folders if they don't exist
        if (!Directory.Exists(ScreenshotsPath))
        {
            Directory.CreateDirectory(ScreenshotsPath);
        }

        // create the folders if they don't exist
        if (!Directory.Exists(ScreenshotsPath))
        {
            Directory.CreateDirectory(ScreenshotsPath);
        }
    }

    public static void LoadPlayerSettings()
    {
        string path = PlayerSettingsPath;
        string json;
        bool successfullyLoaded = false;
        if (File.Exists(path))
        {
            try
            {
                json = File.ReadAllText(path);
                PlayerSettings = JsonUtility.FromJson<PlayerSettingsDto>(json);
                successfullyLoaded = true;
            }
            catch (Exception)
            {
                File.Move(path, path + " (Unloadable Backup)");
            }
        }

        if (!successfullyLoaded)
        {
            PlayerSettings = new PlayerSettingsDto();
            json = JsonUtility.ToJson(PlayerSettings, true);
            File.WriteAllText(path, json);
            Logger.LogWarning(string.Format(Localizer.Strings.Error.PlayerSettingsNotLoaded, path));
        }
    }
}