using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Behaviour for darts...check when dart collides and perform corresponding actions.
 */
public class DartArrow : MonoBehaviour
{
    //public Vector3 contactPoint = Vector3.zero;

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("Arrow collided with: " + collision.collider.name);

        if (collision.collider.name.Equals("Wall"))
        {
            //Destroy(GetComponent<Rigidbody>()); //destroy Rigid Body to stop gravity
            GetComponent<Rigidbody>().isKinematic = true; //set it as isKinematic instead

            var contactPoint = collision.GetContact(0).point;
            //Debug.Log("Collision: " + contactPoint);

            Vector3 hitPoint = new Vector3(contactPoint.x, contactPoint.y);

            GameManager.Instance.DecodeScore(hitPoint); // call callback method
        }
    }
}
