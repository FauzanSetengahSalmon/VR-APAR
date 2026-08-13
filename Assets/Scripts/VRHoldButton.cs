using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

// Kita extend dari XRSimpleInteractable bawaan XR Toolkit
public class VRHoldButton : UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable
{
    [Header("Hold Settings")]
    public float holdDuration = 3.0f; // Tahan 3 detik

    [Header("Events")]
    public UnityEvent OnHoldComplete;

    private float currentHoldTime = 0f;
    private bool isActivated = false;

    // IsActivated dipanggil otomatis oleh XR Toolkit saat tombol Trigger ditekan
    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);
        isActivated = true;
    }

    // Dipanggil otomatis saat tombol Trigger dilepas
    protected override void OnDeactivated(DeactivateEventArgs args)
    {
        base.OnDeactivated(args);
        isActivated = false;
        currentHoldTime = 0f; // Reset kalau dilepas sebelum 3 detik
    }

void Update()
{
    // Cek apakah tombol Trigger VR ditekan ATAU Klik Kiri Mouse (L Mouse di Simulator)
    bool isTriggerPressed = false;

    // Pengecekan dari XR Interactor
    if (isSelected || isActivated)
    {
        isTriggerPressed = true;
    }

    // Pengecekan dari L Mouse untuk XR Device Simulator
    if (UnityEngine.InputSystem.Mouse.current != null && 
        UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
    {
        isTriggerPressed = true;
    }

    // Logic Tahan 3 Detik
    if (isHovered && isTriggerPressed)
    {
        currentHoldTime += Time.deltaTime;
        Debug.Log("Holding... " + Mathf.Clamp(currentHoldTime, 0, holdDuration).ToString("F1") + "s");

        if (currentHoldTime >= holdDuration)
        {
            Debug.Log("Misi Dimulai!");
            OnHoldComplete?.Invoke();
            currentHoldTime = 0f;
        }
    }
    else
    {
        currentHoldTime = 0f;
    }
}
}