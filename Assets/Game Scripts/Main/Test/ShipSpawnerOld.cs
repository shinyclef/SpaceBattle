using UnityEngine;
using Random = Unity.Mathematics.Random;

public class ShipSpawnerOld : MonoBehaviour
{
    public GameObject Prefab;
    public int Count = 1000;

    public void Start()
    {
        Random r = new Random(50);
        for (int i = 0; i < Count; i++)
        {
            Vector3 pos = new Vector3(r.NextFloat(-200, 200), r.NextFloat(-200, 200), r.NextFloat(-20, 20));
            Instantiate(Prefab, transform.position + pos, Quaternion.identity);
        }
    }
}