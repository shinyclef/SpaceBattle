using UnityEngine;

/// <summary>
/// This is just an object to 'tag' GameObjects in order to have them enabled and disabled on game 
/// start by the WakeUpCaller script. This is to allow 'Awake' to be called on disabled GameObjects.
/// </summary>
public class WakeUpCall : MonoBehaviour
{
    public void WakeUp()
    {
        if (gameObject.activeSelf)
        {
            return;
        }

        gameObject.SetActive(true);
    }

    public void GoToSleep()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        gameObject.SetActive(false);
    }
}