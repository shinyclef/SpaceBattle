using UnityEngine;

public class DebugRaysSysMb : MonoBehaviour
{
    public bool EnableDebugRays;
    private bool debugRaysWereEnabled;

    private void Awake()
    {
        debugRaysWereEnabled = EnableDebugRays;
        DebugRaysSys.I.Enabled = EnableDebugRays;
    }

    private void Update()
    {
        if (EnableDebugRays != debugRaysWereEnabled)
        {
            debugRaysWereEnabled = EnableDebugRays;
            DebugRaysSys.I.Enabled = EnableDebugRays;
        }
    }
}