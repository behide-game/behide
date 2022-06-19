using UnityEngine;

public class Prop : MonoBehaviour
{
    public PropScriptableObject prop;

    private void Start()
    {
        GameObject graphics = Instantiate(prop.graphicsPrefab, transform.position, transform.rotation, transform);
        GameObject colliders = Instantiate(prop.collidersPrefab, transform.position, transform.rotation, transform);

        graphics.name = prop.graphicsPrefab.name;
        colliders.name = prop.collidersPrefab.name;

        if (transform.TryGetComponent(out Rigidbody rb))
        {
            rb.mass = prop.mass;
        }
        else
        {
            Debug.LogWarning("Cannot find rigidbody component.", gameObject);
        }
    }
}