using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Spins the dart while flying
 */
public class SpinAnimation : MonoBehaviour
{
    private float speed = 100f;

    public Collider collid;
    public Rigidbody body;


    void Start()
    {
        //collider.GetComponent<Collider>();
    }

    void Update()
    {
        transform.Rotate(Vector3.right, speed * Time.deltaTime);
        //transform.RotateAround(collid.bounds.center, Vector3.up, speed * Time.deltaTime);


        //float v = speed * Time.deltaTime;
        //body.AddTorque(transform.right * v);
        //body.AddTorque(transform.right * v);
    }
}