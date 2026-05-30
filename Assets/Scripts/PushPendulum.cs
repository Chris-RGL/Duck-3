using UnityEngine;

public class PushPendulum : MonoBehaviour
{
    public float pushStrength = 10f;

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        // Pushes along the X axis so it rotates around the Z axis
        rb.AddForce(Vector3.right * pushStrength, ForceMode.Impulse);
    }
}