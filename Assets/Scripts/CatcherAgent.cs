using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// Attach to the bucket alongside a kinematic Rigidbody and DecisionRequester.
/// BehaviorParameters: Behavior Name = "Catcher", Vector Observations = 3, Continuous Actions = 1.
/// Set launcher.agentControlled = true. Tag the bucket GameObject "Bucket".
///
/// Episode flow:
///   Agent rounds  — agent has positioningSteps decisions to slide the bucket along X, then auto-fires.
///   Player rounds — bucket is driven by CatcherUI slider; player presses the UI fire button.
///   After agentRounds + playerRounds episodes the scores reset and the cycle repeats.
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

    [Header("Game Rounds")]
    [Tooltip("Number of rounds the ML agent controls the bucket before handing off to the player")]
    public int agentRounds = 3;
    [Tooltip("Number of rounds the player controls the bucket after agent rounds")]
    public int playerRounds = 3;

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

    private int _roundNumber;
    private int _agentCatches;
    private int _playerCatches;

    // ── Public state for CatcherUI ────────────────────────────────────────────

    public bool IsPlayerRound => _roundNumber >= agentRounds;
    /// True when the player can interact: their round, episode active, no ball in flight yet.
    public bool IsPlayerPhaseActive => IsPlayerRound && _episodeActive && !_ballFired;
    public int AgentCatches => _agentCatches;
    public int PlayerCatches => _playerCatches;
    public int AgentRoundsTotal => agentRounds;
    public int PlayerRoundsTotal => playerRounds;
    public float PreparedAngle => launcher != null ? launcher.PreparedAngle : 0f;
    public float PreparedPower => launcher != null ? launcher.PreparedPower : 0f;

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
        // Reset after completing a full cycle of agent + player rounds.
        if (_roundNumber >= agentRounds + playerRounds)
        {
            _roundNumber = 0;
            _agentCatches = 0;
            _playerCatches = 0;
        }

        if (_activeProjectile != null)
        {
            _activeProjectile.onResult -= OnProjectileResult;
            _activeProjectile = null;
        }

        _rb.position = _startPosition;
        _positioningDecisions = 0;
        _ballFired = false;
        _episodeActive = true;

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

        if (IsPlayerRound)
        {
            if (caught) _playerCatches++;
        }
        else
        {
            if (caught)
            {
                AddReward(catchReward);
                _agentCatches++;
            }
            else
            {
                float landingX = _activeProjectile != null ? _activeProjectile.transform.position.x : 0f;
                float distance = Mathf.Abs(_rb.position.x - landingX);
                float normalised = Mathf.Clamp01(distance / (2f * xLimit));
                AddReward(-missMaxPenalty * normalised);
            }
        }

        _roundNumber++;
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
        if (!_episodeActive || _ballFired) return;

        // Player rounds are driven by the CatcherUI slider and fire button.
        if (IsPlayerRound) return;

        float moveDir = actions.ContinuousActions[0];
        float newX = Mathf.Clamp(_rb.position.x + moveDir * movePerDecision, -xLimit, xLimit);
        _rb.MovePosition(new Vector3(newX, _rb.position.y, _rb.position.z));

        _positioningDecisions++;
        if (_positioningDecisions >= positioningSteps)
        {
            _ballFired = true;
            launcher.FirePrepared();
        }
    }

    // ── Player fire (called by CatcherUI fire button) ─────────────────────────

    public void OnPlayerFireButtonPressed()
    {
        if (!IsPlayerPhaseActive) return;
        _ballFired = true;
        launcher.FirePrepared();
    }

    // Left/right arrow keys move the bucket for play-mode testing without a trained model.
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        actionsOut.ContinuousActions.Array[actionsOut.ContinuousActions.Offset] =
            Input.GetAxis("Horizontal");
    }
}
