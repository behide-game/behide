using UnityEngine;

public class Prop : MonoBehaviour
{
    public bool severalMeshes;

    public Mesh mesh;
    public Material[] materials;
    public float mass;
    public Vector3 scale;

    private void Start()
    {
        if (!severalMeshes)
        {
            MeshFilter localMesh = transform.GetComponent<MeshFilter>();

            if (localMesh != null)
            {
                mesh = localMesh.mesh;
                materials = transform.GetComponent<MeshRenderer>().materials;
                mass = transform.GetComponent<Rigidbody>().mass;
                scale = transform.localScale;
            }
            else
            {
                mesh = transform.GetComponentInChildren<MeshFilter>().mesh;
                materials = transform.GetComponentInChildren<MeshRenderer>().materials;
                mass = transform.GetComponentInChildren<Rigidbody>().mass;
                scale = transform.GetComponentInChildren<MeshFilter>().transform.localScale;
            }
        }
    }

    public bool IsReady()
    {
        return mesh != null && materials != null && mass != 0 && scale != null;
    }
}