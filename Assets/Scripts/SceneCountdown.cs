using System.Collections;
using TMPro;
using UnityEngine;

/// Attach to a UI panel in any scene. On Start it freezes time, counts down
/// 3-2-1-GO! via a TMP_Text, then restores time and hides the panel.
/// Because WaitForSecondsRealtime ignores timeScale, the countdown runs at
/// wall-clock speed even though the game is paused.
public class SceneCountdown : MonoBehaviour
{
    [Tooltip("TMP_Text that displays the countdown numbers and GO!")]
    public TMP_Text countdownText;

    [Tooltip("Panel GameObject to hide after GO! — leave empty to hide only the countdown text")]
    public GameObject panel;

    [Tooltip("Seconds GO! stays visible before hiding")]
    public float goDisplayDuration = 0.5f;

    void Start()
    {
        Time.timeScale = 0f;
        StartCoroutine(RunCountdown());
    }

    IEnumerator RunCountdown()
    {
        for (int i = 3; i >= 1; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSecondsRealtime(1f);
        }

        countdownText.text = "GO!";
        Time.timeScale = 1f;

        yield return new WaitForSecondsRealtime(goDisplayDuration);

        // Hide only the explicit panel if set; otherwise hide just the text so we
        // never accidentally deactivate the EventSystem or a canvas that owns game UI.
        if (panel != null)
            panel.SetActive(false);
        else
            countdownText.gameObject.SetActive(false);
    }
}
