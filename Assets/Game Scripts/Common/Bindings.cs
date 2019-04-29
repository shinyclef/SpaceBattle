using System;
using System.Collections.Generic;
using UnityEngine;

public class Bindings : MonoBehaviour
{
    public Control[] Controls;

    public static Dictionary<Cmd, Control> KeyCodes { get; private set; }

    private void Awake()
    {
        SetupControls();
    }

    private void SetupControls()
    {
        KeyCodes = new Dictionary<Cmd, Control>();
        foreach (Control control in Controls)
        {
            KeyCodes.Add(control.Cmd, control);
            foreach (Control other in Controls)
            {
                if (other.Cmd != control.Cmd &&
                    other.Key == control.Key && control.Modifier != KeyCode.None)
                {
                    other.AddExcludedModifier(control.Modifier);
                }
            }
        }
    }

    [Serializable]
    public class Control
    {
        public Cmd Cmd;
        public KeyCode Key;
        public KeyCode Modifier;
        public bool DisableWithGameControls;

        private List<KeyCode> excludedModifiers;

        public void AddExcludedModifier(KeyCode modifier)
        {
            if (excludedModifiers == null)
            {
                excludedModifiers = new List<KeyCode>();
            }

            if (!excludedModifiers.Contains(modifier))
            {
                excludedModifiers.Add(modifier);
            }
        }

        public bool NoExcludedModifiersArePressed()
        {
            if (excludedModifiers == null)
            {
                return true;
            }

            foreach (KeyCode k in excludedModifiers)
            {
                if (Input.GetKey(k))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
