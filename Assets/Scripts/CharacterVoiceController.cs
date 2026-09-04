using UnityEngine;

/// <summary>
/// Mengontrol suara karakter (Panik dan Kemenangan)
/// Mendukung bahasa Indonesia dan Inggris secara otomatis sesuai pilihan bahasa di VR.
/// </summary>
public class CharacterVoiceController : MonoBehaviour
{
    // Catatan: Instance/singleton SENGAJA dihapus.
    // Kalau script ini dipasang di banyak karakter, singleton lama akan
    // men-Destroy komponen di karakter lain sehingga hanya 1 karakter
    // yang bisa bersuara.
    //
    // Sebagai gantinya, semua CharacterVoiceController yang aktif
    // mendaftarkan diri ke daftar statis "All" di bawah ini. FireManager
    // dan VRSimulationUIManager memakai daftar ini untuk memicu suara
    // di SEMUA karakter sekaligus (misal: semua NPC panik bareng saat
    // MCB dimatikan, semua NPC lega bareng saat api padam).
    public static readonly System.Collections.Generic.List<CharacterVoiceController> All
        = new System.Collections.Generic.List<CharacterVoiceController>();

    [Header("1. Suara Panik Kebakaran")]
    [Tooltip("Audio suara panik/teriak versi Bahasa Indonesia (contoh: Tolong ada kebakaran!)")]
    public AudioClip suaraPanikIndonesia;
    [Tooltip("Audio suara panik/teriak versi Bahasa Inggris (contoh: Help! There's a fire!)")]
    public AudioClip suaraPanikInggris;

    [Header("2. Suara Kemenangan / Api Padam (Optional)")]
    [Tooltip("Audio suara terima kasih/lega versi Bahasa Indonesia")]
    public AudioClip suaraKemenanganIndonesia;
    [Tooltip("Audio suara terima kasih/lega versi Bahasa Inggris")]
    public AudioClip suaraKemenanganInggris;

    [Header("Pengaturan Tambahan")]
    [Tooltip("Nyalakan jika ingin suara panik langsung berbunyi saat scene dimuat. Matikan jika ingin suara panik baru muncul saat timer misi dimulai (MCB dimatikan).")]
    public bool putarPanikOtomatis = false;
    [Tooltip("Apakah suara panik diulang secara berkala dengan jeda hingga api padam")]
    public bool ulangSuaraPanik = true;

    [Header("Jeda & Volume Suara Panik")]
    [Tooltip("Jeda waktu minimal antar teriakan (detik)")]
    public float jedaMinimal = 5.0f;
    [Tooltip("Jeda waktu maksimal antar teriakan (detik)")]
    public float jedaMaksimal = 9.0f;
    [Range(0f, 1f)]
    [Tooltip("Volume suara panik (0.0 - 1.0). Disarankan 0.6 - 0.75 agar tidak terlalu memekakkan telinga.")]
    public float volumePanik = 0.7f;

    private AudioSource audioSource;
    private Animator animator;
    private Coroutine panicRoutine;
    private bool isPanicActive = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Set pengaturan audio 3D agar suara realistis berdasarkan posisi NPC di ruangan VR
        audioSource.spatialBlend = 1.0f;
        audioSource.minDistance = 1.5f;
        audioSource.maxDistance = 20f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        VRLanguageManager.OnLanguageChanged += OnLanguageChanged;

        if (!All.Contains(this))
            All.Add(this);
    }

    private void OnDisable()
    {
        VRLanguageManager.OnLanguageChanged -= OnLanguageChanged;

        All.Remove(this);
        StopPanicSound();
    }

    private void Start()
    {
        if (putarPanikOtomatis)
        {
            PlayPanicSound();
        }
    }

    private void OnLanguageChanged(AppLanguage newLang)
    {
        if (isPanicActive)
        {
            // Reset dan putar kembali dengan bahasa baru
            PlayPanicSound();
        }
    }

    /// <summary>
    /// Memutar suara panik sesuai bahasa aktif (Indonesia / Inggris) dengan jeda waktu natural.
    /// </summary>
    public void PlayPanicSound()
    {
        if (audioSource == null) return;

        isPanicActive = true;

        if (panicRoutine != null)
            StopCoroutine(panicRoutine);

        panicRoutine = StartCoroutine(PanicVoiceRoutine());
    }

    private System.Collections.IEnumerator PanicVoiceRoutine()
    {
        // Beri jeda acak di awal (0.2s - 1.5s) agar jika ada banyak NPC, mereka tidak berteriak serentak di milidetik yang sama
        float initialJitter = Random.Range(0.2f, 1.5f);
        yield return new WaitForSeconds(initialJitter);

        while (isPanicActive)
        {
            bool isEnglish = VRLanguageManager.IsEnglish;
            AudioClip clipToPlay = isEnglish ? suaraPanikInggris : suaraPanikIndonesia;

            // Fallback jika salah satu slot kosong
            if (clipToPlay == null)
                clipToPlay = isEnglish ? suaraPanikIndonesia : suaraPanikInggris;

            if (clipToPlay != null)
            {
                audioSource.clip = clipToPlay;
                audioSource.loop = false;
                audioSource.volume = volumePanik;
                audioSource.Play();

                // Tunggu sampai suara selesai terucap
                yield return new WaitForSeconds(clipToPlay.length);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            if (!ulangSuaraPanik)
            {
                isPanicActive = false;
                break;
            }

            // Berikan jeda hening antar teriakan (misal 5 - 9 detik) agar tidak bising dan realistis
            float jeda = Random.Range(Mathf.Max(1f, jedaMinimal), Mathf.Max(jedaMinimal, jedaMaksimal));
            yield return new WaitForSeconds(jeda);
        }

        panicRoutine = null;
    }

    /// <summary>
    /// Memutar suara terima kasih / lega saat api padam.
    /// </summary>
    public void PlayVictoryVoice()
    {
        if (audioSource == null) return;

        StopPanicSound();

        bool isEnglish = VRLanguageManager.IsEnglish;
        AudioClip clipToPlay = isEnglish ? suaraKemenanganInggris : suaraKemenanganIndonesia;

        if (clipToPlay == null)
            clipToPlay = isEnglish ? suaraKemenanganIndonesia : suaraKemenanganInggris;

        if (clipToPlay != null)
        {
            audioSource.loop = false;
            audioSource.volume = volumePanik;
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    /// <summary>
    /// Menghentikan suara panik.
    /// </summary>
    public void StopPanicSound()
    {
        isPanicActive = false;
        if (panicRoutine != null)
        {
            StopCoroutine(panicRoutine);
            panicRoutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}