﻿using UnityEngine;

public class DebugRaysSysMb : MonoBehaviour
{
    public bool EnableDebug;
    private bool debugWasEnabled;

    private void Awake()
    {
        debugWasEnabled = EnableDebug;
        DebugRaysSys.I.Enabled = EnableDebug;
    }

    private void Update()
    {
        if (EnableDebug != debugWasEnabled)
        {
            debugWasEnabled = EnableDebug;
            DebugRaysSys.I.Enabled = EnableDebug;
        }
    }
}