using UnityEngine;

public class FireExtinguisherTarget : MonoBehaviour
{
    [Header("Pengaturan Api")]
    public ParticleSystem fireParticle; 
    [Tooltip("Semakin kecil nilainya, semakin lama harus disiram sampai padam")]
    public float extinguishSpeed = 0.2f; 

    private float currentHealth = 1.0f; 
    private Vector3 originalScale;

    void Start()
    {
        if (fireParticle == null)
            fireParticle = GetComponent<ParticleSystem>();

        originalScale = transform.localScale;
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("Smoke"))
        {
            Extinguish();
        }
    }

    void Extinguish()
    {
        currentHealth -= extinguishSpeed * Time.deltaTime;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            
            if (fireParticle != null)
                fireParticle.Stop();

            gameObject.SetActive(false);
        }
        else
        {
            transform.localScale = originalScale * currentHealth;
        }
    }
}