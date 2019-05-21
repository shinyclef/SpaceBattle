using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class Logger
{
    private const bool VerboseLog = true;

    public static void LogPrivate(object msg)
    {
        Debug.Log(msg);
    }

    public static void LogVerbosePrivate(object msg)
    {
        Debug.Log(msg);
    }

    public static void LogWarningPrivate(object msg)
    {
        Debug.LogWarning(msg);
    }

    public static void LogErrorPrivate(object msg)
    {
        Debug.LogError(msg);
    }

    public static void Log(object msg)
    {
        Debug.Log(msg);
    }

    public static void LogVerbose(object msg, string tag, bool stackTrace = false)
    {
        if (VerboseLog)
        {
            if (stackTrace)
            {
                msg = $"#{tag}#{msg}{Environment.NewLine}{StackTraceUtility.ExtractStackTrace()}";
            }

            Debug.Log(msg);
        }
    }

    public static void LogVerbose(object msg, bool stackTrace = false)
    {
        if (VerboseLog)
        {
            if (stackTrace)
            {
                msg = $"{msg}{Environment.NewLine}{StackTraceUtility.ExtractStackTrace()}";
            }

            Debug.Log(msg);
        }
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
        LogVerbose($"{methodName}{(msg == null ? "" : ":")} {msg}", stackTrace);
    }

    public static void LogIf(bool condition, object msg, bool stackTrace = false)
    {
        if (condition)
        {
            if (stackTrace)
            {
                msg = $"{msg}{Environment.NewLine}{StackTraceUtility.ExtractStackTrace()}";
            }

            Debug.Log(msg);
        }
    }

    public static void LogWarning(object msg, bool stackTrace = true)
    {
        if (stackTrace)
        {
            msg = $"{msg}{Environment.NewLine}{StackTraceUtility.ExtractStackTrace()}";
        }

        Debug.LogWarning(msg);
    }

    public static void LogError(object msg, bool stackTrace = true)
    {
        if (stackTrace)
        {
            msg = $"{msg}{Environment.NewLine}{StackTraceUtility.ExtractStackTrace()}";
        }

        Debug.LogError(msg);
    }

    public enum LogType : short
    {
        Standard = 1,
        Verbose,
        Warning,
        Error
    }
}