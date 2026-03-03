using System.Collections;
using UnityEngine;

/// <summary>
/// Plays background music on the menu with fade in on start and fade out on scene unload.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MenuMusic : MonoBehaviour
{
    [Header("Fade Settings")]
    [Tooltip("Duration of the fade in at startup, in seconds.")]
    public float fadeInDuration  = 1.5f;
    [Tooltip("Duration of the fade out when leaving the menu, in seconds.")]
    public float fadeOutDuration = 1.0f;
    [Tooltip("Target volume at full intensity.")]
    [Range(0f, 1f)]
    public float targetVolume = 1f;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.loop   = true;
        _audioSource.volume = 0f;
        _audioSource.Play();
    }

    private void Start()
    {
        StartCoroutine(Fade(_audioSource, 0f, targetVolume, fadeInDuration));
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// Fades out the music then loads the target scene.
    /// Call this before any scene transition to get a smooth fade out.
    /// </summary>
    public void FadeOutAndLoadScene(string sceneName)
    {
        StartCoroutine(FadeOutThenLoad(sceneName));
    }

    private IEnumerator FadeOutThenLoad(string sceneName)
    {
        yield return Fade(_audioSource, _audioSource.volume, 0f, fadeOutDuration);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    private static IEnumerator Fade(AudioSource source, float from, float to, float duration)
    {
        float elapsed = 0f;
        source.volume = from;

        while (elapsed < duration)
        {
            elapsed      += Time.deltaTime;
            source.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        source.volume = to;
    }
}
