using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class APARHoseGrabber : MonoBehaviour
{
    [Header("Referensi")]
    public AutoFireExtinguisher mainExtinguisher;

    private XRGrabInteractable hoseGrabInteractable;

    private void Awake()
    {
        hoseGrabInteractable = GetComponent<XRGrabInteractable>();
        if (hoseGrabInteractable == null)
            hoseGrabInteractable = gameObject.AddComponent<XRGrabInteractable>();

        hoseGrabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
        hoseGrabInteractable.trackPosition = true;
        hoseGrabInteractable.trackRotation = true;
    }

    private void OnEnable()
    {
        if (hoseGrabInteractable != null)
        {
            hoseGrabInteractable.selectEntered.AddListener(OnHoseGrabbed);
            hoseGrabInteractable.selectExited.AddListener(OnHoseReleased);
        }
    }

    private void OnDisable()
    {
        if (hoseGrabInteractable != null)
        {
            hoseGrabInteractable.selectEntered.RemoveListener(OnHoseGrabbed);
            hoseGrabInteractable.selectExited.RemoveListener(OnHoseReleased);
        }
    }

    private void OnHoseGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log("[APARHoseGrabber] Hose Grabbed!");
        if (mainExtinguisher != null)
        {
            mainExtinguisher.isHoseHeld = true;
        }
    }

    private void OnHoseReleased(SelectExitEventArgs args)
    {
        Debug.Log("[APARHoseGrabber] Hose Released!");
        if (mainExtinguisher != null)
        {
            mainExtinguisher.isHoseHeld = false;
        }
    }
    
    private void Update()
    {
        // Fallback safety check if events fail
        if (hoseGrabInteractable != null && mainExtinguisher != null)
        {
            mainExtinguisher.isHoseHeld = hoseGrabInteractable.isSelected || hoseGrabInteractable.interactorsSelecting.Count > 0;
        }
    }
}
