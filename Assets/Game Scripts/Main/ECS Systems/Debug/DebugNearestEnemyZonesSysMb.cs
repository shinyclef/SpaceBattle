using UnityEngine;

public class DebugNearestEnemyZonesSysMb : MonoBehaviour
{
    public bool EnableDebug;
    private bool debugWasEnabled;

    private void Awake()
    {
        debugWasEnabled = EnableDebug;
        DebugNearestEnemyZonesSys.I.Enabled = EnableDebug;
    }

    private void Update()
    {
        if (EnableDebug != debugWasEnabled)
        {
            debugWasEnabled = EnableDebug;
            DebugNearestEnemyZonesSys.I.Enabled = EnableDebug;
        }
    }
}