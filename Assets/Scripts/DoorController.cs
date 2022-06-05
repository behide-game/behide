using System.Collections;
using System.Collections.Generic;
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
        //animator.SetBool("Player Nearby", true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") != true) { return; }

        playersNearby += 1;
        animator.SetBool("Player Nearby", true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") != true) { return; }

        playersNearby -= 1;
        if (playersNearby == 0) { animator.SetBool("Player Nearby", false); }
    }
}