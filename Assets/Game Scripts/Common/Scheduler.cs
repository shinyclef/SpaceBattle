using System;
using System.Collections;
using UnityEngine;

public class Scheduler : MonoBehaviour
{
    private static Scheduler I;

    private void Awake()
    {
        I = this;
    }

    public static void InvokeAtEndOfFrame(Action action)
    {
        if (I.gameObject.activeInHierarchy)
        {
            I.StartCoroutine(WaitForEndOfFrameThenRun(action));
        }
    }

    public static void InvokeAfterOneFrame(Action action)
    {
        if (I.gameObject.activeInHierarchy)
        {
            I.StartCoroutine(WaitForFramesThenRun(action, 1));
        }
    }

    public static void InvokeAfterFrames(Action action, int frames)
    {
        if (I.gameObject.activeInHierarchy)
        {
            I.StartCoroutine(WaitForFramesThenRun(action, frames));
        }
    }

    public static void InvokeAfterSeconds(Action action, float seconds, bool realtime)
    {
        if (I.gameObject.activeInHierarchy)
        {
            I.StartCoroutine(WaitForSecondsThenRun(action, seconds, realtime));
        }
    }

    public static void CallWhenTrue(Func<bool> condition, Action callback)
    {
        I.StartCoroutine(CallActionWhenTrueCoroutine(condition, callback));
    }

    private static IEnumerator WaitForEndOfFrameThenRun(Action action)
    {
        yield return new WaitForEndOfFrame();
        action();
    }

    private static IEnumerator CallActionWhenTrueCoroutine(Func<bool> condition, Action callback)
    {
        yield return new WaitUntil(condition);
        callback();
    }

    private static IEnumerator WaitForFramesThenRun(Action action, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return null;
        }

        action();
    }

    public static IEnumerator WaitForSecondsThenRun(Action action, float seconds, bool realtime)
    {
        float ellapsed = 0f;
        while (ellapsed < seconds)
        {
            yield return null;
            ellapsed += realtime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        action();
    }
}