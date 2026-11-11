using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EchoStation.Player
{
    /// <summary>
    /// Holding Ali's breath (Space). Default: keep it pressed. Options can switch to "press once" (ToggleMode).
    /// No meter on screen: the picture darkens at the edges, sound gets muffled, the heartbeat climbs.
    /// Past the limit Ali gasps: a loud noise the killer hears. Under water the limit is shorter and
    /// letting go early means swallowing water (also a noise).
    /// The darkening uses a full-screen Image (a fade, not a HUD element).
    /// </summary>
    public class BreathHold : MonoBehaviour
    {
        public static bool ToggleMode;   // from the options menu
        public static bool Underwater;   // set by HidingSpot

        [SerializeField] AliController ali;
        [SerializeField] float maxHold = 18f;
        [SerializeField] float maxHoldUnderwater = 11f;
        [SerializeField] Image darken;                 // black, full screen, alpha driven here
        [SerializeField] AudioSource heartbeat;        // loop of heartbeat.ogg
        [SerializeField] AudioSource gaspSource;
        [SerializeField] AudioClip gasp, waterGasp;
        [SerializeField] AudioLowPassFilter listenerMuffle;   // on the camera (AudioListener object)

        public static event System.Action<Vector3> OnGasp;    // the killer can listen to this

        float held;
        bool toggled;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || ali == null) return;
            if (ToggleMode && kb.spaceKey.wasPressedThisFrame) toggled = !toggled;
            bool wants = ToggleMode ? toggled : kb.spaceKey.isPressed;
            bool holding = wants && (ali.IsHidden || Underwater);

            float limit = Underwater ? maxHoldUnderwater : maxHold;
            if (holding) held += Time.deltaTime; else held = Mathf.Max(0f, held - Time.deltaTime * 2f);
            if (holding && held >= limit) { Release(true); holding = false; }
            else if (!holding && ali.BreathHeld) Release(false);
            ali.BreathHeld = holding;

            float k = Mathf.Clamp01(held / limit);
            if (darken) { var c = darken.color; c.a = Mathf.SmoothStep(0f, 0.85f, k); darken.color = c; darken.enabled = c.a > 0.01f; }
            if (heartbeat)
            {
                if (k > 0.05f && !heartbeat.isPlaying) heartbeat.Play();
                heartbeat.volume = Mathf.Lerp(0f, 1f, k);
                heartbeat.pitch = Mathf.Lerp(0.9f, 1.6f, k);
                if (k <= 0.05f && heartbeat.isPlaying) heartbeat.Stop();
            }
            if (listenerMuffle) listenerMuffle.cutoffFrequency = Mathf.Lerp(22000f, Underwater ? 600f : 1800f, k);
        }

        void Release(bool forced)
        {
            ali.BreathHeld = false;
            toggled = false;
            // A forced release (or letting go under water) is loud.
            if (forced || Underwater)
            {
                if (gaspSource) gaspSource.PlayOneShot(Underwater ? waterGasp : gasp);
                OnGasp?.Invoke(ali.transform.position);
            }
            held = forced ? held * 0.5f : held;
        }
    }
}
