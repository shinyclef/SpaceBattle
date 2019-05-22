using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsiderationUi : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label = default;
    [SerializeField] private TextMeshProUGUI score = default;
    [SerializeField] private RectTransform inputsPanel = default;
    [SerializeField] private TextMeshProUGUI factType = default;
    [SerializeField] private TMP_InputField factFromInput = default;
    [SerializeField] private TMP_InputField factToInput = default;
    [SerializeField] private TMP_InputField slopeInput = default;
    [SerializeField] private TMP_InputField expInput = default;
    [SerializeField] private TMP_InputField xInput = default;
    [SerializeField] private TMP_InputField yInput = default;
    [SerializeField] private float layoutElementExpandedHeight = default;

    private LayoutElement layoutElement;
    private float layoutElementCollapsedHeight;

    private void Awake()
    {
        layoutElement = GetComponent<LayoutElement>();
        layoutElementCollapsedHeight = layoutElement.minHeight;
    }

    public void Setup(ConsiderationDto con)
    {
        
    }

    private void Update()
    {
        if (!GInput.AnyKeyActivity)
        {
            return;
        }

        if (GInput.GetMouseButtonUpQuick(0) && GInput.HitObjUiTop == label.gameObject)
        {
            bool expand = !inputsPanel.gameObject.activeSelf;
            inputsPanel.gameObject.SetActive(expand);
            SetHeight(expand ? layoutElementExpandedHeight : layoutElementCollapsedHeight);
        }
    }

    private void SetHeight(float height)
    {
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;
    }
}