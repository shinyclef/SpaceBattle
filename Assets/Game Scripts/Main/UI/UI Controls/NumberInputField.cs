using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NumberInputField : MonoBehaviour
{
    [Serializable] public class ChangedEvent : UnityEvent<NumberInputField> { }

    [SerializeField] private Color invalidColour = default;
    [SerializeField] private ChangedEvent OnValueChanged = default;
    [SerializeField] private ChangedEvent OnValueChangedValid = default;

    private TMP_InputField input = default;
    private Image inputImage = default;
    private Color validColour;
    private bool isSetup = false;

    public float LastValidValue { get; private set; }
    public bool ValidStateChanged { get; private set; }
    public bool IsValid { get; private set; }

    private void Awake()
    {
        Setup();
    }

    public void Setup()
    {
        if (isSetup)
        {
            return;
        }

        isSetup = true;
        input = GetComponent<TMP_InputField>();
        inputImage = GetComponent<Image>();
        validColour = inputImage.color;
        IsValid = true;
        ValidStateChanged = false;
        LastValidValue = float.Parse(input.text);
    }

    public bool IsDirty(object cleanValue)
    {
        return IsValid && (float)cleanValue != GetValue();
    }

    public float GetValue()
    {
        return float.Parse(input.text);
    }

    public void SetValue(float val)
    {
        input.text = val.ToString();
    }

    public void SetValueWithoutNotify(float val)
    {
        input.SetTextWithoutNotify(val.ToString());
        bool wasValid = IsValid;
        IsValid = FloatInputValidator.GetValidValue(input.text, out val);
        ValidStateChanged = IsValid != wasValid;
        DisplayAsValid(IsValid);
        if (IsValid)
        {
            LastValidValue = val;
        }
    }

    public void OnChanged()
    {
        float val;
        bool wasValid = IsValid;
        IsValid = FloatInputValidator.GetValidValue(input.text, out val);
        ValidStateChanged = IsValid != wasValid;
        DisplayAsValid(IsValid);
        OnValueChanged?.Invoke(this);
        if (IsValid)
        {
            LastValidValue = val;
            OnValueChangedValid?.Invoke(this);
        }
    }

    private void DisplayAsValid(bool valid)
    {
        inputImage.color = valid ? validColour : invalidColour;
    }
}
