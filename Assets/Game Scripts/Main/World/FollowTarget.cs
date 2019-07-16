using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private bool freezeZ;

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