using UnityEngine;

public class PlayerCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 3, -6);
    public float smoothSpeed = 10f;

    private Vector3 _currentVelocity;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + target.TransformDirection(offset);
        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos, ref _currentVelocity, 1f / smoothSpeed);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}