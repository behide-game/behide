using UnityEngine;
using Mirror;

public class PlayerSetup : NetworkBehaviour
{
    [SerializeField]
    private Behaviour[] componentsToDisable;
    [SerializeField]
    private string remotePlayerLayer;
    [SerializeField]
    private bool isHunter;
    [SerializeField]
    private GameObject cameraDiskPrefab;
    [SerializeField]
    private Vector3 defaultPropCamPos;
    [SerializeField]
    private Vector3 defaultHunterCamPos;

    private GameObject mainCam;
    private GameObject currentCamDisk;

    void Start()
    {
        if (!isLocalPlayer)
        {
            AssignRemoteLayer();
            DisableComponents();
        }
        else
        {
            PlayerMovements playerMovements = transform.GetComponent<PlayerMovements>();
            PlayerPropShoot playerPropShoot = transform.GetComponent<PlayerPropShoot>();
            playerMovements.rb = GetComponent<Rigidbody>();

            if (isHunter)
            {
                Camera.main.transform.SetParent(transform);
                Camera.main.transform.localPosition = defaultHunterCamPos;
                Camera.main.transform.localEulerAngles = Vector3.zero;

                playerMovements.playerCam = Camera.main.gameObject;
                playerPropShoot.enabled = false;
            }
            else
            {
                mainCam = Camera.main.gameObject;
                Camera.main.gameObject.SetActive(false);

                GameObject cameraDisk = Instantiate(cameraDiskPrefab, transform);
                cameraDisk.name = cameraDiskPrefab.name;
                cameraDisk.transform.localPosition = defaultPropCamPos;

                playerMovements.thirdPersonView = true;
                playerMovements.playerCam = cameraDisk;
                playerPropShoot.playerCamDisk = cameraDisk;

                currentCamDisk = cameraDisk;
            }

            if (!Application.isEditor)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();

        if (isHunter)
        {
            CameraConstants camConstants = Camera.main.GetComponent<CameraConstants>();

            Camera.main.transform.SetParent(null);
            Camera.main.transform.localPosition = camConstants.defaultPosition;
            Camera.main.transform.localRotation = camConstants.defaultRotation;
        }
        else if (currentCamDisk != null)
        {
            mainCam.SetActive(true);
            Destroy(currentCamDisk);
        }

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
