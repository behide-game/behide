using UnityEngine;
using Mirror;

public class PlayerSetup : NetworkBehaviour
{
    [SerializeField] private Behaviour[] componentsToDisable;
    [SerializeField] private LayerMask remotePlayerLayer;
    [SerializeField] private GameObject cameraDisk;
    private Camera mainCamera;

    void Start()
    {
        if (!isLocalPlayer)
        {
            AssignRemoteLayer();
            DisableComponents();
            return;
        }

        PlayerMovements playerMovements = GetComponent<PlayerMovements>();
        PlayerPropShoot playerPropShoot = GetComponent<PlayerPropShoot>();
        playerMovements.rb = GetComponent<Rigidbody>();

        mainCamera = Camera.main;
        mainCamera.gameObject.SetActive(false);

        playerMovements.thirdPersonView = true;
        playerMovements.playerCam = cameraDisk;
        playerPropShoot.playerCam = cameraDisk;
        // if (isHunter)
        // {
        //     // Position the camera
        //     Camera.main.transform.SetParent(transform);
        //     Camera.main.transform.localPosition = defaultHunterCamPos;
        //     Camera.main.transform.localEulerAngles = Vector3.zero;

        //     playerMovements.playerCam = Camera.main.gameObject;
        //     playerPropShoot.enabled = false;
        //     return;
        // }

        // Setup camera
        // GameObject cameraDisk = Instantiate(cameraDiskPrefab, transform);
        // cameraDisk.name = cameraDiskPrefab.name;

        // if (!Application.isEditor)
        // {
        //     Cursor.lockState = CursorLockMode.Confined;
        // }
    }

    public override void OnStopLocalPlayer()
    {
        // if (isHunter)
        // {
        //     CameraConstants camConstants = Camera.main.GetComponent<CameraConstants>();

        //     Camera.main.transform.SetParent(null);
        //     Camera.main.transform.localPosition = camConstants.defaultPosition;
        //     Camera.main.transform.localRotation = camConstants.defaultRotation;
        // }
        // else if (currentCamDisk != null)
        // {
        // }

        base.OnStopLocalPlayer();

        mainCamera.gameObject.SetActive(true);
        Destroy(cameraDisk);

        Cursor.lockState = CursorLockMode.None;
    }

    void AssignRemoteLayer()
    {
        gameObject.layer = remotePlayerLayer;
    }

    void DisableComponents()
    {
        foreach (Behaviour component in componentsToDisable)
        {
            component.enabled = false;
        }
    }
}
