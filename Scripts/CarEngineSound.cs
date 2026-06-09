using UnityEngine;

public class CarEngineSound : MonoBehaviour
{
    [Header("Rigidbody")]
    public Rigidbody carRb;

    [Header("Audio Clips")]
    public AudioClip startupClip;
    public AudioClip idleClip;
    public AudioClip lowOnClip;
    public AudioClip lowOffClip;
    public AudioClip medOnClip;
    public AudioClip medOffClip;
    public AudioClip highOnClip;
    public AudioClip highOffClip;
    public AudioClip maxRpmClip;

    [Header("Input")]
    [Range(0f, 1f)]
    public float throttleInput;

    [Header("Speed Settings")]
    public float lowSpeed = 20f;
    public float mediumSpeed = 70f;
    public float highSpeed = 120f;
    public float maxSpeed = 160f;

    [Header("Blend Settings")]
    public float idleFadeOutSpeed = 15f;
    public float maxRpmStartSpeed = 130f;
    public float maxRpmFullSpeed = 160f;

    [Header("Sound Settings")]
    public float fadeSpeed = 8f;
    public float minPitch = 0.85f;
    public float maxPitch = 1.8f;
    public float engineVolume = 1f;
    [Range(0f, 2f)]
    public float masterVolumeMultiplier = 1f;
    public float spatialBlend = 0.7f;

    [Header("3D Distance Attenuation")]
    public bool useDistanceAttenuation = true;
    public float minDistance = 4f;
    public float maxDistance = 45f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
    [Range(0f, 5f)]
    public float dopplerLevel = 0.35f;
    [Range(0f, 360f)]
    public float spread = 45f;

    [Header("Opponent Audio")]
    public float opponentVolumeMultiplier = 0.45f;
    public float opponentMaxDistance = 38f;

    [Header("Throttle Sound Response")]
    public float throttleResponse = 1.5f;
    public float offVolumeMultiplier = 0.65f;
    public float onVolumeMultiplier = 1.0f;

    private AudioSource startupSource;
    private AudioSource idleSource;
    private AudioSource lowOnSource;
    private AudioSource lowOffSource;
    private AudioSource medOnSource;
    private AudioSource medOffSource;
    private AudioSource highOnSource;
    private AudioSource highOffSource;
    private AudioSource maxRpmSource;
    private bool opponentAudioConfigured;

    void Start()
    {
        if (carRb == null)
            carRb = GetComponent<Rigidbody>();

        startupSource = CreateSource("Engine_Startup", startupClip, false);
        idleSource = CreateSource("Engine_Idle", idleClip, true);
        lowOnSource = CreateSource("Engine_LowOn", lowOnClip, true);
        lowOffSource = CreateSource("Engine_LowOff", lowOffClip, true);
        medOnSource = CreateSource("Engine_MedOn", medOnClip, true);
        medOffSource = CreateSource("Engine_MedOff", medOffClip, true);
        highOnSource = CreateSource("Engine_HighOn", highOnClip, true);
        highOffSource = CreateSource("Engine_HighOff", highOffClip, true);
        maxRpmSource = CreateSource("Engine_MaxRPM", maxRpmClip, true);

        if (startupSource != null)
        {
            startupSource.volume = GetScaledEngineVolume();
            startupSource.Play();
        }

        PlayLoop(idleSource);
        PlayLoop(lowOnSource);
        PlayLoop(lowOffSource);
        PlayLoop(medOnSource);
        PlayLoop(medOffSource);
        PlayLoop(highOnSource);
        PlayLoop(highOffSource);
        PlayLoop(maxRpmSource);
    }

