using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class ChoiceUi : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI choiceLabel = default;
    [SerializeField] private TextMeshProUGUI totalScoreLabel = default;
    [SerializeField] private TMP_InputField weightInput = default;
    [SerializeField] private TMP_InputField momentumInput = default;
    [SerializeField] private RectTransform considerationList = default;
    [SerializeField] private RectTransform choiceInfoPanel = default;
    [SerializeField] private RectTransform inputsPanel = default;
    [SerializeField] private RectTransform graph = default;
    [SerializeField] private float choiceInfoExpandedHeight = default;
    [SerializeField] private GameObject considerationPrefab = default;
    [SerializeField] private GameObject graphLinePrefab = default;

    private ChoiceDto dto;
    private List<ConsiderationUi> considerations;
    private LayoutElement layoutElement;
    private float considerationCollapsedHeight;
    private float choiceInfoCollapsedHeight;
    private bool choiceInfoExpanded;
    private int recordedDataIndex;
    private bool heightChangeEventScheduled;

    private float ChoiceInfoPanelHeight { get { return choiceInfoExpanded ? choiceInfoExpandedHeight : choiceInfoCollapsedHeight; } }

    #region Events

    public void OnWeightInputChanged(NumberInputField numberField)
    {
        if (numberField.IsValid)
        {
            dto.Weight = numberField.GetValue();
        }
        
        AiInspector.I.OnConfigurationChanged(numberField);
    }

    public void OnMomentumInputChanged(NumberInputField numberField)
    {
        if (numberField.IsValid)
        {
            dto.Momentum = numberField.GetValue();
        }

        AiInspector.I.OnConfigurationChanged(numberField);
    }

    #endregion

    private void Awake()
    {
        considerations = new List<ConsiderationUi>();
        layoutElement = GetComponent<LayoutElement>();
        considerationCollapsedHeight = considerationPrefab.GetComponent<LayoutElement>().preferredHeight;
        choiceInfoCollapsedHeight = choiceInfoPanel.sizeDelta.y;
        choiceInfoExpanded = false;
        heightChangeEventScheduled = false;
    }

    public void Setup(ChoiceDto dto, int recordedDataIndex)
    {
        this.dto = dto;
        this.recordedDataIndex = recordedDataIndex;
        choiceLabel.text = dto.ChoiceType.ToString();
        totalScoreLabel.text = ".0";

        inputsPanel.gameObject.SetActive(true); // force awake to be called
        inputsPanel.gameObject.SetActive(false);

        UpdateValuesFromDto();
        PopulateConsiderations();
    }

    public void UpdateValuesFromDto()
    {
        weightInput.text = dto.Weight == 0 ? "0" : dto.Weight.ToString(AiInspector.InputFormat);
        momentumInput.text = dto.Momentum == 0 ? "0" : dto.Momentum.ToString(AiInspector.InputFormat);
        for (int i = 0; i < considerations.Count; i++)
        {
            considerations[i].UpdateValuesFromDto();
        }
    }

    private void Update()
    {
        totalScoreLabel.text = AiDataSys.NativeData.RecordedScores[recordedDataIndex].ToString(AiInspector.ScoreFormat);
        if (!GInput.AnyKeyActivity)
        {
            return;
        }

        if (GInput.GetMouseButtonUpQuick(0) && GInput.HitObjUiTop == choiceLabel.gameObject)
        {
            choiceInfoExpanded = !choiceInfoExpanded;
            inputsPanel.gameObject.SetActive(choiceInfoExpanded);

            Vector2 size = choiceInfoPanel.sizeDelta;
            size.y = ChoiceInfoPanelHeight;
            choiceInfoPanel.sizeDelta = size;

            Vector2 anchoredPos = considerationList.anchoredPosition;
            anchoredPos.y = -ChoiceInfoPanelHeight;
            considerationList.anchoredPosition = anchoredPos;

            ChangeHeight((choiceInfoExpandedHeight - choiceInfoCollapsedHeight) * (choiceInfoExpanded ? 1f : -1f));
        }
    }

    public void ChangeHeight(float delta)
    {
        layoutElement.preferredHeight += delta;
        OnChoiceHeightChanged();
    }

    private void SetHeight(float height)
    {
        layoutElement.preferredHeight = height;
        OnChoiceHeightChanged();
    }

    private void OnChoiceHeightChanged()
    {
        if (heightChangeEventScheduled)
        {
            return;
        }

        heightChangeEventScheduled = true;
        Scheduler.InvokeAfterOneFrame(() =>
        {
            for (int i = 0; i < considerations.Count; i++)
            {
                considerations[i].OnChoiceHeightChanged();
            }

            heightChangeEventScheduled = false;
        });
    }

    private void PopulateConsiderations()
    {
        considerations.Clear();
        for (int i = 0; i < dto.Considerations.Length; i++)
        {
            ConsiderationUi con = Instantiate(considerationPrefab, considerationList).GetComponent<ConsiderationUi>();
            UILineRenderer line = Instantiate(graphLinePrefab, graph).GetComponent<UILineRenderer>();
            con.Setup(dto.Considerations[i], this, DistinctColourList.GetColour(i), recordedDataIndex - (dto.Considerations.Length * 2) + (i * 2), graph, line);
            considerations.Add(con);
        }

        SetHeight(-considerationList.anchoredPosition.y + considerationCollapsedHeight * considerations.Count);
    }
}