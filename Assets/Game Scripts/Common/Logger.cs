#define LOG

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Logger : MonoBehaviour
{
    private const bool VerboseLog = true;
    private static List<string> frameLogs = new List<string>();
    private static int lastLoggedFrame = 0;
    private static bool handleLogs = true;
    private static int frameCount;

    private void Update()
    {
        frameCount = Time.frameCount;
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        Application.logMessageReceivedThreaded += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
        Application.logMessageReceivedThreaded -= HandleLog;
        frameLogs.Clear();
    }

    public static void HandleLog(string logString, string stackTrace, UnityEngine.LogType type)
    {
        if (type == UnityEngine.LogType.Exception && handleLogs)
        {
            handleLogs = false;
            lock (frameLogs)
            {
                foreach (string msg in frameLogs)
                {
                    Debug.Log(msg);
                }

                frameLogs.Clear();
            }

            handleLogs = true;
        }
    }

    public static void Log(object msg)
    {
#if (LOG)
        Debug.Log(msg);
#endif
    }

    public static void OnError(object msg)
    {
#if (LOG)
        if (frameCount > lastLoggedFrame)
        {
            lastLoggedFrame = frameCount;
            frameLogs.Clear();
        }

        frameLogs.Add(msg.ToString());
#endif
    }

    public static void LogVerbose(object msg, string tag, bool stackTrace = false)
    {
#if (LOG)
        if (VerboseLog)
        {
            if (stackTrace)
            {
                msg = $"#{tag}#{msg}{Environment.NewLine}{StackTraceUtility.ExtractStackTrace()}";
            }

            Debug.Log(msg);
        }
#endif
    }

    public static void LogVerbose(object msg, bool stackTrace = false)
    {
#if (LOG)
        if (VerboseLog)
        {
            if (stackTrace)
            {
                msg = $"{msg}{Environment.NewLine}{StackTraceUtility.ExtractStackTrace()}";
            }
            
            Debug.Log(msg);
        }
#endif
    }

    public static void LogVerboseIf(bool condition, object msg)
    {
        if (condition)
        {
            LogVerbose(msg);
        }
    }

    public static void LogMethod(object msg = null, [CallerMemberName] string methodName = null, bool stackTrace = false)
    {
        #if (LOG)
        LogVerbose($"{methodName}{(msg == null ? "" : ":")} {msg}", stackTrace);
        #endif
    }

    public static void LogIf(bool condition, object msg, bool stackTrace = false)
    {
#if (LOG)
        if (condition)
        {
            if (stackTrace)
            {
                msg = $"{msg}{Environment.NewLine}{StackTraceUtility.ExtractStackTrace()}";
            }
            
            Debug.Log(msg);
        }
#endif
    }

    public static void LogWarning(object msg, bool stackTrace = true)
    {
#if (LOG)
        if (stackTrace)
        {
            msg = $"{msg}{Environment.NewLine}{StackTraceUtility.ExtractStackTrace()}";
        }
        
        Debug.LogWarning(msg);
#endif
    }

    public static void LogError(object msg, bool stackTrace = true)
    {
#if (LOG)
        if (stackTrace)
        {
            msg = $"{msg}{Environment.NewLine}{StackTraceUtility.ExtractStackTrace()}";
        }
        
        Debug.LogError(msg);
#endif
    }

    public enum LogType : short
    {
        Standard = 1,
        Verbose,
        Warning,
        Error
    }
}