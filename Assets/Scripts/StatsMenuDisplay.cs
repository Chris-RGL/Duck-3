using TMPro;
using UnityEngine;

/// Attach to any GameObject in the StatsMenu scene.
/// Wire each serialized TMP_Text field to its matching table cell in the Canvas.
/// Reads results from GameScoreManager (populated by each game scene on unload).
public class StatsMenuDisplay : MonoBehaviour
{
    [Header("Pendulum Row")]
    public TMP_Text pendulumPlayerText;
    public TMP_Text pendulumAgentText;

    [Header("Lunar Landing Row")]
    public TMP_Text lunarPlayerText;
    public TMP_Text lunarAgentText;

    [Header("Projectile Catch Row")]
    public TMP_Text catcherPlayerText;
    public TMP_Text catcherAgentText;

    void Start()
    {
        var mgr = GameScoreManager.Instance;
        if (mgr == null) return;

        pendulumPlayerText.text = $"{mgr.Pendulum.playerScore:F1}";
        pendulumAgentText.text  = $"{mgr.Pendulum.agentScore:F1}";

        lunarPlayerText.text = $"{mgr.Lunar.playerScore:F1}";
        lunarAgentText.text  = $"{mgr.Lunar.agentScore:F1}";

        catcherPlayerText.text = FormatCatches(mgr.Catcher.playerCatches, mgr.Catcher.playerRoundsPlayed);
        catcherAgentText.text  = FormatCatches(mgr.Catcher.agentCatches,  mgr.Catcher.agentRoundsPlayed);
    }

    static string FormatCatches(int catches, int rounds) =>
        rounds > 0 ? $"{catches}/{rounds}" : "–";
}
