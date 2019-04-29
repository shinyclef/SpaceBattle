using UnityEngine;

public class References : MonoBehaviour
{
    public static References I;

    private void Awake()
    {
        I = this;
    }
}