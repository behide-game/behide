using UnityEngine;
using Mirror;

public class PlayerAudioManager : NetworkBehaviour
{
    [SerializeField] private AudioClip[] audios;
    [SerializeField] private AudioSource audioSource;
    private bool playing = false;

    private void Update()
    {
        if (playing && !audioSource.isPlaying) playing = false;
    }

    public void Play(int audioIndex) { if (!playing) PlayCmd(audioIndex); }

    [Command]
    private void PlayCmd(int audioIndex) => PlayRpc(audioIndex);

    [ClientRpc]
    private void PlayRpc(int audioIndex)
    {
        AudioClip audio = audios[audioIndex];
        audioSource.PlayOneShot(audio);
        playing = true;
    }
}
