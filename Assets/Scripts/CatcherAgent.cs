using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// Attach to the bucket alongside a kinematic Rigidbody and DecisionRequester.
/// BehaviorParameters: Behavior Name = "Catcher", Vector Observations = 3, Continuous Actions = 1.
/// Set launcher.agentControlled = true. Tag the bucket GameObject "Bucket".
///
/// Episode flow:
///   1. PrepareShot — launcher picks a random angle + power (ball not yet in scene).
///   2. Positioning phase — agent has positioningSteps decisions to slide the bucket along X.
///   3. Fire — after positioningSteps decisions the ball launches; bucket is locked for the rest.
///   4. Result — ball hits "Bucket" (positive reward) or "Ground" (negative reward scaled by
///      distance between landing spot and bucket centre), then episode ends.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DecisionRequester))]
public class CatcherAgent : Agent
{
    [Header("References")]
    [Tooltip("The ProjectileLauncher — must have agentControlled = true")]
    public ProjectileLauncher launcher;

    [Header("Movement")]
    [Tooltip("Metres the bucket moves per decision at maximum action value")]
    public float movePerDecision = 2f;
    [Tooltip("Half-width of the arena — bucket is clamped to ±xLimit")]
    public float xLimit = 10f;

    [Header("Episode Phases")]
    [Tooltip("Number of decisions the agent has to position the bucket before the ball fires")]
    public int positioningSteps = 15;

    [Header("Rewards")]
    [Tooltip("Reward when the ball is caught")]
    public float catchReward = 1f;
    [Tooltip("Maximum penalty on a miss (multiplied by normalised distance from landing spot)")]
    public float missMaxPenalty = 1f;

    [Header("Observation Normalisation")]
    [Tooltip("Should match launcher.maxAngle so angle observation stays in [0, 1]")]
    public float maxAngle = 70f;
    [Tooltip("Should match launcher.maxPower so power observation stays in [0, 1]")]
    public float maxPower = 20f;

    private Rigidbody _rb;
    private Vector3 _startPosition;
    private Projectile _activeProjectile;
    private int _positioningDecisions;
    private bool _ballFired;
    private bool _episodeActive;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void Initialize()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _startPosition = transform.position;

        if (launcher != null)
            launcher.onProjectileLaunched += OnProjectileLaunched;
    }

    private void OnDestroy()
    {
        if (launcher != null)
            launcher.onProjectileLaunched -= OnProjectileLaunched;
    }

    public override void OnEpisodeBegin()
    {
        _rb.position = _startPosition;
        _activeProjectile = null;
        _positioningDecisions = 0;
        _ballFired = false;
        _episodeActive = true;

        // Randomise shot parameters so the agent can observe them — ball stays offscreen.
        launcher.PrepareShot();
    }

    private void OnProjectileLaunched(Projectile projectile)
    {
        _activeProjectile = projectile;
        // Append after launcher's own onResult so launcher resets _readyToFire first.
        projectile.onResult += OnProjectileResult;
    }

    private void OnProjectileResult(bool caught)
    {
        if (!_episodeActive) return;
        _episodeActive = false;

        if (caught)
        {
            AddReward(catchReward);
        }
        else
        {
            // Projectile.Destroy is deferred — position is still valid at this point.
            float landingX = _activeProjectile != null ? _activeProjectile.transform.position.x : 0f;
            float distance = Mathf.Abs(_rb.position.x - landingX);
            float normalised = Mathf.Clamp01(distance / (2f * xLimit));
            AddReward(-missMaxPenalty * normalised);
        }

        // Clear before EndEpisode so the synchronous OnEpisodeBegin → PrepareShot chain
        // doesn't leave a stale reference when the next episode starts.
        _activeProjectile = null;
        EndEpisode();
    }

    // ── Observations (3) ──────────────────────────────────────────────────────

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(_rb.position.x / xLimit);             // bucket X   [-1, 1]
        sensor.AddObservation(launcher.PreparedAngle / maxAngle);   // angle      [ 0, 1]
        sensor.AddObservation(launcher.PreparedPower / maxPower);   // power      [ 0, 1]
    }

    // ── Actions (1 continuous: move direction [-1 = left, +1 = right]) ────────

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Bucket is locked once the ball is in the scene.
        if (!_episodeActive || _ballFired) return;

        float moveDir = actions.ContinuousActions[0];
        float newX = Mathf.Clamp(_rb.position.x + moveDir * movePerDecision, -xLimit, xLimit);
        _rb.MovePosition(new Vector3(newX, _rb.position.y, _rb.position.z));

        _positioningDecisions++;
        if (_positioningDecisions >= positioningSteps)
        {
            _ballFired = true;
            launcher.FirePrepared();
            // FirePrepared calls onProjectileLaunched synchronously, so _activeProjectile
            // is set and the result callback is wired before this line returns.
        }
    }

    // Left/right arrow keys move the bucket for play-mode testing without a trained model.
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        actionsOut.ContinuousActions.Array[actionsOut.ContinuousActions.Offset] =
            Input.GetAxis("Horizontal");
    }
}
