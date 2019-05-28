using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class ConsiderationUi : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label = default;
    [SerializeField] private TextMeshProUGUI score = default;
    [SerializeField] private Toggle toggle = default;
    [SerializeField] private Image toggleForeground = default;
    [SerializeField] private RectTransform reorderPanel = default;
    [SerializeField] private RectTransform inputsPanel = default;
    [SerializeField] private TMP_InputField factFromInput = default;
    [SerializeField] private TMP_InputField factToInput = default;
    [SerializeField] private TMP_InputField slopeInput = default;
    [SerializeField] private TMP_InputField expInput = default;
    [SerializeField] private TMP_InputField xInput = default;
    [SerializeField] private TMP_InputField yInput = default;
    [SerializeField] private float layoutElementExpandedHeight = default;
    [SerializeField] private GameObject graphBallPrefab = default;

    private ChoiceUi choice;
    private Image graphBall;
    private LayoutElement layoutElement;
    private RectTransform graph;
    private UILineRenderer graphLine;
    private float layoutElementCollapsedHeight;
    private int recordedDataIndex;
    private float factInputValue;
    private float scoreValue;

    public ConsiderationDto Dto { get; private set; }

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
        layoutElementCollapsedHeight = layoutElement.preferredHeight;
    }

    public void Setup(ConsiderationDto dto, ChoiceUi choice, Color color, RectTransform graph, UILineRenderer graphLine)
    {
        Dto = dto;
        this.choice = choice;
        this.graph = graph;
        this.graphLine = graphLine;

        graphBall = Instantiate(graphBallPrefab, graph).GetComponent<Image>();
        SetColour(color);

        label.text = dto.FactType.ToString();
        score.text = ".0";

        // force awake to be called
        inputsPanel.gameObject.SetActive(true);
        inputsPanel.gameObject.SetActive(false);

        UpdateValuesFromDto();
    }

    public void SetRecordedDataIndecies(int recordedDataIndex)
    {
        this.recordedDataIndex = recordedDataIndex;
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
            choice.ChangeHeight(isExpanded ? -layoutElementExpandedHeight : -layoutElementCollapsedHeight);
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
            scoreValue = AiDataSys.NativeData.RecordedScores[recordedDataIndex];
            score.text = scoreValue.ToString(AiInspector.ScoreFormat);
            factInputValue = AiDataSys.NativeData.RecordedScores[recordedDataIndex + 1];
        }
        
        UpdateGraphBallPosition();
        if (!GInput.AnyKeyActivity)
        {
            return;
        }

        if (GInput.GetMouseButtonUpQuick(0) && GInput.HitObjUiTop == label.gameObject)
        {
            bool expand = !inputsPanel.gameObject.activeSelf;
            inputsPanel.gameObject.SetActive(expand);
            reorderPanel.gameObject.SetActive(expand);
            SetHeight(expand ? layoutElementExpandedHeight : layoutElementCollapsedHeight);
            choice.ChangeHeight((layoutElementExpandedHeight - layoutElementCollapsedHeight) * (expand ? 1f : -1f));
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
        pos.x = factInputValue * w;
        pos.y = scoreValue * h;
        graphBall.rectTransform.anchoredPosition = pos;
    }
}