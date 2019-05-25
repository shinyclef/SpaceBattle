using UnityEngine;

public class FlexibleDraggableObject : MonoBehaviour
{
    [SerializeField] private RectTransform target = default;
    [SerializeField] private Vector2 minimumDimmensions = new Vector2(50, 50);
    [SerializeField] private Vector2 maximumDimmensions = new Vector2(800, 800);

    public RectTransform Target { get { return target; } }
    public Vector2 MinSize { get { return minimumDimmensions; } }
    public Vector2 MaxSize { get { return maximumDimmensions; } }
}