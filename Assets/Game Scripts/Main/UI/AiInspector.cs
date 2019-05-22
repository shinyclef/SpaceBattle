using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AiInspector : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown decisionDropdown = default;
    [SerializeField] private Transform choicesList = default;
    [SerializeField] private GameObject choicePrefab = default;

    #region Events

    private void Awake()
    {
        Messenger.Global.AddListener(Msg.AiLoaded, OnAiLoaded);
    }

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
    }

    #endregion

    private void PopulateDecisionDropDown()
    {
        decisionDropdown.ClearOptions();

        UtilityAiDto dto = AiLoadSys.Data;
        var options = new List<string>();
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
        for (int i = 0; i < AiLoadSys.Data.Decisions.Length; i++)
        {
            if (AiLoadSys.Data.Decisions[i].DecisionType.ToString() == decisionDropdown.itemText.text)
            {
                dec = AiLoadSys.Data.Decisions[i];
                break;
            }
        }

        if (dec == null)
        {
            Logger.LogWarning("Can't populate AI Inspector decision list. No decisions were found.");
            return;
        }

        for (int i = 0; i < dec.Choices.Length; i++)
        {
            ChoiceUi c = Instantiate(choicePrefab, choicesList).GetComponent<ChoiceUi>();
            c.Setup(dec.Choices[i]);
        }
    }
}