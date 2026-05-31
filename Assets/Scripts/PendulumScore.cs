using UnityEngine;
using TMPro;

/// Attach to any scene GameObject.
/// Reads the same reward formula used by PendulumAgent and displays a running
/// score for both a player-controlled and an agent-controlled pendulum.
public class PendulumScore : MonoBehaviour
{
    [Header("Score Display")]
    [Tooltip("TMP text element that shows the player's running score")]
    public TMP_Text playerScoreText;
    [Tooltip("TMP text element that shows the agent's running score")]
    public TMP_Text agentScoreText;

    [Header("Pendulum Cart GameObjects")]
    [Tooltip("Cart (sphere with Rigidbody + PendulumController) of the player pendulum")]
    public GameObject playerPendulum;
    [Tooltip("Cart (sphere with Rigidbody + PendulumController) of the agent pendulum")]
    public GameObject agentPendulum;

    [Header("Reward Weights — keep in sync with PendulumAgent")]
    public float aliveRewardPerStep  = 0.01f;
    public float angleRewardScale    = 1f;
    public float positionRewardScale = 0.3f;
    public float fallPenalty         = 1f;

    private Rigidbody          _playerCartRb;
    private PendulumController _playerCtrl;
    private Rigidbody          _agentCartRb;
    private PendulumController _agentCtrl;

    private float _playerScore;
    private float _agentScore;

    // Tracks whether each pendulum is currently in a fallen state so the
    // fall penalty is applied only once per fall, not every frame.
    private bool _playerFell;
    private bool _agentFell;

    void Start()
    {
        _playerCartRb = playerPendulum.GetComponent<Rigidbody>();
        _playerCtrl   = playerPendulum.GetComponent<PendulumController>();
        _agentCartRb  = agentPendulum.GetComponent<Rigidbody>();
        _agentCtrl    = agentPendulum.GetComponent<PendulumController>();
    }

    void FixedUpdate()
    {
        float playerDelta = StepReward(_playerCtrl, _playerCartRb, ref _playerFell);
        _playerScore += playerDelta;
        _agentScore  += StepReward(_agentCtrl,  _agentCartRb,  ref _agentFell);

        GameScoreManager.Instance?.AddScore(playerDelta);

        playerScoreText.text = $"Player\n{_playerScore:F1}";
        agentScoreText.text  = $"Agent\n{_agentScore:F1}";
    }

    void OnDestroy()
    {
        GameScoreManager.Instance?.SetPendulumStats(_playerScore, _agentScore);
    }

    float StepReward(PendulumController ctrl, Rigidbody cartRb, ref bool fell)
    {
        float angle   = RodAngle(ctrl);
        float cartPos = cartRb.position.x;

        if (Mathf.Abs(angle) >= 90f)
        {
            if (fell) return 0f;   // already penalised this fall
            fell = true;
            return -fallPenalty;
        }

        fell = false;

        float reward  = aliveRewardPerStep;
        reward += Mathf.Cos(angle * Mathf.Deg2Rad) * angleRewardScale;
        float normPos = Mathf.Abs(cartPos) / ctrl.positionLimit;
        reward += (1f - 2f * normPos) * positionRewardScale;
        return reward;
    }

    // Mirrors GetRodAngleDegrees() in PendulumAgent — 0° = upright.
    float RodAngle(PendulumController ctrl)
    {
        float angle = ctrl.rodRigidbody.rotation.eulerAngles.z;
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
