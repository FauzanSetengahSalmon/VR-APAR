using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Tombol Close (X) untuk Poster K3.
/// Menggunakan posisi lokal (child) agar ukurannya konsisten walau scale poster di-scale down.
/// </summary>
public class SafetyPosterCloseButton : MonoBehaviour
{
    [Header("Posisi Lokal Tombol (Relatif Terhadap Poster)")]
    [Tooltip("Posisi offset lokal dari center poster. Nilai X/Y 2 sampai 4 biasanya pas untuk sprite ini.")]
    public Vector3 localOffset = new Vector3(3.8f, 3.8f, -0.1f);

    [Header("Ukuran Tombol")]
    [Tooltip("Ukuran teks X dalam poin TMP (karena terpengaruh scale parent)")]
    public float fontSize = 12f;

    [Tooltip("Radius hitbox sphere untuk raycast/klik")]
    public float hitRadius = 0.3f;

    [Header("Warna Visual")]
    public Color xColor      = new Color(1f, 0.25f, 0.25f, 1f);   // Merah cerah
    public Color xHoverColor = new Color(1f, 0.6f,  0.6f,  1f);   // Merah terang saat hover

    // ── Internal ─────────────────────────────────────────────────────────────
    private GameObject  buttonContainer;
    private TextMeshPro xText;
    private bool        isHovered;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildButton();
    }

    private void OnEnable()
    {
        if (buttonContainer != null) buttonContainer.SetActive(true);
    }

    private void OnDisable()
    {
        if (buttonContainer != null) buttonContainer.SetActive(false);
    }

    private void OnDestroy()
    {
        if (buttonContainer != null) Destroy(buttonContainer);
    }

    private void Update()
    {
        if (buttonContainer == null) return;

        // ── Mouse / Simulator Raycast (Support New Input System) ──────────────
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector2 mousePos = GetMousePosition();
        Ray ray = cam.ScreenPointToRay(mousePos);
        bool hitThisFrame = false;

        if (Physics.Raycast(ray, out RaycastHit hit, 30f))
        {
            if (hit.collider != null && hit.collider.transform.IsChildOf(transform))
            {
                hitThisFrame = true;

                if (IsLeftMousePressedThisFrame())
                {
                    ClosePoster();
                }
            }
        }

        // Hover feedback
        if (hitThisFrame != isHovered)
        {
            isHovered = hitThisFrame;
            if (xText != null)
                xText.color = isHovered ? xHoverColor : xColor;
        }
    }

    // =========================================================================
    //  BUILD (Membuat Child GameObject)
    // =========================================================================
    private void BuildButton()
    {
        // 1. Buat Container Child
        buttonContainer = new GameObject("CloseButton_Child");
        buttonContainer.transform.SetParent(transform, false);
        buttonContainer.transform.localPosition = localOffset;

        // 2. Buat Teks "X" (TextMeshPro 3D)
        GameObject textGO = new GameObject("X_Text");
        textGO.transform.SetParent(buttonContainer.transform, false);

        xText                  = textGO.AddComponent<TextMeshPro>();
        xText.text             = "X";
        xText.color            = xColor;
        xText.fontSize         = fontSize;
        xText.fontStyle        = FontStyles.Bold;
        xText.alignment        = TextAlignmentOptions.Center;
        xText.enableWordWrapping = false;

        // 3. Collider Hitbox (SphereCollider)
        SphereCollider sc = buttonContainer.AddComponent<SphereCollider>();
        sc.radius = hitRadius;

        // 4. XRSimpleInteractable untuk VR Controller
        XRSimpleInteractable xri = buttonContainer.AddComponent<XRSimpleInteractable>();
        xri.selectEntered.AddListener((_) => ClosePoster());
        xri.firstHoverEntered.AddListener((_) =>
        {
            if (xText != null) xText.color = xHoverColor;
        });
        xri.lastHoverExited.AddListener((_) =>
        {
            if (xText != null) xText.color = xColor;
        });
    }

    // ── Input System Wrappers ────────────────────────────────────────────────
    private Vector2 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
        return Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    private bool IsLeftMousePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null) return Mouse.current.leftButton.wasPressedThisFrame;
        return false;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    // ── Close ────────────────────────────────────────────────────────────────
    public void ClosePoster()
    {
        Debug.Log("[SafetyPoster] ❌ Poster ditutup.");
        gameObject.SetActive(false);
    }
}