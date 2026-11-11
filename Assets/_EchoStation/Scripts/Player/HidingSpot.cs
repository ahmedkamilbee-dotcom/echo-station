using UnityEngine;

namespace EchoStation.Player
{
    /// <summary>
    /// A trigger volume where Ali can hide: under the control-room desk, under the pump-hall grate.
    /// Ali is hidden only while crouched inside. Hidden does not mean safe: the killer hears breathing
    /// at 2 m unless the player holds it (BreathHold).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HidingSpot : MonoBehaviour
    {
        [Tooltip("Under water (the grate): holding breath is mandatory, and the screen goes murky.")]
        public bool underwater;

        AliController inside;

        void Reset() { GetComponent<Collider>().isTrigger = true; }

        void OnTriggerStay(Collider other)
        {
            var ali = other.GetComponent<AliController>();
            if (ali == null) return;
            inside = ali;
            ali.IsHidden = ali.IsCrouching;
            BreathHold.Underwater = underwater && ali.IsCrouching;
        }

        void OnTriggerExit(Collider other)
        {
            var ali = other.GetComponent<AliController>();
            if (ali == null || ali != inside) return;
            ali.IsHidden = false;
            BreathHold.Underwater = false;
            inside = null;
        }
    }
}
