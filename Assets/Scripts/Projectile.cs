using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Action<bool> onResult;

    [Tooltip("Destroy and report a miss if the projectile strays this far from the origin")]
    public float outOfBoundsRadius = 50f;

    void Update()
    {
        if (transform.position.magnitude > outOfBoundsRadius)
        {
            onResult?.Invoke(false);
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bucket"))
        {
            Debug.Log("caught ball");
            onResult?.Invoke(true);
            Destroy(gameObject);
        }
        else if (collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log("missed ball");
            onResult?.Invoke(false);
            Destroy(gameObject);
        }
    }
}
