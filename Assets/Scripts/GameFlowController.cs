using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public struct GameStage
{
    public string sceneName;
    [Tooltip("Seconds of game time for this stage (editable per-stage)")]
    public float duration;
}

/// Persistent singleton that sequences the player through all mini-game stages.
/// Attach to the MainMenu EventSystem alongside GameScoreManager — DontDestroyOnLoad
/// keeps it alive across scene changes.
///
/// Each game scene must have a TMP_Text tagged "GameTimer" (create the tag in
/// Edit → Project Settings → Tags & Layers). The timer text is found dynamically
/// after any SceneCountdown in that scene finishes, so the tag just needs to exist
/// on whatever text box is already in the scene.
public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    [Tooltip("Mini-game stages to play through, in order")]
    public GameStage[] stages = new GameStage[]
    {
        new GameStage { sceneName = "Pendulum",        duration = 30f },
        new GameStage { sceneName = "LunarLanding",    duration = 30f },
        new GameStage { sceneName = "ProjectileCatch", duration = 30f },
    };

    [Tooltip("Scene to load when all stages have been played")]
    public string finalScene = "StatsMenu";

    [Tooltip("Tag on the TMP_Text that should display the remaining game time in each scene")]
    public string timerTextTag = "GameTimer";

    private Coroutine _timerCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_timerCoroutine != null)
            StopCoroutine(_timerCoroutine);

        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i].sceneName == scene.name)
            {
                _timerCoroutine = StartCoroutine(RunStage(stages[i].duration, i));
                return;
            }
        }
    }

    IEnumerator RunStage(float duration, int stageIndex)
    {
        // If a SceneCountdown exists, wait for it to finish before starting the timer.
        // sceneLoaded fires before Start() runs, so yield once to let SceneCountdown.Start()
        // set timeScale = 0, then wait until SceneCountdown restores it to 1.
        SceneCountdown countdown = FindFirstObjectByType<SceneCountdown>();
        if (countdown != null)
        {
            yield return null; // let SceneCountdown.Start() run
            yield return new WaitUntil(() => Time.timeScale > 0f);
            yield return null; // one extra frame for the panel to deactivate
        }

        TMP_Text timerText = FindTimerText();

        float remaining = duration;
        while (remaining > 0f)
        {
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(remaining).ToString();
            yield return null;
            remaining -= Time.deltaTime;
        }

        if (timerText != null)
            timerText.text = "0";

        if (stageIndex + 1 < stages.Length)
            SceneManager.LoadScene(stages[stageIndex + 1].sceneName);
        else
            SceneManager.LoadScene(finalScene);
    }

    TMP_Text FindTimerText()
    {
        GameObject obj = GameObject.FindGameObjectWithTag(timerTextTag);
        return obj != null ? obj.GetComponent<TMP_Text>() : null;
    }
}
