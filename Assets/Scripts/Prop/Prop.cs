using UnityEngine;

public class Prop : MonoBehaviour
{
    public PropScriptableObject prop;

    private void Start()
    {
        GameObject prefab = Instantiate(prop.prefab, transform.position, transform.rotation, transform);
        prefab.name = "Prefab";
        gameObject.name = prop.name;

        if (!transform.TryGetComponent(out Rigidbody rb))
        {
            Debug.LogWarning("Cannot find rigidbody component.", gameObject);
            return;
        }
        rb.mass = prop.mass;
    }
}