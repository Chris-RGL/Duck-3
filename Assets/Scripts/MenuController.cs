using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public void OnStartOrNextClicked()
    {
        if (GameManager.Instance != null)
        {
            // Advance the level index
            GameManager.Instance.LoadNextLevel();

            // Subscribe to the scene loaded event so we can fix the cameras 
            // the exact millisecond the new level finishes loading
            SceneManager.sceneLoaded += OnLevelLoadedConfiguration;
        }
    }

    private void OnLevelLoadedConfiguration(Scene scene, LoadSceneMode mode)
    {
        // Unsubscribe immediately so it only runs once
        SceneManager.sceneLoaded -= OnLevelLoadedConfiguration;

        // Find the gameplay cameras in the freshly loaded scene
        Camera humanCam = GameObject.Find("HumanCamera")?.GetComponent<Camera>();
        Camera aiCam = GameObject.Find("AICamera")?.GetComponent<Camera>();

        // Find the main menu camera if it's lingering and kill it
        GameObject oldMenuCam = GameObject.Find("Main Camera");
        if (oldMenuCam != null && (humanCam != null || aiCam != null))
        {
            Destroy(oldMenuCam);
        }

        // Force your split-screen layout metrics via code to be 100% sure
        if (humanCam != null)
        {
            humanCam.rect = new Rect(0f, 0f, 0.5f, 1f);
        }

        if (aiCam != null)
        {
            aiCam.rect = new Rect(0.5f, 0f, 0.5f, 1f);
        }
    }

    public void OnRestartClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
        }
    }
}