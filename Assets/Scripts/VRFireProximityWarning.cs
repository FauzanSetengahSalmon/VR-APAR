using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR;

/// <summary>
/// Mendeteksi jarak pemain terhadap kobaran api aktif dan menampilkan UI peringatan animasi
/// ketika pemain berada terlalu dekat (< 1.8 meter).
/// </summary>
public class VRFireProximityWarning : MonoBehaviour
{
    public static VRFireProximityWarning Instance { get; private set; }

    [Header("Pengaturan Jarak Bahaya (Meter)")]
    [Tooltip("Batas jarak memicu peringatan (meter).")]
    public float safeDistanceThreshold = 1.85f;

    [Tooltip("Kecepatan transisi fade in/out UI peringatan.")]
    public float fadeSpeed = 6.0f;

    [Header("Haptic Feedback")]
    [Tooltip("Aktifkan getaran haptic controller saat terlalu dekat dengan api.")]
    public bool enableHaptics = true;
    public float hapticPulseInterval = 0.7f;

    // ── Internal Cache ──
    private Transform _cameraTransform;
    private List<FireExtinguisherTarget> _fireTargets = new List<FireExtinguisherTarget>();

    private Canvas _warningCanvas;
    private CanvasGroup _canvasGroup;
    private RectTransform _panelRect;
    private Image _panelBgImage;
    private Image _borderGlowImage;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _subText;
    private TextMeshProUGUI _distanceText;

