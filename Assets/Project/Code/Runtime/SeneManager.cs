using UnityEngine;
using UnityEngine.SceneManagement;

public class SeneManager : MonoBehaviour
{
    [Header("Testing")]
    [Tooltip("If true, loads the Sandbox_Logan scene on start for testing purposes.")]
    public bool loadSandboxLoganOnStart = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (loadSandboxLoganOnStart)
        {
            LoadScene(SceneNames.TestScene);
        }
    }

    // Loads a scene by name
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Reloads the current active scene
    public void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    // Loads the next scene in the build settings (if available)
    public void LoadNextScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.LogWarning("No next scene in build settings.");
        }
    }

    // Quits the application
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}

public static class SceneNames
{
    public const string TestScene = "Sandbox_Logan";
}