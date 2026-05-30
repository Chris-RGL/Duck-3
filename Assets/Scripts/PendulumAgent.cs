using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// Attach to the cart sphere alongside PendulumController and DecisionRequester.
/// BehaviorParameters: Vector Observations = 4, Continuous Actions = 3.
/// Recommended DecisionRequester Decision Period: 5 (acts every 5 physics steps).
[RequireComponent(typeof(PendulumController))]
[RequireComponent(typeof(DecisionRequester))]
public class PendulumAgent : Agent
{
    [Header("References")]
    [Tooltip("Rigidbody on the rod GameObject")]
    public Rigidbody rodRigidbody;

    [Header("PID Output Ranges")]
    [Tooltip("Upper bound of Kp the agent can output")]
    public float kpMax = 50f;
    [Tooltip("Upper bound of Ki the agent can output")]
    public float kiMax = 2f;
    [Tooltip("Upper bound of Kd the agent can output")]
    public float kdMax = 20f;

    [Header("Episode Reset")]
    [Tooltip("Max random angle (degrees) applied to the rod on episode begin — trains recovery")]
    public float randomStartAngle = 5f;

    [Header("Reward Weights")]
    [Tooltip("Small positive reward added each step the rod stays above the sphere plane")]
    public float aliveRewardPerStep = 0.01f;
    [Tooltip("Scales the cosine angle reward — positive near 0°, increasingly negative toward 90°")]
    public float angleRewardScale = 1f;
    [Tooltip("Scales the origin position reward — positive at centre, increasingly negative toward limit")]
    public float positionRewardScale = 0.3f;
    [Tooltip("One-shot penalty applied at the moment the rod falls below the sphere plane")]
    public float fallPenalty = 1f;

    [Header("Observation Normalisation")]
    [Tooltip("Expected max cart speed in m/s — used to normalise the velocity observation")]
    public float maxExpectedCartSpeed = 20f;
    [Tooltip("Expected max rod angular speed in deg/s — used to normalise the angular velocity observation")]
    public float maxExpectedAngularSpeed = 720f;

    private PendulumController _controller;
    private Rigidbody _cartRb;
    private Vector3 _rodOffsetFromCart;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void Initialize()
    {
        _controller = GetComponent<PendulumController>();
        _cartRb = GetComponent<Rigidbody>();
        // Capture rod's offset relative to the cart so the pair always spawns together.
        // Both objects must be positioned correctly relative to each other in the scene.
        _rodOffsetFromCart = rodRigidbody.position - _cartRb.position;
    }

    public override void OnEpisodeBegin()
    {
        // Always reset cart to world origin regardless of where it was in the scene.
        // transform.position must be set directly so Physics.SyncTransforms() pushes
        // it into the physics engine — rb.position alone on a kinematic body is deferred.
        transform.position = Vector3.zero;

        // Place rod at the same offset from the origin it had from the cart in the scene,
        // so both objects arrive together at (0,0,0) + rod offset.
        rodRigidbody.position = _rodOffsetFromCart;
        rodRigidbody.rotation = Quaternion.Euler(0f, 0f, Random.Range(-randomStartAngle, randomStartAngle));
        rodRigidbody.linearVelocity = Vector3.zero;
        rodRigidbody.angularVelocity = Vector3.zero;

        Physics.SyncTransforms();

        _controller.ResetState();
    }

    // ── Observations (4) ──────────────────────────────────────────────────────

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(_cartRb.position.x / _controller.positionLimit);                           // [-1, 1]
        sensor.AddObservation(_controller.CartVelocity / maxExpectedCartSpeed);                          // [-1, 1]
        sensor.AddObservation(GetRodAngleDegrees() / 180f);                                              // [-1, 1]
        sensor.AddObservation(rodRigidbody.angularVelocity.z * Mathf.Rad2Deg / maxExpectedAngularSpeed); // [-1, 1]
    }

    // ── Actions (3 continuous: Kp, Ki, Kd) ───────────────────────────────────

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Remap tanh output [-1, 1] → [0, max] for each gain
        _controller.Kp = Mathf.Lerp(0f, kpMax, (actions.ContinuousActions[0] + 1f) * 0.5f);
        _controller.Ki = Mathf.Lerp(0f, kiMax, (actions.ContinuousActions[1] + 1f) * 0.5f);
        _controller.Kd = Mathf.Lerp(0f, kdMax, (actions.ContinuousActions[2] + 1f) * 0.5f);

        float rodAngle = GetRodAngleDegrees();
        float cartPos  = _cartRb.position.x;

        // Terminate — rod has fallen below the sphere's horizontal plane
        if (Mathf.Abs(rodAngle) >= 90f)
        {
            AddReward(-fallPenalty);
            EndEpisode();
            return;
        }

        // Alive: small per-step bonus that accumulates the longer it stays balanced
        AddReward(aliveRewardPerStep);

        // Angle: cos(angle) → +1 at 0°, 0 at 90°, increasingly negative beyond
        AddReward(Mathf.Cos(rodAngle * Mathf.Deg2Rad) * angleRewardScale);

        // Position: linear — +1 at origin, -1 at positionLimit
        float normPos = Mathf.Abs(cartPos) / _controller.positionLimit;
        AddReward((1f - 2f * normPos) * positionRewardScale);
    }

    // Heuristic maps actions to mid-range gains for play-mode testing without a
    // trained model (resolves to Kp ≈ 25, Ki ≈ 0.5, Kd ≈ 10).
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        ca[0] = 0f;
        ca[1] = 0f;
        ca[2] = 0f;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    float GetRodAngleDegrees()
    {
        float angle = rodRigidbody.rotation.eulerAngles.z;
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
