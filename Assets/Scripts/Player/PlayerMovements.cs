using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovements : NetworkBehaviour
{
    [SerializeField] float moveSpeed;
    [SerializeField] float mouseXSensitivity;
    [SerializeField] float mouseYSensitivity;

    [HideInInspector]
    public bool thirdPersonView;

    [HideInInspector]
    public GameObject playerCam;
    [HideInInspector]
    public Rigidbody rb;
    private float currentVerticalRotation = 0f;

    private void Update()
    {
        if (playerCam == null || rb == null || !isLocalPlayer) return;

        // Movement
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        Vector3 movement = new Vector3(moveX, 0, moveZ).normalized * Time.deltaTime * moveSpeed;
        transform.Translate(movement);

        // Rotation
        float rotateX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * mouseXSensitivity;
        currentVerticalRotation -= Input.GetAxisRaw("Mouse Y") * Time.deltaTime * mouseYSensitivity;
        currentVerticalRotation = Mathf.Clamp(currentVerticalRotation, -90, 90);

        // Rotate (left/right) the body
        transform.Rotate(new Vector3(0, rotateX, 0));
        // Rotate (top/bottom) the camera
        playerCam.transform.localEulerAngles = new Vector3(currentVerticalRotation, 0, 0);
    }
}