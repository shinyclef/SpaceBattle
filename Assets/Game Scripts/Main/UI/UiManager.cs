using UnityEngine;

public class UiManager : MonoBehaviour
{
    [SerializeField] private GameObject[] uiToggleElements = default;
    [SerializeField] private GameObject aiInspector = default;

    bool uiActive = true;

    private void Update()
    {
        if (!GInput.AnyKeyActivity)
        {
            return;
        }

        if (GInput.GetButtonDown(Cmd.ToggleHelpOverlay))
        {
            uiActive = !uiActive;
            foreach (GameObject go in uiToggleElements)
            {
                go.SetActive(uiActive);
            }
        }

        if (GInput.GetButtonDown(Cmd.ToggleAiInspector))
        { 
            aiInspector.SetActive(!aiInspector.activeSelf);
        }
    }
}