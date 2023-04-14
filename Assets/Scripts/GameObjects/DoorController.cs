using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private int playersNearby = 0;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersNearby += 1;
        animator.SetBool("Player Nearby", true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersNearby -= 1;
        if (playersNearby == 0) { animator.SetBool("Player Nearby", false); }
    }
}