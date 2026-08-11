using UnityEngine;

/// <summary>
/// Alarm Kebakaran Dapur (Smoke Detector / Fire Alarm).
/// Mengontrol lampu indikator merah berkedip & sirene/alarm saat ada api aktif.
/// Otomatis mati ketika api berhasil dipadamkan.
/// </summary>
public class FireAlarmSystem : MonoBehaviour
{
    [Header("Referensi")]
    [Tooltip("Target api yang dipantau")]
    public FireExtinguisherTarget targetFire;

    [Header("Lampu Alarm Merah")]
    public Light alarmLight;
    public float flashSpeed = 4.0f;
    public float maxIntensity = 4.0f;

    [Header("Audio Alarm")]
    public AudioClip alarmAudioClip;
    [Range(0f, 1f)] public float alarmVolume = 0.7f;

    private AudioSource audioSource;
    private bool isAlarmActive = true;

    private void Awake()
    {
        if (targetFire == null)
            targetFire = FindFirstObjectByType<FireExtinguisherTarget>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.clip = alarmAudioClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = alarmVolume;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 10f;
    }

    private void Start()
    {
        if (alarmLight != null)
        {
            alarmLight.color = Color.red;
            alarmLight.type = LightType.Point;
        }

        if (isAlarmActive && audioSource != null && alarmAudioClip != null)
        {
            audioSource.Play();
        }
    }

    private void Update()
    {
        // Cek status api
        if (targetFire != null && !targetFire.gameObject.activeInHierarchy)
        {
            StopAlarm();
            return;
        }

        if (isAlarmActive && alarmLight != null)
        {
            // Flash lampu merah sinus
            float pingpong = Mathf.PingPong(Time.time * flashSpeed, 1.0f);
            alarmLight.intensity = Mathf.Lerp(0.2f, maxIntensity, pingpong);
        }
    }

    public void StopAlarm()
    {
        isAlarmActive = false;
        if (alarmLight != null) alarmLight.intensity = 0f;
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
    }

    public void StartAlarm()
    {
        isAlarmActive = true;
        if (audioSource != null && alarmAudioClip != null && !audioSource.isPlaying) audioSource.Play();
    }
}
