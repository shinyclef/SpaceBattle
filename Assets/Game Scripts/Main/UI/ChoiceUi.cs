using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChoiceUi : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label = default;
    [SerializeField] private Transform considerationList = default;
    [SerializeField] private TextMeshProUGUI totalScore = default;
    [SerializeField] private GameObject considerationPrefab = default;

    private List<ConsiderationUi> Considerations;

    public void Setup(ChoiceDto dto)
    {
        label.text = dto.ChoiceType.ToString();
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