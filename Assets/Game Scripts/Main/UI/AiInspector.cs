using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;

public class AiInspector : MonoBehaviour
{
    public const string ScoreFormat = "#.000";

    [SerializeField] private TMP_Dropdown decisionDropdown = default;
    [SerializeField] private Transform choicesList = default;
    [SerializeField] private GameObject choicePrefab = default;

    private static AiInspector I;
    private static DecisionType recordedDecision;
    private List<string> options;

    public static DecisionType RecordedDecision { get { return (I == null || !I.gameObject.activeSelf) ? DecisionType.None : recordedDecision; } }

    private string SelectedDecision { get { return options[decisionDropdown.value]; } }

    private DecisionType SelectedDecisionType { get { return (DecisionType)Enum.Parse(typeof(DecisionType), SelectedDecision); } }

    #region Events

    public void OnCloseButtonPressed()
    {
        gameObject.SetActive(false);
    }

    private void OnAiLoaded()
    {
        PopulateDecisionDropDown();
    }

    public void OnDecisionSelectionChanged()
    {
        PopulateChoices();
        recordedDecision = SelectedDecisionType;
    }

    #endregion

    private void Start()
    {
        I = this;
        Messenger.Global.AddListener(Msg.AiLoaded, OnAiLoaded);
        OnAiLoaded();
    }

    private void PopulateDecisionDropDown()
    {
        decisionDropdown.ClearOptions();

        UtilityAiDto dto = AiDataSys.Data;
        options = new List<string>();
        for (int i = 0; i < dto.Decisions.Length; i++)
        {
            options.Add(dto.Decisions[i].DecisionType.ToString());
        }

        if (options.Count > 0)
        {
            decisionDropdown.AddOptions(options);
            OnDecisionSelectionChanged();
        }
    }

    private void ClearChoices()
    {
        for (int i = choicesList.childCount - 1; i >= 0; i--)
        {
            Destroy(choicesList.GetChild(i).gameObject);
        }
    }

    private void PopulateChoices()
    {
        ClearChoices();
        DecisionDto dec = null;
        for (int i = 0; i < AiDataSys.Data.Decisions.Length; i++)
        {
            if (AiDataSys.Data.Decisions[i].DecisionType.ToString() == SelectedDecision)
            {
                dec = AiDataSys.Data.Decisions[i];
                break;
            }
        }

        if (dec == null)
        {
            Logger.LogWarning("Can't populate AI Inspector decision list. No decisions were found.");
            return;
        }

        int recordedDataIndex = -1;
        for (int i = 0; i < dec.Choices.Length; i++)
        {
            ChoiceUi c = Instantiate(choicePrefab, choicesList).GetComponent<ChoiceUi>();
            recordedDataIndex = recordedDataIndex + 1 + dec.Choices[i].Considerations.Length;
            c.Setup(dec.Choices[i], recordedDataIndex);
        }
    }
}