using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SceneBgmPlayer : MonoBehaviour
{
    [Header("BGM")]
    public AudioClip musicClip;
    [Range(0f, 1f)]
    public float volume = 0.35f;
    public bool playOnStart = true;
    public bool loop = true;

    [Header("Output")]
    [Tooltip("0 keeps BGM as normal stereo music. Use 3D only for diegetic speakers in the scene.")]
    [Range(0f, 1f)]
    public float spatialBlend = 0f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ApplySettings();
    }

    private void Start()
    {
        if (playOnStart && musicClip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void OnValidate()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            ApplySettings();
        }
    }

    public void Play()
    {
        if (musicClip == null)
        {
            return;
        }

        ApplySettings();
        audioSource.Play();
    }

    public void Stop()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private void ApplySettings()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.clip = musicClip;
        audioSource.volume = volume;
        audioSource.loop = loop;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = spatialBlend;
    }
}
