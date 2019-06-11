#define LOG

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class Logger
{
    private const bool VerboseLog = true;

    public static void Log(object msg)
    {
        #if (LOG)
        Debug.Log(msg);
        #endif
    }

    public static void Msg(object msg)
    {
        Application.SetStackTraceLogType(UnityEngine.LogType.Log, StackTraceLogType.None);
        Debug.Log(msg);
        Application.SetStackTraceLogType(UnityEngine.LogType.Log, StackTraceLogType.ScriptOnly);
    }

    public static void MsgIf(bool condition, object msg)
    {
        if (condition)
        {
            Msg(msg);
        }
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