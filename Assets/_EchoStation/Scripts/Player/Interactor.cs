using UnityEngine;
using UnityEngine.InputSystem;
using EchoStation.Audio;

namespace EchoStation.Player
{
    /// <summary>
    /// One place that handles "E": a short raycast from the camera. No HUD and no prompt on screen:
    /// the thing you can use simply catches a slightly warmer light (handled by its own material).
    /// Put this on the camera.
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        [SerializeField] float reach = 2.2f;
        [SerializeField] LayerMask mask = ~0;

        LivingTape focusedTape;

        void Update()
        {
            LivingTape tape = null;
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, reach, mask, QueryTriggerInteraction.Ignore))
                tape = hit.collider.GetComponentInParent<LivingTape>();

            if (focusedTape != tape)
            {
                if (focusedTape) focusedTape.Focused = false;
                focusedTape = tape;
                if (focusedTape) focusedTape.Focused = true;
            }

            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame && focusedTape) focusedTape.Interact();
        }
    }
}
