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

    private void Start()
    {
        Messenger.Global.AddListener(Msg.ExitGame, OnExitGame);
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
        string json;
        bool successfullyLoaded = false;
        if (File.Exists(PlayerSettingsPath))
        {
            try
            {
                json = File.ReadAllText(PlayerSettingsPath);
                PlayerSettings = JsonUtility.FromJson<PlayerSettingsDto>(json);
                successfullyLoaded = true;
            }
            catch (Exception)
            {
                File.Move(PlayerSettingsPath, PlayerSettingsPath + " (Unloadable Backup)");
            }
        }

        if (!successfullyLoaded)
        {
            PlayerSettings = new PlayerSettingsDto();
            SavePlayerSettings();
            Logger.LogWarning(string.Format(Localizer.Strings.Error.PlayerSettingsNotLoaded, PlayerSettingsPath));
        }
    }

    public static void SavePlayerSettings()
    {
        string json = JsonUtility.ToJson(PlayerSettings, true);
        File.WriteAllText(PlayerSettingsPath, json);
    }

    private static void OnExitGame()
    {
        SavePlayerSettings();
    }
}