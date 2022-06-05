using UnityEngine;

public class CameraConstants : MonoBehaviour
{
    public Vector3 defaultPosition;
    public Quaternion defaultRotation;

    void Start()
    {
        defaultPosition = transform.position;
        defaultRotation = transform.rotation;
    }
}
