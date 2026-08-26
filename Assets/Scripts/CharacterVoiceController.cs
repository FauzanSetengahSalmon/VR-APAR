using UnityEngine;

public class CharacterVoiceController : MonoBehaviour
{
    private AudioSource audioSource;
    private Animator animator;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Dipanggil saat api padam untuk menghentikan suara dan animasi panik.
    /// </summary>
    public void StopPanicSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Opsional: jika ingin mengembalikan animasi ke Idle/Tenang
        if (animator != null)
        {
            // animator.SetTrigger("Calm"); // sesuaikan dengan parameter Animator kamu
        }
    }
}