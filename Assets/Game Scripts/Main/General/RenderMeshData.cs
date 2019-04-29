using UnityEngine;
using UnityEngine.Rendering;

public class RenderMeshData : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public int subMesh;
    public int layer;
    public ShadowCastingMode castShadows;
    public bool receiveShadows;
}
