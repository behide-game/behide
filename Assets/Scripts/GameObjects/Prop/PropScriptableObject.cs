using UnityEngine;

[CreateAssetMenu(fileName = "PropScriptableObject", menuName = "Behide/Prop")]
public class PropScriptableObject : ScriptableObject
{
    public string propName;
    public GameObject prefab;
    public float mass;
}