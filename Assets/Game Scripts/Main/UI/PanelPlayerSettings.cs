using System;
using UnityEngine;

public class PanelPlayerSettings : MonoBehaviour
{
    [SerializeField] private RectTransform aiInspectorTransform = default;

    private Vector2 defaultAiInspectorPos;
    private Vector2 defaultAiInspectorSize;

    public static PanelPlayerSettings I { get; private set; }

    private Vector2 AiInspectorPos
    {
        get { return aiInspectorTransform.anchoredPosition; }
        set { aiInspectorTransform.anchoredPosition = value; }
    }

    private Vector2 AiInspectorSize
    {
        get { return aiInspectorTransform.sizeDelta; }
        set { aiInspectorTransform.sizeDelta = value; }
    }

    private void Awake()
    {
        I = this;
        Messenger.Global.AddListener(Msg.ExitGameStart, SavePanelSettings);
        GetDefaults();
    }

    private void Start()
    {
        try
        {
            LoadInventoryPanelTransformSettings();
        }
        catch (Exception)
        {
            SavePanelSettings();
        }
    }

    private void GetDefaults()
    {
        defaultAiInspectorPos = AiInspectorPos;
        defaultAiInspectorSize = AiInspectorSize;
    }

    public void RestoreDefaultAiInspectorSettings()
    {
        AiInspectorPos = defaultAiInspectorPos;
        AiInspectorSize = defaultAiInspectorSize;
    }

    public void LoadInventoryPanelTransformSettings()
    {
        AiInspectorPos = Util.ParseVector2(Config.PlayerSettings.AiInspectorPosition);
        AiInspectorSize = Util.ParseVector2(Config.PlayerSettings.AiInspectorSize);
    }

    private void SavePanelSettings()
    {
        Config.PlayerSettings.AiInspectorPosition = AiInspectorPos.ToString();
        Config.PlayerSettings.AiInspectorSize = AiInspectorSize.ToString();
    }
}