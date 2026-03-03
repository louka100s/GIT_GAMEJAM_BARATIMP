using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the main menu: Play and Exit button actions.
/// </summary>
public class MenuController : MonoBehaviour
{
    private const string PLAY_SCENE_NAME = "SampleScene";

    [SerializeField] private MenuMusic menuMusic;

    /// <summary>
    /// Fades out music then loads the game scene.
    /// </summary>
    public void OnPlayPressed()
    {
        if (menuMusic != null)
            menuMusic.FadeOutAndLoadScene(PLAY_SCENE_NAME);
        else
            SceneManager.LoadScene(PLAY_SCENE_NAME);
    }

    /// <summary>
    /// Quits the application.
    /// </summary>
    public void OnExitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
