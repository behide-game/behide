using UnityEngine;

public class PlayerKeyManager : MonoBehaviour
{
    [SerializeField] private PlayerAudioManager audioManager;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) audioManager.Play(0);
    }
}