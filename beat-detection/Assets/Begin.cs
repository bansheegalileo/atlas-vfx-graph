using UnityEngine;

public class TriggerMusicAndBeatFX2D : MonoBehaviour
{
    [Header("Scene Setup")]
    public GameObject objectToActivate;

    [Header("Audio")]
    public AudioSource musicSource;         // music to analyze (used by beat detector)

    [Header("Optional FX Setup")]
    public GameObject beatDetectionController; // GameObject with PerBandParticleTrigger script
    public float delayBeforeMusic = 0.5f;

    private bool hasTriggered = false;

    private void Start()
    {
        // Validate setup
        if (musicSource == null)
        {
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;

        if (objectToActivate != null)
            objectToActivate.SetActive(true);

        Invoke(nameof(StartMusicAndFX), delayBeforeMusic);
    }

    private void StartMusicAndFX()
    {
        if (musicSource == null)
        {
            return;
        }

        if (musicSource.clip == null)
        {
            return;
        }

        if (!musicSource.enabled)
        {
            return;
        }

        if (!musicSource.gameObject.activeInHierarchy)
        {
            return;
        }

        // All checks passed, play the music
        musicSource.Play();

        if (beatDetectionController != null)
        {
            beatDetectionController.SetActive(true);
        }
    }
}
