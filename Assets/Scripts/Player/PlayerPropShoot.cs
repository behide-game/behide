using System;
using UnityEngine;
using Mirror;

public class PlayerPropShoot : NetworkBehaviour
{
    [SerializeField]
    private float maxShootDistance;
    [SerializeField]
    private string propTag;
    [SerializeField]
    private string playerTag;

    [NonSerialized]
    public GameObject playerCamDisk;

    [NonSerialized]
    public string currentPropName;

    private void Update()
    {
        if (Input.GetButtonDown("Fire1")) { Shoot(); }
    }

    private void Shoot()
    {
        bool raycastHitProp = Physics.Raycast(playerCamDisk.transform.position, playerCamDisk.transform.forward, out RaycastHit hitData, maxShootDistance);

        // Check if touched and tag
        if (raycastHitProp && hitData.transform.gameObject.CompareTag(propTag))
        {
            // Check if prop is ready
            if (hitData.transform.TryGetComponent(out Prop touchedProp))
            {
                // Check if it's a different prop
                if (currentPropName != touchedProp.prop.propName)
                {
                    currentPropName = touchedProp.prop.propName;

                    Vector3 touchedPropSize = hitData.transform.GetComponentInChildren<MeshRenderer>().bounds.size;
                    playerCamDisk.transform.localPosition = new Vector3(0, touchedPropSize.y * 1.5f, -touchedPropSize.z / 2);
                    playerCamDisk.transform.GetComponentInChildren<Camera>().transform.localPosition = new Vector3(0, 0, -touchedPropSize.z);

                    CmdTransformInProp(hitData.transform.gameObject);
                }
            }
        }
    }

    [Command]
    void CmdTransformInProp(GameObject propGameObject)
    {
        RpcTransformInProp(gameObject, propGameObject);
    }

    [ClientRpc]
    private void RpcTransformInProp(GameObject targetPlayer, GameObject targetProp)
    {
        PropScriptableObject prop = targetProp.GetComponent<Prop>().prop;
        targetPlayer.transform.Find("Default graphics").gameObject.SetActive(false);

        if (targetPlayer.transform.Find("Prop graphics") != null)
        {
            Destroy(targetPlayer.transform.Find("Prop graphics").gameObject);
        }

        GameObject propGraphics = new("Prop graphics");
        propGraphics.transform.SetParent(targetPlayer.transform, false);

        Instantiate(prop.graphicsPrefab, propGraphics.transform.position, prop.graphicsPrefab.transform.rotation, propGraphics.transform);
        Instantiate(prop.collidersPrefab, propGraphics.transform.position, prop.collidersPrefab.transform.rotation, propGraphics.transform).tag = playerTag;
        targetPlayer.GetComponent<Rigidbody>().mass = prop.mass;
    }
}