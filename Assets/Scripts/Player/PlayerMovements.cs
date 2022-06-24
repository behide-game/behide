using System;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovements : NetworkBehaviour
{
    [SerializeField]
    private float mouseXSensitivity;
    [SerializeField]
    private float mouseYSensitivity;

    [NonSerialized]
    public bool thirdPersonView;

    [NonSerialized]
    public GameObject playerCam;
    [NonSerialized]
    public Rigidbody rb;
    private float cameraRotationY = 0f;

    private void FixedUpdate()
    {
        float moveX = Input.GetAxis("Horizontal") * Time.fixedDeltaTime * 4f;
        float moveZ = Input.GetAxis("Vertical") * Time.fixedDeltaTime * 4f;
        float rotateX = Input.GetAxisRaw("Mouse X") * Time.fixedDeltaTime * mouseXSensitivity;
        float rotateY = -Input.GetAxisRaw("Mouse Y") * Time.fixedDeltaTime * mouseYSensitivity;
        cameraRotationY += rotateY;
        cameraRotationY = Mathf.Clamp(cameraRotationY, -90, 90);

        transform.Translate(moveX, 0, moveZ);
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0, rotateX, 0));

        playerCam.transform.localEulerAngles = new Vector3(cameraRotationY, 0, 0);
    }
}