    private bool _isWarningActive = false;
    private float _currentClosestDist = 999f;
    private float _lastHapticTime = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        BuildWarningUI();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (FindFirstObjectByType<VRFireProximityWarning>() == null)
        {
            GameObject go = new GameObject("VR_Fire_Proximity_Warning_Manager");
            go.AddComponent<VRFireProximityWarning>();
        }
    }

    private void Start()
    {
        if (Camera.main != null)
            _cameraTransform = Camera.main.transform;

        RefreshFireList();
    }

    public void RefreshFireList()
    {
        _fireTargets.Clear();
        _fireTargets.AddRange(FindObjectsByType<FireExtinguisherTarget>(FindObjectsSortMode.None));
    }

    private void Update()
    {
        // Peringatan HANYA aktif saat misi pemadaman resmi berjalan (ActiveMission)
        if (VRSimulationUIManager.Instance != null && VRSimulationUIManager.Instance.currentPhase != VRSimulationUIManager.UIPhase.ActiveMission)
        {
            _isWarningActive = false;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
            }
            return;
        }

        if (_cameraTransform == null)
        {
            if (Camera.main != null) _cameraTransform = Camera.main.transform;
            else return;
        }

        // Cek jarak horizontal terdekat ke titik api aktif (pada bidang lantai XZ)
        _currentClosestDist = 999f;
        bool anyFireTooClose = false;

        Vector3 camPosFlat = new Vector3(_cameraTransform.position.x, 0f, _cameraTransform.position.z);

        foreach (var fire in _fireTargets)
        {
            if (fire == null || !fire.gameObject.activeInHierarchy || fire.IsExtinguished)
                continue;

            Vector3 firePosFlat = new Vector3(fire.transform.position.x, 0f, fire.transform.position.z);
            float dist = Vector3.Distance(camPosFlat, firePosFlat);
            if (dist < _currentClosestDist)
            {
                _currentClosestDist = dist;
            }

            // Peringatan bahaya HANYA berbunyi jika jarak horizontal lebih dekat dari 1.35 meter (< 1.35 m)
            // Jarak 1.5 - 2.0 meter adalah Jarak Ideal Aman pemadaman APAR.
            if (dist < 1.35f)
            {
                anyFireTooClose = true;
            }
        }

        _isWarningActive = anyFireTooClose;

        // Animate UI Visibility
        float targetAlpha = _isWarningActive ? 1f : 0f;
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }

        if (_canvasGroup != null && _canvasGroup.alpha > 0.01f)
        {
            UpdateUIPositionAndAnimation();
        }

        // Haptic Feedback
        if (_isWarningActive && enableHaptics && Time.time - _lastHapticTime > hapticPulseInterval)
        {
            _lastHapticTime = Time.time;
            TriggerHapticPulse(0.35f, 0.15f);
        }
    }

    private void UpdateUIPositionAndAnimation()
    {
        if (_cameraTransform == null || _warningCanvas == null) return;

        // Posisikan canvas di depan kamera mata sedikit ke bawah pandangan
        Vector3 forward = _cameraTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = _cameraTransform.forward;
        forward.Normalize();

        Vector3 targetPos = _cameraTransform.position + forward * 0.95f + Vector3.up * -0.15f;
        _warningCanvas.transform.position = targetPos;
        _warningCanvas.transform.rotation = Quaternion.LookRotation(forward);

        // Animasi pulsing glow merah
        float pulse = (Mathf.Sin(Time.time * 5.0f) + 1f) * 0.5f; // 0..1
        if (_borderGlowImage != null)
        {
            _borderGlowImage.color = Color.Lerp(new Color(1f, 0.15f, 0.15f, 0.4f), new Color(1f, 0.2f, 0.2f, 0.95f), pulse);
        }

        bool isEnglish = VRLanguageManager.IsEnglish;

        if (_titleText != null)
        {
            _titleText.text = isEnglish ? "[!] DANGER: TOO CLOSE TO FIRE!" : "[!] BAHAYA: TERLALU DEKAT DENGAN API!";
        }

        if (_subText != null)
        {
            _subText.text = isEnglish 
                ? "Step back and maintain a safe distance of 1.5 – 2.0 meters from fire" 
                : "Mundurlah dan jaga jarak aman 1.5 – 2.0 meter dari titik api";
        }

        if (_distanceText != null)
        {
            _distanceText.text = isEnglish
                ? $"Your Distance: <color=#FF3B30><b>{_currentClosestDist:F1} m</b></color>  |  Safe Extinguishing Distance: <b>1.5 - 2.0 m</b>"
                : $"Jarak Anda: <color=#FF3B30><b>{_currentClosestDist:F1} m</b></color>  |  Jarak Aman Pemadaman: <b>1.5 - 2.0 m</b>";
        }
    }

    private void TriggerHapticPulse(float amplitude, float duration)
    {
        var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.isValid) leftHand.SendHapticImpulse(0, amplitude, duration);

        var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightHand.isValid) rightHand.SendHapticImpulse(0, amplitude, duration);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BUILD WARNING UI PROGRAMMATICALLY
    // ═══════════════════════════════════════════════════════════════════════

    private void BuildWarningUI()
    {
        GameObject canvasGO = new GameObject("VR_Fire_Proximity_Warning_Canvas");
        _warningCanvas = canvasGO.AddComponent<Canvas>();
        _warningCanvas.renderMode = RenderMode.WorldSpace;
        _warningCanvas.sortingOrder = 99;

        _canvasGroup = canvasGO.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(700f, 220f);
        canvasRT.localScale = Vector3.one * 0.0012f;

        // Outer Glow Border
        GameObject borderGO = new GameObject("BorderGlow");
        borderGO.transform.SetParent(canvasGO.transform, false);
        _borderGlowImage = borderGO.AddComponent<Image>();
        _borderGlowImage.color = new Color(1f, 0.15f, 0.15f, 0.8f);
        RectTransform borderRT = borderGO.GetComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.sizeDelta = new Vector2(10f, 10f);

        // Panel Background
        GameObject panelGO = new GameObject("WarningPanel");
        panelGO.transform.SetParent(borderGO.transform, false);
        _panelBgImage = panelGO.AddComponent<Image>();
        _panelBgImage.color = new Color(0.08f, 0.02f, 0.02f, 0.92f);
        _panelRect = panelGO.GetComponent<RectTransform>();
        _panelRect.anchorMin = Vector2.zero;
        _panelRect.anchorMax = Vector2.one;
        _panelRect.sizeDelta = new Vector2(-8f, -8f);

        // Vertical Layout Container
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(panelGO.transform, false);
        RectTransform contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 16, 16);
        vlg.spacing = 8f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;

        // Title
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(contentGO.transform, false);
        _titleText = titleGO.AddComponent<TextMeshProUGUI>();
        _titleText.text = "[!] BAHAYA: TERLALU DEKAT DENGAN API!";
        _titleText.fontSize = 32f;
        _titleText.fontStyle = FontStyles.Bold;
        _titleText.alignment = TextAlignmentOptions.Center;
        _titleText.color = new Color(1f, 0.25f, 0.25f, 1f);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.sizeDelta = new Vector2(650f, 42f);

        // Subtitle
        GameObject subGO = new GameObject("SubtitleText");
        subGO.transform.SetParent(contentGO.transform, false);
        _subText = subGO.AddComponent<TextMeshProUGUI>();
        _subText.text = "Mundurlah dan jaga jarak aman 1.5 – 2.0 meter dari titik api";
        _subText.fontSize = 22f;
        _subText.alignment = TextAlignmentOptions.Center;
        _subText.color = new Color(0.92f, 0.92f, 0.92f, 0.95f);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.sizeDelta = new Vector2(650f, 32f);

        // Distance Indicator
        GameObject distGO = new GameObject("DistanceText");
        distGO.transform.SetParent(contentGO.transform, false);
        _distanceText = distGO.AddComponent<TextMeshProUGUI>();
        _distanceText.text = "Jarak Anda: 1.2 m  |  Batas Aman: 1.5 - 2.0 m";
        _distanceText.fontSize = 20f;
        _distanceText.fontStyle = FontStyles.Bold;
        _distanceText.alignment = TextAlignmentOptions.Center;
        _distanceText.color = new Color(1f, 0.85f, 0.2f, 1f);
        RectTransform distRT = distGO.GetComponent<RectTransform>();
        distRT.sizeDelta = new Vector2(650f, 28f);
    }
}
