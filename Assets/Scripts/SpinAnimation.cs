using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Spins the dart while flying
 */
public class SpinAnimation : MonoBehaviour
{
    public float speed = 10f;

    void Update()
    {
        transform.Rotate(Vector3.right, speed * Time.deltaTime);
    }
}
