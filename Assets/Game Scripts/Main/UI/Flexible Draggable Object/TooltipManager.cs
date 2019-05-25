using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    [SerializeField] private RectTransform panelRt = default;
    [SerializeField] private TextMeshProUGUI tooltipText = default;
    [SerializeField] private Vector2 padding = default;
    [SerializeField] private Vector2 offsetFromMouse = default;

    private RectTransform rt;
    private Dictionary<GameObject, Func<string>> tooltipObjects;
    private Func<string> textFunc;
    private bool tooltipActive;

    public static TooltipManager I { get; private set; }

    private void Awake()
    {
        rt = (RectTransform)transform;
        tooltipObjects = new Dictionary<GameObject, Func<string>>();
        tooltipActive = false;
        I = this;
    }

    private void Update()
    {
        UpdateMouseOverTooptip();
        if (tooltipActive)
        {
            PositionByMouse();
        }
    }

    public void RegisterForTooltip(GameObject go, Func<string> tooltipTextFunc)
    {
        UnregisterForTooltip(go);
        tooltipObjects.Add(go, tooltipTextFunc);
    }

    public void UnregisterForTooltip(GameObject go)
    {
        if (tooltipObjects.ContainsKey(go))
        {
            tooltipObjects.Remove(go);
        }
    }

    private void UpdateMouseOverTooptip()
    {
        if (GInput.HitObjUiTop == null)
        {
            SetTooltipActive(false);
            return;
        }

        if (tooltipObjects.TryGetValue(GInput.HitObjUiTop, out textFunc))
        {
            SetTooltipActive(true);
            SetTooltipText(textFunc());
        }
        else
        {
            SetTooltipActive(false);
        }
    }

    private void SetTooltipActive(bool active)
    {
        if (tooltipActive != active)
        {
            tooltipActive = active;
            panelRt.gameObject.SetActive(active);
        }
    }
    
    private void SetTooltipText(string newText)
    {
        tooltipText.text = newText;
        AdjustPanelToFitText();
    }

    private void AdjustPanelToFitText()
    {
        tooltipText.ForceMeshUpdate();
        panelRt.sizeDelta = (Vector2)tooltipText.bounds.size + padding;
    }

    private void PositionByMouse()
    {
        float uiScale = Config.PlayerSettings.UiScalePercent / 100f;
        Vector2 size = panelRt.sizeDelta * uiScale;

        Vector2 targetPos = (GInput.MousePos + offsetFromMouse);
        Vector2 maxPox = new Vector2(Screen.width - size.x, Screen.height - size.y);

        targetPos = Vector2.Max(targetPos, Vector2.zero);
        targetPos = Vector2.Min(targetPos, maxPox);

        panelRt.anchoredPosition = targetPos / uiScale;
    }
}