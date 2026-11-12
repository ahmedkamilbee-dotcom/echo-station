using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoStation.Player
{
    /// <summary>Anything Ali can use with E (looked at, within reach).</summary>
    public interface IInteractable
    {
        void Interact();
        bool Focused { get; set; }
    }

    /// <summary>
    /// One place that handles "E": a short raycast from the camera. No HUD and no prompt on screen:
    /// the thing you can use simply catches a slightly warmer light (handled by its own material).
    /// Put this on the camera.
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        [SerializeField] float reach = 2.2f;
        [SerializeField] LayerMask mask = ~0;

        IInteractable focused;

        void Update()
        {
            IInteractable target = null;
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, reach, mask, QueryTriggerInteraction.Ignore))
                target = hit.collider.GetComponentInParent<IInteractable>();

            if (!ReferenceEquals(focused, target))
            {
                if (focused as Object) focused.Focused = false;
                focused = target;
                if (focused as Object) focused.Focused = true;
            }

            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame && focused as Object) focused.Interact();
        }
    }
}