    void Update()
    {
        if (carRb == null)
            return;

        float speedKmh = carRb.linearVelocity.magnitude * 3.6f;
        float speedPercent = Mathf.Clamp01(speedKmh / maxSpeed);

        // 엑셀 입력을 더 부드럽게 사용
        float throttle = Mathf.Clamp01(throttleInput);
        float throttleBlend = Mathf.Pow(throttle, throttleResponse);

        // 엑셀을 많이 밟을수록 on 계열이 커지고,
        // 엑셀을 덜 밟거나 떼면 off 계열이 커짐
        float onBlend = throttleBlend;
        float offBlend = 1f - throttleBlend;

        // 정지/저속에서는 idle 유지
        float idleWeight = 1f - Mathf.InverseLerp(0f, idleFadeOutSpeed, speedKmh);

        // 속도별 low / med / high 비율 계산
        float lowWeight = 0f;
        float medWeight = 0f;
        float highWeight = 0f;

        if (speedKmh < mediumSpeed)
        {
            float t = Mathf.InverseLerp(lowSpeed, mediumSpeed, speedKmh);
            lowWeight = 1f - t;
            medWeight = t;
            highWeight = 0f;
        }
        else
        {
            float t = Mathf.InverseLerp(mediumSpeed, highSpeed, speedKmh);
            lowWeight = 0f;
            medWeight = 1f - t;
            highWeight = t;
        }

        // 너무 저속에서는 low가 더 잘 들리게 보정
        if (speedKmh < lowSpeed)
        {
            lowWeight = 1f;
            medWeight = 0f;
            highWeight = 0f;
        }

        // maxRPM은 고속 구간에서만 천천히 섞기
        float maxRpmWeight = Mathf.InverseLerp(maxRpmStartSpeed, maxRpmFullSpeed, speedKmh);
        maxRpmWeight *= onBlend;

        // Pitch도 속도 + 엑셀에 따라 자연스럽게 변화
        float basePitch = Mathf.Lerp(minPitch, maxPitch, speedPercent);
        float throttlePitchAdd = Mathf.Lerp(0f, 0.15f, throttleBlend);
        float finalPitch = basePitch + throttlePitchAdd;

        SetPitch(idleSource, Mathf.Lerp(0.85f, 1.05f, speedPercent));
        SetPitch(lowOnSource, finalPitch);
        SetPitch(lowOffSource, finalPitch * 0.95f);
        SetPitch(medOnSource, finalPitch);
        SetPitch(medOffSource, finalPitch * 0.95f);
        SetPitch(highOnSource, finalPitch);
        SetPitch(highOffSource, finalPitch * 0.95f);
        SetPitch(maxRpmSource, Mathf.Lerp(1.0f, 1.4f, maxRpmWeight));

        // 최종 볼륨
        float idleVol = idleWeight * 0.75f;

        float lowOnVol = lowWeight * onBlend * onVolumeMultiplier;
        float lowOffVol = lowWeight * offBlend * offVolumeMultiplier;

        float medOnVol = medWeight * onBlend * onVolumeMultiplier;
        float medOffVol = medWeight * offBlend * offVolumeMultiplier;

        float highOnVol = highWeight * onBlend * onVolumeMultiplier;
        float highOffVol = highWeight * offBlend * offVolumeMultiplier;

        float maxRpmVol = maxRpmWeight * 0.8f;

        // 정지 상태에서 엑셀 밟으면 idle + low_on이 같이 들리게
        if (speedKmh < 3f && throttle > 0.1f)
        {
            idleVol = 0.45f;
            lowOnVol = throttleBlend * 0.8f;
            lowOffVol = 0f;
        }

        float scaledEngineVolume = GetScaledEngineVolume();

        FadeVolume(idleSource, idleVol * scaledEngineVolume);
        FadeVolume(lowOnSource, lowOnVol * scaledEngineVolume);
        FadeVolume(lowOffSource, lowOffVol * scaledEngineVolume);
        FadeVolume(medOnSource, medOnVol * scaledEngineVolume);
        FadeVolume(medOffSource, medOffVol * scaledEngineVolume);
        FadeVolume(highOnSource, highOnVol * scaledEngineVolume);
        FadeVolume(highOffSource, highOffVol * scaledEngineVolume);
        FadeVolume(maxRpmSource, maxRpmVol * scaledEngineVolume);
    }

    AudioSource CreateSource(string sourceName, AudioClip clip, bool loop)
    {
        if (clip == null)
            return null;

        GameObject audioObj = new GameObject(sourceName);
        audioObj.transform.SetParent(transform);
        audioObj.transform.localPosition = Vector3.zero;

        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.volume = 0f;
        ApplyDistanceSettings(source);

        return source;
    }

    void ApplyDistanceSettings(AudioSource source)
    {
        if (source == null)
            return;

        source.spatialBlend = useDistanceAttenuation ? Mathf.Max(spatialBlend, 0.95f) : spatialBlend;
        source.rolloffMode = rolloffMode;
        source.minDistance = Mathf.Max(0.01f, minDistance);
        source.maxDistance = Mathf.Max(source.minDistance + 0.01f, maxDistance);
        source.dopplerLevel = dopplerLevel;
        source.spread = spread;
    }

    void PlayLoop(AudioSource source)
    {
        if (source == null)
            return;

        source.loop = true;
        source.volume = 0f;

        if (!source.isPlaying)
            source.Play();
    }

    void FadeVolume(AudioSource source, float targetVolume)
    {
        if (source == null)
            return;

        source.volume = Mathf.Lerp(
            source.volume,
            targetVolume,
            Time.deltaTime * fadeSpeed
        );
    }

    void SetPitch(AudioSource source, float pitch)
    {
        if (source == null)
            return;

        source.pitch = pitch;
    }

    public void SetThrottle(float value)
    {
        throttleInput = Mathf.Clamp01(value);
    }

    public void SetMasterVolume(float value)
    {
        masterVolumeMultiplier = Mathf.Clamp(value, 0f, 2f);
    }

    public void StopEngineAudio()
    {
        throttleInput = 0f;
        StopSource(startupSource);
        StopSource(idleSource);
        StopSource(lowOnSource);
        StopSource(lowOffSource);
        StopSource(medOnSource);
        StopSource(medOffSource);
        StopSource(highOnSource);
        StopSource(highOffSource);
        StopSource(maxRpmSource);
    }

    private float GetScaledEngineVolume()
    {
        return engineVolume * masterVolumeMultiplier;
    }

    private static void StopSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.volume = 0f;
        source.Stop();
    }

    public void ConfigureAsOpponentAudio()
    {
        if (!opponentAudioConfigured)
        {
            engineVolume *= opponentVolumeMultiplier;
            opponentAudioConfigured = true;
        }

        spatialBlend = 1f;
        useDistanceAttenuation = true;
        maxDistance = opponentMaxDistance;
        rolloffMode = AudioRolloffMode.Linear;
        dopplerLevel = Mathf.Min(dopplerLevel, 0.25f);

        ApplyDistanceSettings(startupSource);
        ApplyDistanceSettings(idleSource);
        ApplyDistanceSettings(lowOnSource);
        ApplyDistanceSettings(lowOffSource);
        ApplyDistanceSettings(medOnSource);
        ApplyDistanceSettings(medOffSource);
        ApplyDistanceSettings(highOnSource);
        ApplyDistanceSettings(highOffSource);
        ApplyDistanceSettings(maxRpmSource);
    }
}
