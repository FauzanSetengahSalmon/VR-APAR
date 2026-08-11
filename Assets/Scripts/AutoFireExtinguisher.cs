using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class AutoFireExtinguisher : MonoBehaviour
{
    [Header("Referensi Spray & Audio")]
    public ParticleSystem sprayEffect;
    public AudioClip sprayAudioClip;
    [Range(0f, 1f)] public float sprayVolume = 0.9f;

    [Header("Logika Jarak Tembak")]
    [Tooltip("Jarak tembak asap APAR dalam meter")]
    public float extinguishRange = 4f;

    [Header("Mode Semprot Otomatis")]
    public bool autoSprayOnGrab = true;

    [Header("Offset Pegangan Tangan")]
    [Tooltip("Geser posisi APAR relatif terhadap tangan")]
    public Vector3 handOffsetPosition = Vector3.zero;
    [Tooltip("Putar rotasi APAR relatif terhadap tangan")]
    public Vector3 handOffsetRotation = Vector3.zero;

    [Header("Testing / Debug")]
    public bool debugForceSpray = false;

    private XRGrabInteractable grabInteractable;
    private AudioSource audioSource;
    private bool isTestingActive = false;

    // Status attachment ke tangan
    private bool isAttachedToHand = false;

    [HideInInspector] public bool isMainHandleHeld = false;
    [HideInInspector] public bool isHoseHeld = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Mengatur Rigidbody awal agar tidak jatuh di dinding saat game di-play
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = sprayAudioClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = sprayVolume;

        if (sprayEffect == null)
            sprayEffect = GetComponentInChildren<ParticleSystem>();

        if (sprayEffect != null)
        {
            var main = sprayEffect.main;
            main.playOnAwake = false;
            var em = sprayEffect.emission;
            em.enabled = true;

            if (sprayEffect.isPlaying)
                sprayEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnEnable()
    {
        if (grabInteractable == null) grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabEnter);
            grabInteractable.selectExited.AddListener(OnGrabExit);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabEnter);
            grabInteractable.selectExited.RemoveListener(OnGrabExit);
        }
        StopSpray();
    }

    private void OnGrabEnter(SelectEnterEventArgs args)
    {
        isMainHandleHeld = true;

        // Kunci APAR sebagai anak (Child) dari controller tangan
        if (!isAttachedToHand && args.interactorObject != null)
        {
            isAttachedToHand = true;

            // 1. Pindahkan parent ke tangan
            Transform handTransform = args.interactorObject.transform;
            transform.SetParent(handTransform);

            // 2. Reset posisi & rotasi lokal agar menempel di tangan
            transform.localPosition = handOffsetPosition;
            transform.localRotation = Quaternion.Euler(handOffsetRotation);

            // 3. Matikan fisika
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // 4. Matikan XRGrabInteractable agar sistem XR tidak lagi menarik/menggeser APAR
            if (grabInteractable != null)
            {
                grabInteractable.enabled = false;
            }
        }

        if (autoSprayOnGrab) StartSpray();
    }

    private void OnGrabExit(SelectExitEventArgs args)
    {
        // Jika sudah menempel di tangan, paksa status dipegang tetap true
        if (isAttachedToHand)
        {
            isMainHandleHeld = true;
            return; 
        }

        isMainHandleHeld = false;
        if (!isHoseHeld && !isTestingActive && !debugForceSpray)
        {
            StopSpray();
        }
    }

    private void Update()
    {
        if (grabInteractable != null && !isAttachedToHand)
        {
            isMainHandleHeld = grabInteractable.isSelected || grabInteractable.interactorsSelecting.Count > 0;
        }

        if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.gKey.wasPressedThisFrame))
        {
            ToggleTesting();
        }

        bool isAnyPartHeld = isMainHandleHeld || isHoseHeld || isAttachedToHand;
        bool shouldSpray = (isAnyPartHeld && autoSprayOnGrab) || isTestingActive || debugForceSpray;

        if (shouldSpray)
        {
            if (sprayEffect != null && !sprayEffect.isPlaying) StartSpray();

            ExtinguishFiresGradually();
        }
        else
        {
            if (sprayEffect != null && sprayEffect.isPlaying) StopSpray();
        }
    }

    private void ExtinguishFiresGradually()
    {
        Transform nozzle = sprayEffect != null ? sprayEffect.transform : transform;
        Debug.DrawRay(nozzle.position, nozzle.forward * extinguishRange, Color.red);

        RaycastHit[] hits = Physics.SphereCastAll(nozzle.position, 0.4f, nozzle.forward, extinguishRange);

        foreach (RaycastHit hit in hits)
        {
            FireExtinguisherTarget target = hit.collider.GetComponentInParent<FireExtinguisherTarget>();
            if (target != null)
            {
                target.ExtinguishGradually(Time.deltaTime);
            }
        }
    }

    private void ToggleTesting()
    {
        isTestingActive = !isTestingActive;
        Debug.Log("[AutoFireExtinguisher] Debug spray toggled: " + isTestingActive);
    }

    public void StartSpray()
    {
        if (sprayEffect != null)
        {
            if (!sprayEffect.gameObject.activeSelf) sprayEffect.gameObject.SetActive(true);
            if (!sprayEffect.isPlaying) sprayEffect.Play(true);
        }

        if (audioSource != null && sprayAudioClip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void StopSpray()
    {
        if (sprayEffect != null && sprayEffect.isPlaying)
        {
            sprayEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}