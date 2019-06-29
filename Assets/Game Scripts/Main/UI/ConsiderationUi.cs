using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class ConsiderationUi : MonoBehaviour
{
    private const float MultiChoiceReorderButtonsHeight = 32.5f;

    [SerializeField] private TextMeshProUGUI label = default;
    [SerializeField] private TextMeshProUGUI score = default;
    [SerializeField] private TextMeshProUGUI[] scoreLabelMulti = default;
    [SerializeField] private Toggle toggle = default;
    [SerializeField] private Image toggleForeground = default;
    [SerializeField] private RectTransform reorderPanel = default;
    [SerializeField] private RectTransform[] reorderButtons = default;
    [SerializeField] private GameObject multiScorePanel = default;
    [SerializeField] private RectTransform inputsPanel = default;
    [SerializeField] private TMP_InputField factFromInput = default;
    [SerializeField] private TMP_InputField factToInput = default;
    [SerializeField] private TMP_InputField slopeInput = default;
    [SerializeField] private TMP_InputField expInput = default;
    [SerializeField] private TMP_InputField xInput = default;
    [SerializeField] private TMP_InputField yInput = default;
    [SerializeField] private float collapsedHeight = default;
    [SerializeField] private float expandedHeight = default;
    [SerializeField] private float expandedMultiHeight = default;
    [SerializeField] private GameObject graphBallPrefab = default;

    private bool isMultiTarget;
    private int maxTargets;
    private bool expanded;
    private ChoiceUi choice;
    private Image graphBall;
    private LayoutElement layoutElement;
    private RectTransform graph;
    private UILineRenderer graphLine;
    
    
    private int recordedDataKey;
    private float bestFactInputValue;
    private float bestScoreValue;

    public ConsiderationDto Dto { get; private set; }

    private float CurrentHeight { get { return expanded ? ExpandedHeight : collapsedHeight; } }
    private float ExpandedHeight { get { return isMultiTarget ? expandedMultiHeight : expandedHeight; } }

    #region Events

    public void OnRemoveButtonPressed()
    {
        Remove(true);
    }

    public void OnFactFromInputChanged(NumberInputField numberField)
    {
        if (numberField.IsValid)
        {
            Dto.InputMin = numberField.GetValue();
            RedrawGraphLine();
        }

        AiInspector.I.OnConfigurationChanged(numberField);
    }

    public void OnFactToInputChanged(NumberInputField numberField)
    {
        if (numberField.IsValid)
        {
            Dto.InputMax = numberField.GetValue();
            RedrawGraphLine();
        }

        AiInspector.I.OnConfigurationChanged(numberField);
    }

    public void OnSlopeInputChanged(NumberInputField numberField)
    {
        if (numberField.IsValid)
        {
            Dto.Slope = numberField.GetValue();
            RedrawGraphLine();
        }

        AiInspector.I.OnConfigurationChanged(numberField);
    }

    public void OnExpInputChanged(NumberInputField numberField)
    {
        if (numberField.IsValid)
        {
            Dto.Exp = numberField.GetValue();
            RedrawGraphLine();
        }

        AiInspector.I.OnConfigurationChanged(numberField);
    }

    public void OnXShiftInputChanged(NumberInputField numberField)
    {
        if (numberField.IsValid)
        {
            Dto.XShift = numberField.GetValue();
            RedrawGraphLine();
        }

        AiInspector.I.OnConfigurationChanged(numberField);
    }

    public void OnYShiftInputChanged(NumberInputField numberField)
    {
        if (numberField.IsValid)
        {
            Dto.YShift = numberField.GetValue();
            RedrawGraphLine();
        }

        AiInspector.I.OnConfigurationChanged(numberField);
    }

    public void OnToggle()
    {
        graphLine.enabled = toggle.isOn;
        graphBall.enabled = toggle.isOn;
    }

    public void OnChoiceHeightChanged()
    {
        RedrawGraphLine();
        UpdateGraphBallPosition();
    }

    public void OnReorderUpButtonPressed()
    {
        int index = transform.GetSiblingIndex();
        if (index == 0)
        {
            return;
        }

        ConsiderationDto other = choice.Dto.Considerations[index - 1];
        choice.Dto.Considerations[index - 1] = Dto;
        choice.Dto.Considerations[index] = other;
        transform.SetSiblingIndex(index - 1);
        choice.ChangeConsiderationIndex(this, index, index - 1);
    }

    public void OnReorderDownButtonPressed()
    {
        int index = transform.GetSiblingIndex();
        if (index == transform.parent.childCount - 1)
        {
            return;
        }

        ConsiderationDto other = choice.Dto.Considerations[index + 1];
        choice.Dto.Considerations[index + 1] = Dto;
        choice.Dto.Considerations[index] = other;
        transform.SetSiblingIndex(index + 1);
        choice.ChangeConsiderationIndex(this, index, index + 1);
    }

    #endregion

    private void Awake()
    {
        layoutElement = GetComponent<LayoutElement>();
        SetHeight(collapsedHeight);
    }

    public void Setup(ConsiderationDto dto, ChoiceUi choice, Color color, RectTransform graph, UILineRenderer graphLine)
    {
        Dto = dto;
        this.choice = choice;
        this.graph = graph;
        this.graphLine = graphLine;

        graphBall = Instantiate(graphBallPrefab, graph).GetComponent<Image>();
        SetColour(color);

        label.text = dto.FactType;
        score.text = ".0";

        // force awake to be called
        inputsPanel.gameObject.SetActive(true);
        inputsPanel.gameObject.SetActive(false);

        if (dto.IsMultiTarget)
        {
            isMultiTarget = true;
            for (int i = 0; i < reorderButtons.Length; i++)
            {
                Vector2 size = reorderButtons[i].sizeDelta;
                size.y = MultiChoiceReorderButtonsHeight;
                reorderButtons[i].sizeDelta = size;
            }
        }
        
        UpdateValuesFromDto();
    }

    public void SetRecordedDataKeys(int choiceTargets, int recordedDataKey)
    {
        maxTargets = MultiTargetUtil.IsMultiTargetFact(Dto.FactTypeEnum) ? math.min(choiceTargets, scoreLabelMulti.Length) : 1;
        this.recordedDataKey = recordedDataKey;
    }

    public void UpdateValuesFromDto()
    {
        factFromInput.text = Dto.InputMin == 0 ? "0" : Dto.InputMin.ToString(AiInspector.InputFormat);
        factToInput.text = Dto.InputMax == 0 ? "0" : Dto.InputMax.ToString(AiInspector.InputFormat);
        slopeInput.text = Dto.Slope == 0 ? "0" : Dto.Slope.ToString(AiInspector.InputFormat);
        expInput.text = Dto.Exp == 0 ? "0" :  Dto.Exp.ToString(AiInspector.InputFormat);
        xInput.text = Dto.XShift == 0 ? "0" : Dto.XShift.ToString(AiInspector.InputFormat);
        yInput.text = Dto.YShift == 0 ? "0" : Dto.YShift.ToString(AiInspector.InputFormat);
    }

    public void Remove(bool removeFromChoice)
    {
        bool isExpanded = inputsPanel.gameObject.activeSelf;
        Destroy(graphLine.gameObject);
        Destroy(graphBall.gameObject);
        if (removeFromChoice)
        {
            choice.ChangeHeight(isExpanded ? -expandedHeight : -collapsedHeight);
            choice.RemoveConsideration(this);
        }

        Destroy(gameObject);
    }

    public void SetColour(Color color)
    {
        graphBall.color = color;
        toggleForeground.color = color;
        graphLine.color = color;
    }

    private void Update()
    {
        if (!AiInspector.RecordingPaused)
        {
            bool noTargetsLeft = false;
            for (int i = 0; i < maxTargets; i++)
            {
                float score = 0f;
                scoreLabelMulti[i].enabled = true;
                if (noTargetsLeft || !AiDataSys.NativeData.RecordedScores.TryGetValue(recordedDataKey + i * 10, out score))
                {
                    noTargetsLeft = true; // just to shortcut the rest of the loop
                    score = 0f;
                    scoreLabelMulti[i].enabled = false;
                }

                scoreLabelMulti[i].text = score.ToString(AiInspector.ScoreFormat);
            }

            for (int i = maxTargets; i < 8; i++)
            {
                scoreLabelMulti[i].enabled = false;
            }

            AiDataSys.NativeData.RecordedScores.TryGetValue(recordedDataKey + choice.BestScoreTargetIndex * 10, out bestScoreValue);
            AiDataSys.NativeData.RecordedScores.TryGetValue(recordedDataKey + choice.BestScoreTargetIndex * 10 + 1, out bestFactInputValue);
            score.text = bestScoreValue.ToString(AiInspector.ScoreFormat);
        }
        
        UpdateGraphBallPosition();
        if (!GInput.AnyKeyActivity)
        {
            return;
        }

        if (GInput.GetMouseButtonUpQuick(0) && GInput.HitObjUiTop == label.gameObject)
        {
            expanded = !inputsPanel.gameObject.activeSelf;
            inputsPanel.gameObject.SetActive(expanded);
            multiScorePanel.SetActive(isMultiTarget && expanded);
            reorderPanel.gameObject.SetActive(expanded);
            SetHeight(expanded ? ExpandedHeight : collapsedHeight);
            choice.ChangeHeight((ExpandedHeight - collapsedHeight) * (expanded ? 1f : -1f));
        }
    }

    private void SetHeight(float height)
    {
        layoutElement.preferredHeight = height;
    }

    private void RedrawGraphLine()
    {
        const int pointCount = 51;
        var consideration = Dto.ToConsideration();

        float w = math.max(graph.rect.width, 0f);
        float h = math.max(graph.rect.height, 0f);

        Vector2[] points = new Vector2[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            float xNorm = 1f / (pointCount - 1) * i;
            float x = xNorm * w;
            points[i] = new Vector2(x, consideration.Evaluate(xNorm) * h);
        }

        graphLine.Points = points;
    }

    private void UpdateGraphBallPosition()
    {
        float w = graph.rect.width;
        float h = graph.rect.height;

        Vector2 pos = graphBall.rectTransform.anchoredPosition;
        pos.x = bestFactInputValue * w;
        pos.y = bestScoreValue * h;
        graphBall.rectTransform.anchoredPosition = pos;
    }
}