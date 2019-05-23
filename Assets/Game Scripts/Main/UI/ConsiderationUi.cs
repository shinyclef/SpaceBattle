using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsiderationUi : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label = default;
    [SerializeField] private TextMeshProUGUI score = default;
    [SerializeField] private RectTransform inputsPanel = default;
    [SerializeField] private TMP_InputField factFromInput = default;
    [SerializeField] private TMP_InputField factToInput = default;
    [SerializeField] private TMP_InputField slopeInput = default;
    [SerializeField] private TMP_InputField expInput = default;
    [SerializeField] private TMP_InputField xInput = default;
    [SerializeField] private TMP_InputField yInput = default;
    [SerializeField] private float layoutElementExpandedHeight = default;

    private ChoiceUi choice;
    private LayoutElement layoutElement;
    private float layoutElementCollapsedHeight;
    private int recordedDataIndex;

    private void Awake()
    {
        layoutElement = GetComponent<LayoutElement>();
        layoutElementCollapsedHeight = layoutElement.preferredHeight;
    }

    public void Setup(ConsiderationDto dto, ChoiceUi choice, int recordedDataIndex)
    {
        this.choice = choice;
        this.recordedDataIndex = recordedDataIndex;
        label.text = dto.FactType.ToString();
        score.text = ".0";
        factFromInput.SetTextWithoutNotify(dto.InputMin.ToString(AiInspector.ScoreFormat));
        factToInput.SetTextWithoutNotify(dto.InputMax.ToString(AiInspector.ScoreFormat));
        slopeInput.SetTextWithoutNotify(dto.Slope.ToString(AiInspector.ScoreFormat));
        expInput.SetTextWithoutNotify(dto.Exp.ToString(AiInspector.ScoreFormat));
        xInput.SetTextWithoutNotify(dto.XShift.ToString(AiInspector.ScoreFormat));
        yInput.SetTextWithoutNotify(dto.YShift.ToString(AiInspector.ScoreFormat));
    }

    private void Update()
    {
        score.text = AiDataSys.NativeData.RecordedScores[recordedDataIndex].ToString(AiInspector.ScoreFormat);
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
}