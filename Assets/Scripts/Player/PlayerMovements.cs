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
    private float rotateY = 0f;

    private void FixedUpdate()
    {
        if (playerCam == null || rb == null || !isLocalPlayer) return;

        // Movement
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(moveX, 0, moveZ).normalized * (Time.fixedDeltaTime * moveSpeed);
        transform.Translate(movement);

        // Rotation
        float rotateX = Input.GetAxisRaw("Mouse X") * Time.fixedDeltaTime * mouseXSensitivity;
        rotateY -= Input.GetAxisRaw("Mouse Y") * Time.fixedDeltaTime * mouseYSensitivity;
        rotateY = Mathf.Clamp(rotateY, -90, 90);

        // Rotate (left/right) the body
        transform.Rotate(new Vector3(0, rotateX, 0));
        // Rotate (top/bottom) the camera
        playerCam.transform.localEulerAngles = new Vector3(rotateY, 0, 0);
    }
}