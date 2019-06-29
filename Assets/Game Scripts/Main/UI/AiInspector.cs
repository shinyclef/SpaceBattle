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
    [SerializeField] private Button newChoiceButton = default;
    [SerializeField] private Transform choicesList = default;
    [SerializeField] private GameObject choicePrefab = default;

    private static DecisionType recordedDecision;

    DecisionDto decisionDto;
    private PopupMenu popupMenu;
    private List<ChoiceUi> choices;
    private int invalidFieldsCount;
    private List<string> options;
    private bool populating;

    public static AiInspector I { get; private set; }
    public static bool RecordingPaused { get; private set; }
    public static DecisionType RecordedDecision { get { return (RecordingPaused || I == null || !I.gameObject.activeSelf) ? DecisionType.None : recordedDecision; } }
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

        if (!populating)
        {
            SetDirtyState(false);
            if (invalidFieldsCount == 0)
            {
                SetDirtyState(AiDataSys.I.DataIsDirty);
                AiDataSys.I.UpdateAiData();
            }
            else
            {
                SetDirtyState(true);
            }
        }
    }

    public void OnStructureChanged()
    {
        SetDirtyState(AiDataSys.I.DataIsDirty);
        RecordingPaused = true;
    }

    public void OnMenuButtonPressed()
    {
        if (popupMenu.PopupObject.gameObject.activeSelf)
        {
            popupMenu.Deactivate();
        }
        else
        {
            popupMenu.Activate(3);
        }
    }

    public void OnCloseButtonPressed()
    {
        gameObject.SetActive(false);
    }

    public void OnSaveButtonPressed()
    {
        AiDataSys.I.SaveAiData();
        SetDirtyState(AiDataSys.I.DataIsDirty);
    }

    public void OnRevertButtonPressed()
    {
        AiDataSys.I.RevertAiData();
    }

    public void OnReloadButtonPressed()
    {
        AiDataSys.I.ReloadAiData();
    }

    public void OnNewChoiceButtonPressed()
    {
        Logger.Log("New Choice");
    }

    private void OnAiLoaded()
    {
        PopulateDecisionDropDown();
    }

    private void OnAiNativeArraysGenerated()
    {
        SetRecordedDataKeys();
        RecordingPaused = false;
    }
    

    #endregion

    private void Awake()
    {
        I = this;
        popupMenu = GetComponent<PopupMenu>();
        populating = false;
        choices = new List<ChoiceUi>();
        Localize();
    }

    private void Start()
    {
        Messenger.Global.AddListener(Msg.AiLoadedFromDisk, OnAiLoaded);
        Messenger.Global.AddListener(Msg.AiNativeArrayssGenerated, OnAiNativeArraysGenerated);
        Messenger.Global.AddListener(Msg.AiRevertedUnsavedChanges, UpdateValuesFromDto);
        OnAiLoaded();
    }

    private void OnEnable()
    {
        popupMenu.Deactivate();
    }

    private void Update()
    {
        if (choices.Count == 0)
        {
            return;
        }

        float max = choices[0].BestScore;
        ChoiceUi bestChoice = choices[0];
        for (int i = 1; i < choices.Count; i++)
        {
            if (choices[i].BestScore > max)
            {
                max = choices[i].BestScore;
                bestChoice = choices[i];
            }
        }

        if (bestChoice.BestScore > 0)
        {
            bestChoice.SetIsSelected(true);
        }
    }

    public void UpdateValuesFromDto()
    {
        for (int i = 0; i < choices.Count; i++)
        {
            choices[i].UpdateValuesFromDto();
        }
    }

    public void ChangeChoiceIndex(ChoiceUi choiceUi, int oldIndex, int newIndex)
    {
        choices.RemoveAt(oldIndex);
        choices.Insert(newIndex, choiceUi);
        AiDataSys.I.UpdateAiData();
        OnStructureChanged();
    }

    public void RemoveChoice(ChoiceUi choice)
    {
        var newArr = new ChoiceDto[decisionDto.Choices.Length - 1];
        int j = 0;
        for (int i = 0; i < decisionDto.Choices.Length; i++)
        {
            if (decisionDto.Choices[i] != choice.Dto)
            {
                newArr[j] = decisionDto.Choices[i];
                j++;
            }
        }

        decisionDto.Choices = newArr;
        choices.Remove(choice);
        AiDataSys.I.UpdateAiData();
        OnStructureChanged();
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
            options.Add(dto.Decisions[i].DecisionType);
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

        newChoiceButton.interactable = false;
    }

    private void PopulateChoices()
    {
        populating = true;
        ClearChoices();
        decisionDto = null;
        for (int i = 0; i < AiDataSys.Data.Decisions.Length; i++)
        {
            if (AiDataSys.Data.Decisions[i].DecisionType == SelectedDecision)
            {
                decisionDto = AiDataSys.Data.Decisions[i];
                break;
            }
        }

        if (decisionDto == null)
        {
            Logger.LogWarning("Can't populate AI Inspector decision list. No decisions were found.");
            return;
        }

        for (int i = 0; i < decisionDto.Choices.Length; i++)
        {
            ChoiceUi c = Instantiate(choicePrefab, choicesList).GetComponent<ChoiceUi>();
            choices.Add(c);
            c.Setup(decisionDto.Choices[i], decisionDto);
        }

        SetRecordedDataKeys();
        populating = false;
        SetDirtyState(AiDataSys.I.DataIsDirty);
        newChoiceButton.interactable = true;
    }

    private void SetRecordedDataKeys()
    {
        for (int i = 0; i < choices.Count; i++)
        {
            ChoiceUi c = choices[i];
            c.SetRecordedDataKeys((i + 1) * 100000);
        }
    }
}