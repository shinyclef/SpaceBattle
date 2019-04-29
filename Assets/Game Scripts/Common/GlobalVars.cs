using UnityEngine;

public class GlobalVars
{
    public const int LayerAllExceptIgnoreRaycast = ~(1 << 2);
    public const int LayerRaycastOnly = 1 << 9;
    public const int LayerTemplateBlock = 1 << 11;
    public static readonly Vector3 ViewportMid = new Vector3(0.5f, 0.5f, 0f);
}