using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChoiceUi : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI choiceLabel = default;
    [SerializeField] private TextMeshProUGUI totalScoreLabel = default;
    [SerializeField] private TMP_InputField weightInput = default;
    [SerializeField] private TMP_InputField momentumInput = default;
    [SerializeField] private Transform considerationList = default;
    [SerializeField] private GameObject considerationPrefab = default;

    private List<ConsiderationUi> Considerations;

    public void Setup(ChoiceDto dto)
    {
        choiceLabel.text = dto.ChoiceType.ToString();
        totalScoreLabel.text = ".000";

        PopulateConsiderations(dto);
    }

    

    private void ClearConsiderations()
    {
        Considerations.Clear();
        for (int i = considerationList.childCount - 1; i >= 0; i--)
        {
            Destroy(considerationList.GetChild(i).gameObject);
        }
    }


    private void PopulateConsiderations(ChoiceDto dto)
    {
        for (int i = 0; i < dto.Considerations.Length; i++)
        {
            ConsiderationUi con = Instantiate(considerationPrefab, considerationList).GetComponent<ConsiderationUi>();
            con.Setup(dto.Considerations[i]);
        }
    }

    private void AddConsideration()
    {

    }
}