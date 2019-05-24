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
    [SerializeField] private RectTransform inputsPanel = default;
    [SerializeField] private TMP_InputField factFromInput = default;
    [SerializeField] private TMP_InputField factToInput = default;
    [SerializeField] private TMP_InputField slopeInput = default;
    [SerializeField] private TMP_InputField expInput = default;
    [SerializeField] private TMP_InputField xInput = default;
    [SerializeField] private TMP_InputField yInput = default;
    [SerializeField] private float layoutElementExpandedHeight = default;
    [SerializeField] private GameObject graphBallPrefab = default;

    private Image graphBall;
    private ChoiceUi choice;
    private LayoutElement layoutElement;
    private RectTransform graph;
    private UILineRenderer graphLine;
    private float layoutElementCollapsedHeight;
    private int recordedDataIndex;
    private float factInputValue;
    private float scoreValue;
    private Consideration consideration;

    private void Awake()
    {
        layoutElement = GetComponent<LayoutElement>();
        layoutElementCollapsedHeight = layoutElement.preferredHeight;
    }

    public void Setup(ConsiderationDto dto, ChoiceUi choice, Color color, int recordedDataIndex, RectTransform graph, UILineRenderer graphLine)
    {
        this.choice = choice;
        this.recordedDataIndex = recordedDataIndex;
        this.graph = graph;
        this.graphLine = graphLine;

        graphBall = Instantiate(graphBallPrefab, graph).GetComponent<Image>();
        graphBall.color = color;

        toggleForeground.color = color;
        graphLine.color = color;

        label.text = dto.FactType.ToString();
        score.text = ".0";
        factFromInput.SetTextWithoutNotify(dto.InputMin.ToString(AiInspector.InputFormat));
        factToInput.SetTextWithoutNotify(dto.InputMax.ToString(AiInspector.InputFormat));
        slopeInput.SetTextWithoutNotify(dto.Slope.ToString(AiInspector.InputFormat));
        expInput.SetTextWithoutNotify(dto.Exp.ToString(AiInspector.InputFormat));
        xInput.SetTextWithoutNotify(dto.XShift.ToString(AiInspector.InputFormat));
        yInput.SetTextWithoutNotify(dto.YShift.ToString(AiInspector.InputFormat));
        consideration = dto.ToConsideration();
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

    private void Update()
    {
        scoreValue = AiDataSys.NativeData.RecordedScores[recordedDataIndex];
        score.text = scoreValue.ToString(AiInspector.ScoreFormat);
        factInputValue = AiDataSys.NativeData.RecordedScores[recordedDataIndex + 1];
        UpdateGraphBallPosition();

        if (!GInput.AnyKeyActivity)
        {
            return;
        }

        if (GInput.GetMouseButtonUpQuick(0) && GInput.HitObjUiTop == label.gameObject)
        {
            bool expand = !inputsPanel.gameObject.activeSelf;
            inputsPanel.gameObject.SetActive(expand);
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