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
    public Camera playerCam;

    [NonSerialized]
    public string currentPropName;

    private void Update()
    {
        if (Input.GetButtonDown("Fire1")) { Shoot(); }
    }

    private void Shoot()
    {
        bool raycastHitProp = Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out RaycastHit hitData, maxShootDistance);

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
                    playerCam.transform.localPosition = Vector3.zero;
                    CmdTransformInProp(hitData.transform.gameObject);
                }
            }
        }
    }

    [Command]
    void CmdTransformInProp(GameObject propGameObject)
    {
        RpcTransformInProp(gameObject, propGameObject);
        Debug.Log($"Command: Transform {transform.name} in {propGameObject.name}", transform);
    }

    [ClientRpc]
    private void RpcTransformInProp(GameObject targetPlayer, GameObject targetProp)
    {
        PropScriptableObject prop = targetProp.GetComponent<Prop>().prop;
        targetPlayer.transform.Find("Default graphics").gameObject.SetActive(false);
        Destroy(targetPlayer.transform.Find("Prop graphics"));

        GameObject propGraphics = new("Prop graphics");
        propGraphics.transform.SetParent(targetPlayer.transform, false);

        Instantiate(prop.graphicsPrefab, propGraphics.transform.position, prop.graphicsPrefab.transform.rotation, propGraphics.transform);
        Instantiate(prop.collidersPrefab, propGraphics.transform.position, prop.collidersPrefab.transform.rotation, propGraphics.transform).tag = playerTag;
        targetPlayer.GetComponent<Rigidbody>().mass = prop.mass;

        Debug.Log($"Rpc: Transformed {targetPlayer.name} in {targetProp.name}", targetPlayer);
    }
}