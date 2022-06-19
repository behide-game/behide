using UnityEngine;

[CreateAssetMenu(fileName = "PropScriptableObject", menuName = "Behide/Prop")]
public class PropScriptableObject : ScriptableObject
{
    public string propName;
    public GameObject graphicsPrefab;
    public GameObject collidersPrefab;
    public float mass;
}