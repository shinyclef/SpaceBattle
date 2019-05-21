using UnityEngine;

public class UiManager : MonoBehaviour
{
    [SerializeField] private GameObject AiInspector = default;

    private void Update()
    {
        if (!GInput.AnyKeyActivity)
        {
            return;
        }

        if (GInput.GetButtonDown(Cmd.ToggleAiInspector))
        { 
            AiInspector.SetActive(!AiInspector.activeSelf);
        }
    }
}