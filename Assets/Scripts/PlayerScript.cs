using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerScript : NetworkBehaviour
{
    public float mouseXSensitivity;
    public float mouseYSensitivity;


    private Camera playerCamera;
    [SerializeField]
    private Vector3 defaultCameraPosition;
    private float cameraRotationY = 0f;

    private Rigidbody rb;

    public override void OnStartLocalPlayer()
    {
        playerCamera = Camera.main;
        playerCamera.transform.SetParent(transform);
        playerCamera.transform.localPosition = defaultCameraPosition;

        rb = transform.GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Confined;
    }

    public override void OnStopClient()
    {
        CameraConstants cameraConstants = Camera.main.GetComponent<CameraConstants>();
        Camera.main.transform.SetParent(null, false);
        Camera.main.transform.SetPositionAndRotation(cameraConstants.defaultPosition, cameraConstants.defaultRotation);
    }

    void Update()
    {
        if (!isLocalPlayer) { return; }

        float moveX = Input.GetAxis("Horizontal") * Time.deltaTime * 4f;
        float moveZ = Input.GetAxis("Vertical") * Time.deltaTime * 4f;
        float rotateX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * mouseXSensitivity;
        float rotateY = - Input.GetAxisRaw("Mouse Y") * Time.deltaTime * mouseYSensitivity;
        cameraRotationY += rotateY;
        cameraRotationY = Mathf.Clamp(cameraRotationY, -90, 90);

        // Move player
        transform.Translate(moveX, 0, moveZ);
        // Rotate view
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0, rotateX, 0));
        playerCamera.transform.localEulerAngles = new Vector3(cameraRotationY, 0, 0);
    }
}