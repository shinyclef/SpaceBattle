using UnityEngine;

public class ShipOld : MonoBehaviour
{
    private void Update()
    {
        transform.rotation = transform.rotation.normalized * Quaternion.AngleAxis(Mathf.Rad2Deg * 0.5f * Time.deltaTime, new Vector3(0, 0, -1));
        Vector3 velocity = transform.rotation * new Vector3(0, 1, 0) * 20f;
        transform.position = transform.position + velocity * Time.deltaTime;
    }
}