using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UiHotkeySelect : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            GameObject currentGo = EventSystem.current.currentSelectedGameObject;
            if (currentGo == null)
            {
                return;
            }

            Selectable current = currentGo.GetComponent<Selectable>();
            if (current != null && current.navigation.mode == Navigation.Mode.Explicit)
            {
                Selectable target = Input.GetKey(KeyCode.LeftShift) ? current.FindSelectableOnUp() : current.FindSelectableOnDown();
                if (target != null)
                {
                    InputField inputfield = target.GetComponent<InputField>();
                    if (inputfield != null)
                    {
                        // if it's an input field, also set the text caret
                        inputfield.OnPointerClick(new PointerEventData(EventSystem.current));
                    }

                    EventSystem.current.SetSelectedGameObject(target.gameObject, new BaseEventData(EventSystem.current));
                }
            }
        }
    }
}