using Unity.Mathematics;
using UnityEngine;

public class CameraCtr : MonoBehaviour
{
    [SerializeField] private float panSpeed = 50f;
    [SerializeField] private float zoomSpeed = 500f;
    [SerializeField] private float minZ = -500f;
    [SerializeField] private float maxZ = -20f;
    [SerializeField] private float panSmoothing = 0.1f;
    [SerializeField] private float zoomSmoothing = 0.1f;
    [SerializeField] private float boostMultiplier = 2f;

    private Vector2 currentPanPos;
    private Vector2 targetPanPos;
    private float currentZPos;
    private float targetZPos;
    private Vector2 viewAngle;
    private Vector2 mouseDragStart;

    private void Awake()
    {
        currentPanPos = transform.position;
        targetPanPos = transform.position;
        currentZPos = transform.position.z;
        targetZPos = transform.position.z;
        viewAngle = new Vector2(transform.rotation.x, transform.rotation.y);
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

    private Vector2 GetInputAngle()
    {
        
        return new Vector2(0f, 0f);
    }

    private void UpdatePosition()
    {
        Vector3 v = GetInputVelocity();
        targetPanPos += new Vector2(v.x, v.y);
        targetZPos = math.clamp(targetZPos + v.z, minZ, maxZ);

        currentPanPos = Vector2.Lerp(currentPanPos, targetPanPos, panSmoothing);
        currentZPos = math.lerp(currentZPos, targetZPos, zoomSmoothing);

        transform.position = new Vector3(currentPanPos.x, currentPanPos.y, currentZPos);
    }
}