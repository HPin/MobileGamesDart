using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Used to integrate Sounds
 */
public class SoundController : MonoBehaviour
{

    public AudioClip dartSound;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<AudioSource>().clip = dartSound;
    }

    private void OnCollisionEnter(Collision collision)
    {
        GetComponent<AudioSource>().Play();
    }

}
