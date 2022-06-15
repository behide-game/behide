using System;
using UnityEngine;
using Mirror;

public class PlayerPropShoot : NetworkBehaviour
{
    [SerializeField]
    private float maxShootDistance;
    [SerializeField]
    private string propLayer;

    [NonSerialized]
    public Camera playerCam;

    private void Update()
    {
        if (Input.GetButtonDown("Fire1")) { Shoot(); }
    }

    private void Shoot()
    {
        bool raycastHitProp = Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out RaycastHit hitData, maxShootDistance);

        // Check if touched and layer
        if (raycastHitProp && hitData.transform.gameObject.layer == LayerMask.NameToLayer(propLayer))
        {
            // Check if prop is ready
            if (hitData.transform.TryGetComponent(out Prop touchedProp) && touchedProp.IsReady())
            {
                // Check if it's a different prop
                string currentMeshName = transform.GetComponent<MeshFilter>().mesh.name;
                string propMeshName = touchedProp.mesh.name;

                if (currentMeshName != propMeshName)
                {
                    CmdTransformInProp(hitData.transform.gameObject);
                    playerCam.transform.localPosition = Vector3.zero;
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
        Prop prop = targetProp.GetComponent<Prop>();

        targetPlayer.GetComponent<MeshFilter>().mesh = prop.mesh;
        targetPlayer.GetComponent<MeshFilter>().mesh.name = prop.mesh.name;
        targetPlayer.GetComponent<MeshRenderer>().materials = prop.materials;
        targetPlayer.GetComponent<MeshCollider>().sharedMesh = prop.mesh;
        targetPlayer.GetComponent<Rigidbody>().mass = prop.mass;
        targetPlayer.transform.localScale = prop.scale;

        Debug.Log($"Rpc: Transform {targetPlayer.name} in {targetProp.name}", targetPlayer);
    }
}