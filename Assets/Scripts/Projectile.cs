using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Action<bool> onResult;

    [Tooltip("Destroy and report a miss if the projectile strays this far from the origin")]
    public float outOfBoundsRadius = 50f;

    private bool _resultFired;

    void Update()
    {
        if (!_resultFired && transform.position.magnitude > outOfBoundsRadius)
        {
            _resultFired = true;
            onResult?.Invoke(false);
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_resultFired) return;

        if (collision.gameObject.CompareTag("Bucket"))
        {
            Debug.Log("caught ball");
            _resultFired = true;
            onResult?.Invoke(true);
            Destroy(gameObject);
        }
        else if (collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log("missed ball");
            _resultFired = true;
            onResult?.Invoke(false);
            Destroy(gameObject);
        }
    }
}
