using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PendulumController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Rigidbody on the rod GameObject")]
    public Rigidbody rodRigidbody;

    [Header("Angle PID Gains")]
    [Tooltip("Proportional gain on rod angle error (degrees)")]
    public float Kp = 25f;
    [Tooltip("Integral gain on accumulated angle error")]
    public float Ki = 0.5f;
    [Tooltip("Derivative gain — applied directly to rod angular velocity")]
    public float Kd = 8f;

    [Header("Position Correction Gains")]
    [Tooltip("Proportional gain on cart X position — pulls cart back toward centre")]
    public float KpPosition = 1.5f;
    [Tooltip("Derivative gain on cart X velocity — damps cart motion")]
    public float KdPosition = 2f;

    [Header("Cart Constraints")]
    [Tooltip("Hard X position limit in world units (applied symmetrically)")]
    public float positionLimit = 10f;
    [Tooltip("Maximum cart acceleration in m/s²")]
    public float maxAcceleration = 50f;

    [Header("Anti-Windup")]
    [Tooltip("Integral accumulation is frozen when |rod angle| exceeds this (degrees)")]
    public float integralFreezeThreshold = 30f;
    [Tooltip("Clamp on the absolute value of the accumulated integral")]
    public float integralClamp = 20f;

    public float CartVelocity => _cartVelocity;

    private Rigidbody _rb;
    private float _integral;
    private float _cartVelocity;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void ResetState()
    {
        _integral = 0f;
        _cartVelocity = 0f;
    }

    void FixedUpdate()
    {
        // ── Observations ─────────────────────────────────────────────────────
        float cartPosition  = _rb.position.x;                                  // obs 1
        float cartVelocity  = _cartVelocity;                                   // obs 2
        float rodAngle      = GetRodAngleDegrees();                            // obs 3  (0 = upright)
        float rodAngularVel = rodRigidbody.angularVelocity.z * Mathf.Rad2Deg; // obs 4

        // ── PID on rod angle ──────────────────────────────────────────────────
        // Error is negative angle: we want angle → 0, so error = 0 − angle.
        // Positive Unity Z-rotation is counterclockwise (rod tilts left) →
        // negative control output pushes the cart left, which rights the rod.
        float angleError = -rodAngle;

        if (Mathf.Abs(rodAngle) < integralFreezeThreshold)
            _integral = Mathf.Clamp(_integral + angleError * Time.fixedDeltaTime, -integralClamp, integralClamp);

        float pidOutput = Kp * angleError
                        + Ki * _integral
                        + Kd * (-rodAngularVel); // derivative on measurement, no kick

        // ── Position correction ───────────────────────────────────────────────
        // Subtracts a gentle restoring force so the cart stays near the origin.
        float posCorrection = KpPosition * cartPosition + KdPosition * cartVelocity;

        // ── Integrate to velocity ─────────────────────────────────────────────
        float acceleration = Mathf.Clamp(pidOutput - posCorrection, -maxAcceleration, maxAcceleration);
        _cartVelocity += acceleration * Time.fixedDeltaTime;

        // ── Enforce position limits ───────────────────────────────────────────
        float newX = cartPosition + _cartVelocity * Time.fixedDeltaTime;

        if (newX >= positionLimit)
        {
            newX = positionLimit;
            _cartVelocity = Mathf.Min(_cartVelocity, 0f);
        }
        else if (newX <= -positionLimit)
        {
            newX = -positionLimit;
            _cartVelocity = Mathf.Max(_cartVelocity, 0f);
        }

        // ── Move kinematic sphere along X only ────────────────────────────────
        Vector3 pos = _rb.position;
        pos.x = newX;
        _rb.MovePosition(pos);
    }

    // Returns rod angle in degrees, normalised to (-180, 180].
    // 0° = rod pointing straight up. Positive = counterclockwise (left tilt).
    float GetRodAngleDegrees()
    {
        float angle = rodRigidbody.rotation.eulerAngles.z;
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
