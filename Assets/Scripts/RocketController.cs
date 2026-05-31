using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RocketController : MonoBehaviour
{
    [Header("Manual Input")]
    [Tooltip("When enabled, keyboard input is disabled and only the ML agent can control this rocket")]
    public bool agentControlled = false;
    [Tooltip("Torque applied around the X axis per input frame (N·m)")]
    public float rotationTorque = 20f;

    [Tooltip("Controls are disabled once the rocket's Y position falls below this value")]
    public float controlCutoffY = 3.5f;

    [Header("PID Control")]
    [Tooltip("Proportional gain on rotation angle error")]
    public float Kp = 0f;
    [Tooltip("Integral gain on accumulated rotation error")]
    public float Ki = 0f;
    [Tooltip("Derivative gain — applied directly to angular velocity (derivative on measurement)")]
    public float Kd = 0f;
    [Tooltip("Clamp on accumulated PID integral to prevent windup")]
    public float integralClamp = 15f;
    [Tooltip("Flip the PID correction direction if it fights rather than corrects the rocket")]
    public bool invertPidOutput = false;

    [Header("Episode Reset")]
    [Tooltip("Z position the rocket spawns at on every reset")]
    public float spawnZ = 0f;
    [Tooltip("Y position the rocket spawns at on every reset")]
    public float spawnHeight = 30f;
    [Tooltip("Max angular impulse (N·m·s) applied to the rocket on episode start to kick it into a spin")]
    public float maxStartImpulse = 2f;

    [Header("Idle Reset")]
    [Tooltip("Seconds of near-zero movement before triggering a reset")]
    public float idleResetTime = 3f;

    // Set by RocketAgent to EndEpisode(); defaults to ResetEpisode() when no agent is present.
    public System.Action onIdleTimeout;

    private Rigidbody _rb;
    private float _pidIntegral;
    private float _idleTimer;

    private const float IdleSpeedThreshold = 0.05f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        ResetEpisode();
    }

    public void ResetEpisode()
    {
        _rb.position = new Vector3(0f, spawnHeight, spawnZ);
        _rb.rotation = Quaternion.identity;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _idleTimer = 0f;

        float impulse = maxStartImpulse * (Random.value < 0.5f ? -1f : 1f);
        _rb.AddTorque(Vector3.right * impulse, ForceMode.Impulse);
    }

    public void ResetPidState()
    {
        _pidIntegral = 0f;
    }

    // Returns X rotation in (-180, 180]. 0 = upright.
    public float GetAngleDegrees()
    {
        float angle = _rb.rotation.eulerAngles.x;
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    void FixedUpdate()
    {
        // ── Idle detection ────────────────────────────────────────────────────
        float speed = _rb.linearVelocity.magnitude + _rb.angularVelocity.magnitude;
        if (speed < IdleSpeedThreshold)
        {
            _idleTimer += Time.fixedDeltaTime;
            if (_idleTimer >= idleResetTime)
            {
                _idleTimer = 0f;
                if (onIdleTimeout != null)
                    onIdleTimeout.Invoke();
                else
                    ResetEpisode();
                return;
            }
        }
        else
        {
            _idleTimer = 0f;
        }

        if (_rb.position.y < controlCutoffY) return;

        // ── Manual keyboard input ─────────────────────────────────────────────
        if (!agentControlled)
        {
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                float input = 0f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  input -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input += 1f;

                if (input != 0f)
                    _rb.AddTorque(Vector3.right * input * rotationTorque, ForceMode.Force);
            }
        }

        // ── PID rotation control ──────────────────────────────────────────────
        if (Kp == 0f && Ki == 0f && Kd == 0f) return;

        float angleDeg    = GetAngleDegrees();
        float angVelDeg   = _rb.angularVelocity.x * Mathf.Rad2Deg;
        float error       = -angleDeg; // target is 0°

        _pidIntegral = Mathf.Clamp(_pidIntegral + error * Time.fixedDeltaTime,
                                   -integralClamp, integralClamp);

        float rawOutput = Kp * error + Ki * _pidIntegral + Kd * (-angVelDeg);
        float torque    = Mathf.Clamp(rawOutput, -rotationTorque, rotationTorque);

        if (invertPidOutput) torque = -torque;

        _rb.AddTorque(Vector3.right * torque, ForceMode.Force);
    }
}
