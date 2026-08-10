using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class AutoFireExtinguisher : MonoBehaviour
{
    public ParticleSystem sprayEffect;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnDrop);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnDrop);
        }
    }

    private void OnGrab(BaseInteractionEventArgs args)
    {
        if (rb != null) rb.isKinematic = false;
        if (sprayEffect != null) sprayEffect.Play();
    }

    private void OnDrop(BaseInteractionEventArgs args)
    {
        if (sprayEffect != null) sprayEffect.Stop();
    }
}