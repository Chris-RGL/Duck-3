using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// Attach alongside RocketController and DecisionRequester on the rocket cube.
/// BehaviorParameters: Vector Observations = 4, Continuous Actions = 3, Behavior Name = "Rocket".
/// Recommended DecisionRequester Decision Period: 5.
[RequireComponent(typeof(RocketController))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DecisionRequester))]
public class RocketAgent : Agent
{
    [Header("PID Output Ranges")]
    [Tooltip("Upper bound of Kp the agent can output")]
    public float kpMax = 50f;
    [Tooltip("Upper bound of Ki the agent can output")]
    public float kiMax = 2f;
    [Tooltip("Upper bound of Kd the agent can output")]
    public float kdMax = 20f;

    [Header("Landing")]
    [Tooltip("Y position at or below which landing is evaluated")]
    public float landingYThreshold = 0.6f;
    [Tooltip("Max X angle (degrees) for a successful landing")]
    public float successAngleThreshold = 15f;

    [Header("Reward Weights")]
    [Tooltip("Per-step reward when angle is near 0 — accumulates while aligned")]
    public float alignedRewardPerStep = 0.005f;
    [Tooltip("Scales the cosine-based per-step angle reward (+1 at 0°, approaches -1 at 180°)")]
    public float angleRewardScale = 0.005f;
    [Tooltip("Additional per-step penalty per degree of drift beyond 5°")]
    public float driftPenaltyPerDegree = 0.0001f;
    [Tooltip("One-shot bonus awarded on a successful upright landing")]
    public float landingBonus = 2f;
    [Tooltip("One-shot penalty on failure (tip-over or bad landing)")]
    public float failPenalty = 1f;

    private RocketController _controller;
    private Rigidbody _rb;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void Initialize()
    {
        _controller = GetComponent<RocketController>();
        _rb = GetComponent<Rigidbody>();

        // Override the play-mode idle fallback so the agent ends the episode cleanly.
        _controller.onIdleTimeout = EndEpisode;
    }

    public override void OnEpisodeBegin()
    {
        _controller.ResetEpisode();
        _controller.ResetPidState();
        Physics.SyncTransforms();
    }

    // ── Observations (4) ──────────────────────────────────────────────────────

    public override void CollectObservations(VectorSensor sensor)
    {
        // Height above landing threshold, normalised to [0, 1] at episode start.
        float heightRange = Mathf.Max(1f, _controller.spawnHeight - landingYThreshold);
        float normY = (_rb.position.y - landingYThreshold) / heightRange;
        sensor.AddObservation(Mathf.Clamp(normY, -0.5f, 1.5f));                           // ~[0, 1]

        sensor.AddObservation(_rb.linearVelocity.y);
        sensor.AddObservation(_controller.GetAngleDegrees() / 180f);
        sensor.AddObservation(_rb.angularVelocity.x * Mathf.Rad2Deg);
    }

    // ── Actions (3 continuous: Kp, Ki, Kd) ───────────────────────────────────

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Remap tanh output [-1, 1] → [0, max] for each gain.
        _controller.Kp = Mathf.Lerp(0f, kpMax, (actions.ContinuousActions[0] + 1f) * 0.5f);
        _controller.Ki = Mathf.Lerp(0f, kiMax, (actions.ContinuousActions[1] + 1f) * 0.5f);
        _controller.Kd = Mathf.Lerp(0f, kdMax, (actions.ContinuousActions[2] + 1f) * 0.5f);

        float angle = _controller.GetAngleDegrees();

        // ── Termination: landing ──────────────────────────────────────────────
        if (_rb.position.y <= landingYThreshold)
        {
            AddReward(Mathf.Abs(angle) < successAngleThreshold ? landingBonus : -failPenalty);
            EndEpisode();
            return;
        }

        // ── Per-step alignment rewards ────────────────────────────────────────

        // Cosine reward: +1 at 0°, 0 at 90°, increasingly negative beyond.
        AddReward(Mathf.Cos(angle * Mathf.Deg2Rad) * angleRewardScale);

        // Small flat bonus just for staying alive and aligned.
        if (Mathf.Abs(angle) < successAngleThreshold)
            AddReward(alignedRewardPerStep);

        // Penalty that grows proportionally with how far the rocket has drifted.
        float drift = Mathf.Max(0f, Mathf.Abs(angle) - 5f);
        if (drift > 0f)
            AddReward(-drift * driftPenaltyPerDegree);
    }

    // Heuristic maps actions to mid-range gains for play-mode testing without a
    // trained model (resolves to Kp ≈ kpMax/2, etc.).
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        ca[0] = 0f;
        ca[1] = 0f;
        ca[2] = 0f;
    }
}
