using UnityEngine;

public struct PendulumStats
{
    public float playerScore;
    public float agentScore;
}

public struct LunarStats
{
    public float playerScore;
    public float agentScore;
}

public struct CatcherStats
{
    public int playerCatches;
    public int agentCatches;
    public int playerRoundsPlayed;
    public int agentRoundsPlayed;
}

/// Persistent singleton that accumulates scores and per-game results across all scenes.
/// Attach to the EventSystem (or any persistent root GameObject) in your first scene.
/// DontDestroyOnLoad keeps it alive as scenes change; duplicate instances self-destruct.
public class GameScoreManager : MonoBehaviour
{
    public static GameScoreManager Instance { get; private set; }

    public float TotalScore { get; private set; }

    public PendulumStats Pendulum { get; private set; }
    public LunarStats    Lunar    { get; private set; }
    public CatcherStats  Catcher  { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddScore(float delta) => TotalScore += delta;

    public void ResetScore() => TotalScore = 0f;

    public void SetPendulumStats(float player, float agent) =>
        Pendulum = new PendulumStats { playerScore = player, agentScore = agent };

    public void SetLunarStats(float player, float agent) =>
        Lunar = new LunarStats { playerScore = player, agentScore = agent };

    public void SetCatcherStats(int playerCatches, int agentCatches,
                                int playerRoundsPlayed, int agentRoundsPlayed) =>
        Catcher = new CatcherStats
        {
            playerCatches      = playerCatches,
            agentCatches       = agentCatches,
            playerRoundsPlayed = playerRoundsPlayed,
            agentRoundsPlayed  = agentRoundsPlayed,
        };
}
