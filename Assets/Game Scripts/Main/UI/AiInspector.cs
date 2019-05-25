using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AiInspector : MonoBehaviour
{
    public const string ScoreFormat = "#.000";
    public const string InputFormat = "#.###";

    [SerializeField] private TextMeshProUGUI headingLabel = default;
    [SerializeField] private TextMeshProUGUI decisionLabel = default;
    [SerializeField] private TMP_Dropdown decisionDropdown = default;
    [SerializeField] private Button saveButton = default;
    [SerializeField] private Button revertButton = default;
    [SerializeField] private Transform choicesList = default;
    [SerializeField] private GameObject choicePrefab = default;

    private static DecisionType recordedDecision;

    private List<ChoiceUi> choices;
    private int invalidFieldsCount;
    private List<string> options;
    private bool populating;

    public static AiInspector I { get; private set; }
    public static DecisionType RecordedDecision { get { return (I == null || !I.gameObject.activeSelf) ? DecisionType.None : recordedDecision; } }
    private string SelectedDecision { get { return options[decisionDropdown.value]; } }
    private DecisionType SelectedDecisionType { get { return (DecisionType)Enum.Parse(typeof(DecisionType), SelectedDecision); } }

    #region Events

    public void OnDecisionSelectionChanged()
    {
        PopulateChoices();
        recordedDecision = SelectedDecisionType;
    }

    public void OnConfigurationChanged(NumberInputField numberField)
    {
        if (numberField.ValidStateChanged)
        {
            invalidFieldsCount += numberField.IsValid ? -1 : 1;
        }

        if (!populating && invalidFieldsCount == 0)
        {
            AiDataSys.I.UpdateAiData();
        }

        SetDirtyState(AiDataSys.I.DataIsDirty);
    }

    public void OnCloseButtonPressed()
    {
        gameObject.SetActive(false);
    }

    public void OnSaveButtonPressed()
    {
        AiDataSys.I.SaveAiData();
    }

    public void OnRevertButtonPressed()
    {
        AiDataSys.I.RevertAiData();
    }

    public void OnReloadButtonPressed()
    {
        AiDataSys.I.ReloadAiData();
    }

    private void OnAiLoaded()
    {
        PopulateDecisionDropDown();
    }

    #endregion

    private void Awake()
    {
        I = this;
        populating = false;
        choices = new List<ChoiceUi>();
        Localize();
    }

    private void Start()
    {
        Messenger.Global.AddListener(Msg.AiLoadedFromDisk, OnAiLoaded);
        Messenger.Global.AddListener(Msg.AiRevertedUnsavedChanges, UpdateValuesFromDto);
        OnAiLoaded();
    }

    public void UpdateValuesFromDto()
    {
        for (int i = 0; i < choices.Count; i++)
        {
            choices[i].UpdateValuesFromDto();
        }
    }

    private void Localize()
    {
        headingLabel.text = Localizer.Strings.AiInspector.Heading;
        decisionLabel.text = Localizer.Strings.AiInspector.DecisionLabel;
    }

    private void SetDirtyState(bool dirty)
    {
        saveButton.interactable = dirty && invalidFieldsCount == 0;
        revertButton.interactable = dirty;
    }

    private void PopulateDecisionDropDown()
    {
        decisionDropdown.ClearOptions();

        UtilityAiDto dto = AiDataSys.Data;
        if (dto == null)
        {
            return;
        }

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
        invalidFieldsCount = 0;
        choices.Clear();
        for (int i = choicesList.childCount - 1; i >= 0; i--)
        {
            Destroy(choicesList.GetChild(i).gameObject);
        }
    }

    private void PopulateChoices()
    {
        populating = true;
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
            choices.Add(c);
            recordedDataIndex = recordedDataIndex + 1 + (dec.Choices[i].Considerations.Length * 2);
            c.Setup(dec.Choices[i], recordedDataIndex);
        }

        populating = false;
    }
}