using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    [SerializeField] private Transform target = default;
    [SerializeField] private Vector3 offset = default;
    [SerializeField] private bool freezeZ = default;

    private void Awake()
    {
        offset = new Vector3(
            target.position.x - transform.position.x,
            target.position.y - transform.position.y,
            offset.z);
    }

    private void LateUpdate()
    {
        Vector3 pos = target.position + offset;
        if (freezeZ)
        {
            pos.z = transform.position.z;
        }

        transform.position = pos;
    }
}