using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class ChoiceUi : MonoBehaviour
{
    private const float MultiChoiceReorderButtonsHeight = 37f;

    [SerializeField] private TextMeshProUGUI choiceLabel = default;
    [SerializeField] private TextMeshProUGUI totalScoreLabel = default;
    [SerializeField] private TextMeshProUGUI[] totalScoreLabelMulti = default;
    [SerializeField] private Image selectedIcon = default;
    [SerializeField] private TMP_InputField weightInput = default;
    [SerializeField] private TMP_InputField momentumInput = default;
    [SerializeField] private RectTransform considerationList = default;
    [SerializeField] private RectTransform choiceInfoPanel = default;
    [SerializeField] private RectTransform reorderPanel = default;
    [SerializeField] private RectTransform[] reorderButtons = default;
    [SerializeField] private GameObject multiScorePanel = default;
    [SerializeField] private RectTransform inputsPanel = default;
    [SerializeField] private RectTransform graph = default;
    [SerializeField] private float choiceInfoCollapsedHeight = default;
    [SerializeField] private float choiceInfoExpandedHeight = default;
    [SerializeField] private float choiceInfoExpandedMultiHeight = default;
    [SerializeField] private GameObject considerationPrefab = default;
    [SerializeField] private GameObject graphLinePrefab = default;

    private bool isMultiTarget;
    private DecisionDto decision;
    private List<ConsiderationUi> considerations;
    private LayoutElement layoutElement;
    private float considerationCollapsedHeight;
    private bool choiceInfoExpanded;
    private int recordedDataIndex;
    private bool heightChangeEventScheduled;

    public ChoiceDto Dto { get; private set; }

    public int ConsiderationsCount => Dto.Considerations.Length;
    public float Score { get; private set; }

    private float ChoiceInfoPanelCurrentHeight { get { return choiceInfoExpanded ? ChoiceInfoPanelExpandedHeight : choiceInfoCollapsedHeight; } }
    private float ChoiceInfoPanelExpandedHeight { get { return isMultiTarget ? choiceInfoExpandedMultiHeight : choiceInfoExpandedHeight; } }

    #region Events

    public void OnWeightInputChanged(NumberInputField numberField)
    {
        if (numberField.IsValid)
        {
            Dto.Weight = numberField.GetValue();
        }
        
        AiInspector.I.OnConfigurationChanged(numberField);
    }

    public void OnMomentumInputChanged(NumberInputField numberField)
    {
        if (numberField.IsValid)
        {
            Dto.Momentum = numberField.GetValue();
        }

        AiInspector.I.OnConfigurationChanged(numberField);
    }

    public void OnRemoveButtonPressed()
    {
        Remove();
    }
    
    public void OnNewConsiderationButtonPressed()
    {
        AddConsideration();
    }

    public void OnReorderUpButtonPressed()
    {
        int index = transform.GetSiblingIndex();
        if (index == 0)
        {
            return;
        }

        ChoiceDto other = decision.Choices[index - 1];
        decision.Choices[index - 1] = Dto;
        decision.Choices[index] = other;
        transform.SetSiblingIndex(index - 1);
        AiInspector.I.ChangeChoiceIndex(this, index, index - 1);
    }

    public void OnReorderDownButtonPressed()
    {
        int index = transform.GetSiblingIndex();
        if (index == transform.parent.childCount - 1)
        {
            return;
        }

        ChoiceDto other = decision.Choices[index + 1];
        decision.Choices[index + 1] = Dto;
        decision.Choices[index] = other;
        transform.SetSiblingIndex(index + 1);
        AiInspector.I.ChangeChoiceIndex(this, index, index + 1);
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
    
    #endregion

    private void Awake()
    {
        considerations = new List<ConsiderationUi>();
        layoutElement = GetComponent<LayoutElement>();
        considerationCollapsedHeight = considerationPrefab.GetComponent<LayoutElement>().preferredHeight;

        Vector2 size = choiceInfoPanel.sizeDelta;
        size.y = choiceInfoCollapsedHeight;
        choiceInfoPanel.sizeDelta = size;

        choiceInfoExpanded = false;
        heightChangeEventScheduled = false;
        isMultiTarget = false;
    }

    public void Setup(ChoiceDto dto, DecisionDto decision)
    {
        Dto = dto;
        this.decision = decision;
        choiceLabel.text = dto.ChoiceType;
        totalScoreLabel.text = ".0";

        inputsPanel.gameObject.SetActive(true); // force awake to be called
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
        PopulateConsiderations();
    }

    public void SetRecordedDataIndecies(int recordedDataIndex)
    {
        this.recordedDataIndex = recordedDataIndex;
        for (int i = 0; i < considerations.Count; i++)
        {
            ConsiderationUi con = considerations[i];
            con.SetRecordedDataIndecies(recordedDataIndex - (Dto.Considerations.Length * 2) + (i * 2));
        }
    }

    private void Update()
    {
        SetIsSelected(false); // AiInspector will reselect if this is still the best choice
        if (!AiInspector.RecordingPaused)
        {
            Score = AiDataSys.NativeData.RecordedScores[recordedDataIndex];
        }
        
        totalScoreLabel.text = Score.ToString(AiInspector.ScoreFormat);
        if (!GInput.AnyKeyActivity)
        {
            return;
        }

        if (GInput.GetMouseButtonUpQuick(0) && GInput.HitObjUiTop == choiceLabel.gameObject)
        {
            choiceInfoExpanded = !choiceInfoExpanded;
            inputsPanel.gameObject.SetActive(choiceInfoExpanded);
            multiScorePanel.SetActive(isMultiTarget && choiceInfoExpanded);
            reorderPanel.gameObject.SetActive(choiceInfoExpanded);

            Vector2 size = choiceInfoPanel.sizeDelta;
            size.y = ChoiceInfoPanelCurrentHeight;
            choiceInfoPanel.sizeDelta = size;

            Vector2 anchoredPos = considerationList.anchoredPosition;
            anchoredPos.y = -ChoiceInfoPanelCurrentHeight;
            considerationList.anchoredPosition = anchoredPos;

            ChangeHeight((ChoiceInfoPanelExpandedHeight - choiceInfoCollapsedHeight) * (choiceInfoExpanded ? 1f : -1f));
        }
    }

    private void Remove()
    {
        for (int i = 0; i < considerations.Count; i++)
        {
            considerations[i].Remove(false);
        }

        Destroy(gameObject);
        AiInspector.I.RemoveChoice(this);
    }

    public void ChangeConsiderationIndex(ConsiderationUi considerationUi, int oldIndex, int newIndex)
    {
        considerations.RemoveAt(oldIndex);
        considerations.Insert(newIndex, considerationUi);
        considerations[oldIndex].SetColour(DistinctColourList.GetColour(oldIndex));
        considerations[newIndex].SetColour(DistinctColourList.GetColour(newIndex));
        AiDataSys.I.UpdateAiData();
        AiInspector.I.OnStructureChanged();
    }

    public void RemoveConsideration(ConsiderationUi considerationUi)
    {
        var newArr = new ConsiderationDto[Dto.Considerations.Length - 1];
        int j = 0;
        for (int i = 0; i < Dto.Considerations.Length; i++)
        {
            if (Dto.Considerations[i] != considerationUi.Dto)
            {
                newArr[j] = Dto.Considerations[i];
                j++;
            }
        }

        Dto.Considerations = newArr;
        considerations.Remove(considerationUi);
        AiDataSys.I.UpdateAiData();
        AiInspector.I.OnStructureChanged();
    }

    public void AddConsideration()
    {
        var newArr =  new ConsiderationDto[Dto.Considerations.Length + 1];
        Array.Copy(Dto.Considerations, newArr, Dto.Considerations.Length);
        ConsiderationDto consideration = ConsiderationDto.GetDefault();
        newArr[newArr.Length - 1] = consideration;
        Dto.Considerations = newArr;
        AiDataSys.I.UpdateAiData();
        AiInspector.I.OnStructureChanged();
    }

    public void SetIsSelected(bool selected)
    {
        selectedIcon.enabled = selected;
    }

    public void UpdateValuesFromDto()
    {
        weightInput.text = Dto.Weight == 0 ? "0" : Dto.Weight.ToString(AiInspector.InputFormat);
        momentumInput.text = Dto.Momentum == 0 ? "0" : Dto.Momentum.ToString(AiInspector.InputFormat);
        for (int i = 0; i < considerations.Count; i++)
        {
            considerations[i].UpdateValuesFromDto();
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

    private void PopulateConsiderations()
    {
        considerations.Clear();
        for (int i = 0; i < Dto.Considerations.Length; i++)
        {
            ConsiderationUi con = Instantiate(considerationPrefab, considerationList).GetComponent<ConsiderationUi>();
            UILineRenderer line = Instantiate(graphLinePrefab, graph).GetComponent<UILineRenderer>();
            con.Setup(Dto.Considerations[i], this, DistinctColourList.GetColour(i), graph, line);
            considerations.Add(con);
        }

        SetHeight(-considerationList.anchoredPosition.y + considerationCollapsedHeight * considerations.Count);
    }
}