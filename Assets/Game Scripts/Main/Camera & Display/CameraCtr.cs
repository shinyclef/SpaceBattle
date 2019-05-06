using UnityEngine;

public class CameraCtr : MonoBehaviour
{
    [SerializeField] private float panSpeed = 50f;
    [SerializeField] private float zoomSpeed = 500f;
    [SerializeField] private float panSmoothing = 0.1f;
    [SerializeField] private float zoomSmoothing = 0.1f;
    [SerializeField] private float boostMultiplier = 2f;

    private Vector2 currentPanPos;
    private Vector2 targetPanPos;
    private float currentZoomPos;
    private float targetZoomPos;

    private void Awake()
    {
        currentPanPos = transform.position;
        targetPanPos = transform.position;
        currentZoomPos = transform.position.z;
        targetZoomPos = transform.position.z;
    }

    private void Update()
    {
        UpdatePosition();
    }

    private Vector3 GetInputVelocity()
    {
        float b = GInput.GetButton(Cmd.Boost) ? boostMultiplier : 1f;
        float x = GInput.GetAxisRaw(InputAxis.Horizontal) * panSpeed * Time.unscaledDeltaTime;
        float y = GInput.GetAxisRaw(InputAxis.Vertical) * panSpeed * Time.unscaledDeltaTime;
        float z = GInput.GetMouseWheelRaw() * zoomSpeed * Time.unscaledDeltaTime;
        return new Vector3(x, y, z) * b;
    }

    private void UpdatePosition()
    {
        Vector3 v = GetInputVelocity();
        targetPanPos += new Vector2(-v.x, v.y);
        targetZoomPos -= v.z;

        currentPanPos = Vector2.Lerp(currentPanPos, targetPanPos, panSmoothing);
        currentZoomPos = Mathf.Lerp(currentZoomPos, targetZoomPos, zoomSmoothing);

        transform.position = new Vector3(currentPanPos.x, currentPanPos.y, currentZoomPos);
    }
}