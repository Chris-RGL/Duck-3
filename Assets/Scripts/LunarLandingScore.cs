using TMPro;
using UnityEngine;

/// Attach to any scene GameObject.
/// Computes and displays a running score for two rockets using the same reward
/// formula as RocketAgent. Works for both agent-controlled and player-controlled
/// rockets regardless of whether RocketAgent is enabled.
public class LunarLandingScore : MonoBehaviour
{
    [Header("Score Display")]
    [Tooltip("TMP text element that shows rocket 1's score")]
    public TMP_Text rocket1ScoreText;
    [Tooltip("TMP text element that shows rocket 2's score")]
    public TMP_Text rocket2ScoreText;

    [Header("Rockets")]
    [Tooltip("GameObject with RocketController on it")]
    public GameObject rocket1;
    [Tooltip("GameObject with RocketController on it")]
    public GameObject rocket2;

    [Header("Reward Weights — keep in sync with RocketAgent")]
    [Tooltip("Per-step reward when angle is near 0")]
    public float alignedRewardPerStep = 0.005f;
    [Tooltip("Scales the cosine-based per-step angle reward (+1 at 0°, approaches -1 at 180°)")]
    public float angleRewardScale = 0.005f;
    [Tooltip("Per-step penalty per degree of drift beyond 5°")]
    public float driftPenaltyPerDegree = 0.0001f;
    [Tooltip("One-shot bonus on a successful upright landing")]
    public float landingBonus = 2f;
    [Tooltip("One-shot penalty on a failed landing")]
    public float failPenalty = 1f;
    [Tooltip("Y position at or below which landing is evaluated")]
    public float landingYThreshold = 0.6f;
    [Tooltip("Max angle (degrees) for a successful landing")]
    public float successAngleThreshold = 15f;

    private RocketController _ctrl1;
    private RocketController _ctrl2;
    private Rigidbody _rb1;
    private Rigidbody _rb2;

    private float _score1;
    private float _score2;
    private bool _landed1;
    private bool _landed2;

    void Start()
    {
        _ctrl1 = rocket1.GetComponent<RocketController>();
        _rb1   = rocket1.GetComponent<Rigidbody>();
        _ctrl2 = rocket2.GetComponent<RocketController>();
        _rb2   = rocket2.GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        _score1 += StepReward(_ctrl1, _rb1, ref _landed1);
        _score2 += StepReward(_ctrl2, _rb2, ref _landed2);

        rocket1ScoreText.text = $"{_score1:F2}";
        rocket2ScoreText.text = $"{_score2:F2}";
    }

    float StepReward(RocketController ctrl, Rigidbody rb, ref bool landed)
    {
        float y     = rb.position.y;
        float angle = ctrl.GetAngleDegrees();

        if (y <= landingYThreshold)
        {
            if (landed) return 0f;
            landed = true;
            return Mathf.Abs(angle) < successAngleThreshold ? landingBonus : -failPenalty;
        }

        landed = false;

        float reward = Mathf.Cos(angle * Mathf.Deg2Rad) * angleRewardScale;
        if (Mathf.Abs(angle) < successAngleThreshold)
            reward += alignedRewardPerStep;
        float drift = Mathf.Max(0f, Mathf.Abs(angle) - 5f);
        if (drift > 0f)
            reward -= drift * driftPenaltyPerDegree;

        return reward;
    }
}
