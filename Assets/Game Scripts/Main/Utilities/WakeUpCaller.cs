using UnityEngine;

/// <summary>
/// This script finds all objects that have a 'WakeUpCall' component attached to them,
/// and enables and disables them (if they are disabled) to ensure their 'Aawke' method is called.
/// </summary>
public class WakeUpCaller : MonoBehaviour
{
    private void Awake()
    {
        WakeUpCall[] calls = Resources.FindObjectsOfTypeAll<WakeUpCall>();
        for (int i = 0; i < calls.Length; i++)
        {
            calls[i].WakeUp();
        }

        for (int i = 0; i < calls.Length; i++)
        {
            calls[i].GoToSleep();
        }
    }
}