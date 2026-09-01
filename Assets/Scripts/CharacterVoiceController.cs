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
    [Tooltip("Apakah suara panik diulang (loop) hingga api padam")]
    public bool ulangSuaraPanik = true;

    private AudioSource audioSource;
    private Animator animator;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

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
        // Jika sedang memutar suara panik (baik loop maupun sekali putar),
        // langsung ganti ke klip bahasa yang baru.
        // Catatan: sengaja TIDAK mensyaratkan audioSource.loop == true, karena
        // kalau "ulangSuaraPanik" di-uncheck (loop = false), syarat lama membuat
        // suara tidak akan pernah ikut berganti bahasa selama masih diputar.
        bool isPlayingPanicClip = audioSource != null && audioSource.isPlaying &&
            (audioSource.clip == suaraPanikIndonesia || audioSource.clip == suaraPanikInggris);

        if (isPlayingPanicClip)
        {
            PlayPanicSound();
        }
    }

    /// <summary>
    /// Memutar suara panik sesuai bahasa aktif (Indonesia / Inggris).
    /// </summary>
    public void PlayPanicSound()
    {
        if (audioSource == null) return;

        bool isEnglish = VRLanguageManager.IsEnglish;
        AudioClip clipToPlay = isEnglish ? suaraPanikInggris : suaraPanikIndonesia;

        // Fallback jika salah satu slot kosong
        if (clipToPlay == null)
            clipToPlay = isEnglish ? suaraPanikIndonesia : suaraPanikInggris;

        if (clipToPlay != null)
        {
            audioSource.clip = clipToPlay;
            audioSource.loop = ulangSuaraPanik;
            audioSource.volume = 1f;
            audioSource.Play();
        }
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
            audioSource.volume = 1f;
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    /// <summary>
    /// Menghentikan suara panik.
    /// </summary>
    public void StopPanicSound()
    {
        StopAllCoroutines();
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.volume = 1f;
        }
    }
}