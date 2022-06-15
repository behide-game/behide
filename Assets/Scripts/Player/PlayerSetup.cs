using UnityEngine;
using Mirror;

public class PlayerSetup : NetworkBehaviour
{
    [SerializeField]
    Behaviour[] componentsToDisable;
    [SerializeField]
    string remotePlayerLayer;

    private Vector3 defaultCameraPosition;

    void Start()
    {
        if (Application.platform == RuntimePlatform.LinuxServer) { return; }


        if (!isLocalPlayer)
        {
            AssignRemoteLayer();
            DisableComponents();
        }
        else
        {

            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = defaultCameraPosition;
            Camera.main.transform.localEulerAngles = Vector3.zero;

            PlayerMovements playerMovements = transform.GetComponent<PlayerMovements>();
            PlayerPropShoot playerPropShoot = transform.GetComponent<PlayerPropShoot>();
            playerMovements.rb = GetComponent<Rigidbody>();
            playerMovements.playerCam = Camera.main;
            playerPropShoot.playerCam = Camera.main;

            #if UNITY_EDITOR
            #else
            Cursor.lockState = CursorLockMode.Confined;
            #endif
        }
    }

    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();

        CameraConstants camConstants = Camera.main.GetComponent<CameraConstants>();

        Camera.main.transform.SetParent(null);
        Camera.main.transform.localPosition = camConstants.defaultPosition;
        Camera.main.transform.localRotation = camConstants.defaultRotation;

        Cursor.lockState = CursorLockMode.None;
    }

    void AssignRemoteLayer()
    {
        gameObject.layer = LayerMask.NameToLayer(remotePlayerLayer);
    }

    void DisableComponents()
    {
        for (int i = 0; i < componentsToDisable.Length; i++)
        {
            componentsToDisable[i].enabled = false;
        }
    }
}
