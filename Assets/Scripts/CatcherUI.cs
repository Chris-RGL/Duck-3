using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Attach to any scene GameObject.
/// Drives the Catcher scene's canvas: bucket slider during player rounds, fire button,
/// agent/player score displays, and a shot-info panel showing angle and power.
///
/// Wire-up in Inspector:
///   Agent          → the bucket's CatcherAgent component
///   BucketRigidbody → the bucket's kinematic Rigidbody
///   BucketSlider   → UI Slider (min/max set automatically from agent.xLimit)
///   FireButton     → UI Button; disabled outside of player phase
///   AgentScoreText → TMP_Text showing agent catches
///   PlayerScoreText → TMP_Text showing player catches
///   ShotInfoText   → TMP_Text showing angle + power during player rounds
public class CatcherUI : MonoBehaviour
{
    [Header("References")]
    public CatcherAgent agent;
    public Rigidbody bucketRigidbody;

    [Header("UI Elements")]
    public Slider bucketSlider;
    public Button fireButton;
    public TMP_Text agentScoreText;
    public TMP_Text playerScoreText;
    public TMP_Text shotInfoText;

    void Start()
    {
        if (bucketSlider != null && agent != null)
        {
            bucketSlider.minValue = -agent.xLimit;
            bucketSlider.maxValue =  agent.xLimit;
            bucketSlider.value    =  0f;
        }

        if (fireButton != null)
            fireButton.onClick.AddListener(OnFireButtonClicked);
    }

    void Update()
    {
        if (agent == null) return;

        bool playerPhaseActive = agent.IsPlayerPhaseActive;

        if (bucketSlider != null)
            bucketSlider.interactable = playerPhaseActive;

        if (fireButton != null)
            fireButton.interactable = playerPhaseActive;

        if (agentScoreText != null)
            agentScoreText.text = $"Agent\n{agent.AgentCatches}/{agent.AgentRoundsTotal}";

        if (playerScoreText != null)
            playerScoreText.text = $"Player\n{agent.PlayerCatches}/{agent.PlayerRoundsTotal}";

        if (shotInfoText != null)
            shotInfoText.text = $"Angle: {agent.PreparedAngle:F1}°\nPower: {agent.PreparedPower:F1}";
    }

    void FixedUpdate()
    {
        // Move the bucket via MovePosition so physics interactions remain consistent.
        if (agent == null || !agent.IsPlayerPhaseActive || bucketRigidbody == null || bucketSlider == null)
            return;

        float targetX = Mathf.Clamp(bucketSlider.value, -agent.xLimit, agent.xLimit);
        Vector3 pos = bucketRigidbody.position;
        pos.x = targetX;
        bucketRigidbody.MovePosition(pos);
    }

    void OnFireButtonClicked()
    {
        agent?.OnPlayerFireButtonPressed();
    }
}
