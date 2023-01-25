using System;
using UnityEngine;
using Mirror;

public class PlayerPropShoot : NetworkBehaviour
{
    [SerializeField] private float maxShootDistance;
    [SerializeField] private string propTag;
    [SerializeField] private string playerTag;

    [HideInInspector]
    public GameObject playerCam;

    [HideInInspector]
    public string currentPropName;

    private void Update()
    {
        if (Input.GetButtonDown("Fire1")) Shoot();
    }

    private void Shoot()
    {
        bool raycastHitProp = Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out RaycastHit hitData, maxShootDistance);

        if (raycastHitProp // Did we touch anything ?
            && hitData.transform.gameObject.CompareTag(propTag) // Is it a prop ?
            && hitData.transform.TryGetComponent(out Prop touchedProp)
            && currentPropName != touchedProp.prop.propName) // Is it a different from the current prop
        {
            CmdTransformInProp(touchedProp.gameObject);

            currentPropName = touchedProp.prop.propName;
            // Move the camera
            Vector3 touchedPropSize = hitData.transform.GetComponentInChildren<MeshRenderer>().bounds.size;
            playerCam.transform.localPosition = new Vector3(0, touchedPropSize.y * 1.5f, -touchedPropSize.z / 2);
            playerCam.transform.GetComponentInChildren<Camera>().transform.localPosition = new Vector3(0, 0, -touchedPropSize.z);
        }
    }

    [Command]
    private void CmdTransformInProp(GameObject propGameObject) => RpcTransformInProp(propGameObject);

    [ClientRpc]
    private void RpcTransformInProp(GameObject targetProp)
    {
        PropScriptableObject prop = targetProp.GetComponent<Prop>().prop;

        // Remove old
        this.transform.Find("Default graphics").gameObject.SetActive(false);

        Transform oldPropGraphics = this.transform.Find("Prop renderer");
        if (oldPropGraphics != null) Destroy(oldPropGraphics.gameObject);

        // Add new
        GameObject propRenderer = Instantiate(prop.prefab, this.transform);
        propRenderer.name = "Prop renderer";
        propRenderer.tag = this.tag;
        this.GetComponent<Rigidbody>().mass = prop.mass;
    }
}