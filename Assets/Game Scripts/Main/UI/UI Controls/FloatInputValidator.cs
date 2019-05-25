using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Assumes input that has come from an input field with decimal validation.
/// </summary>
public static class FloatInputValidator
{
    public static bool ValueIsValid(string input, float min = float.MinValue, float max = float.MaxValue, bool integer = false)
    {
        float val;
        if (string.IsNullOrEmpty(input.Trim()))
        {
            return false;
        }

        if (input == "-" || input == "." || input.EndsWith("."))
        {
            return false;
        }

        try
        {
            val = (float)Math.Round(float.Parse(input, CultureInfo.InvariantCulture), 3);
        }
        catch (Exception e)
        {
            Assert.IsTrue(false, "Uncaught parse exception: " + e);
            return false;
        }

        if (val < min || val > max)
        {
            return false;
        }

        if (integer && Mathf.Round(val) != val)
        {
            return false;
        }

        return true;
    }

    public static bool GetValidValue(string input, out float val, bool clamp = false, float min = float.MinValue, float max = float.MaxValue, bool integer = false)
    {
        if (string.IsNullOrEmpty(input.Trim()))
        {
            val = 0f;
            return false;
        }

        if (input == "-" || input == "." || input.EndsWith("."))
        {
            val = 0f;
            return false;
        }

        try
        {
            val = (float)Math.Round(float.Parse(input, CultureInfo.InvariantCulture), 3);
        }
        catch (Exception e)
        {
            Assert.IsTrue(false, "Uncaught parse exception: " + e);
            val = 0f;
            return false;
        }

        if (clamp)
        {
            val = Mathf.Clamp(val, min, max);
        }
        else
        {
            if (val < min || val > max)
            {
                return false;
            }
        }

        if (integer)
        {
            val = Mathf.Round(val);
        }

        return true;
    }
}