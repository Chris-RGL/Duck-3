using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Singleton pattern so we can access this from anywhere
    public static GameManager Instance;

    public int humanScore = 0;
    public int aiScore = 0;

    private void Awake()
    {
        // Ensure only one GameManager exists and it survives scene loads
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Moves to the next scene in your Build Settings list
    public void LoadNextLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
    }

    // Wipes stats and goes back to Main Menu
    public void ResetGame()
    {
        humanScore = 0;
        aiScore = 0;
        SceneManager.LoadScene(0);
    }
}