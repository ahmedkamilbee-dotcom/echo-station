using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoStation.Player
{
    /// <summary>
    /// Ali's flashlight. No battery bar on screen (no HUD): the beam gets warmer and weaker as it drains.
    /// Tapping the flashlight body (key: F) is also what fires the echo pulse (see EchoPulse).
    /// "Reduce flashing" in the options limits flicker speed for photosensitive players.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class Flashlight : MonoBehaviour
    {
        [SerializeField] float maxIntensity = 800f;   // HDRP physical units (lumens)
        [SerializeField] Color freshColor = new Color(1f, 0.97f, 0.88f);
        [SerializeField] Color drainedColor = new Color(1f, 0.72f, 0.42f);
        [SerializeField, Range(0f, 1f)] float battery = 1f;
        [SerializeField] float drainPerMinute = 0.025f;
        [SerializeField] AudioSource clickSource;
        [SerializeField] AudioClip clickOn, clickOff;

        public static bool ReduceFlashing;   // set from the options menu / first warning screen

        Light beam;
        bool isOn = true;
        bool flickering;

        public bool IsOn => isOn && !flickering;

        void Awake() { beam = GetComponent<Light>(); }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.qKey.wasPressedThisFrame) Toggle();   // Q: flashlight on/off (E is "interact")

            if (isOn) battery = Mathf.Max(0.15f, battery - drainPerMinute / 60f * Time.deltaTime);
            if (!flickering)
            {
                beam.enabled = isOn;
                beam.intensity = maxIntensity * Mathf.Lerp(0.35f, 1f, battery);
                beam.color = Color.Lerp(drainedColor, freshColor, battery);
            }
        }

        public void Toggle()
        {
            if (flickering) return;
            isOn = !isOn;
            if (clickSource != null) clickSource.PlayOneShot(isOn ? clickOn : clickOff, 0.6f);
        }

        /// <summary>Scripted flicker/die (e.g. the wake-up room at 0:50). Never more than ~3 flashes per second.</summary>
        public void Flicker(float duration, bool endOff = false)
        {
            if (!flickering) StartCoroutine(FlickerRoutine(duration, endOff));
        }

        IEnumerator FlickerRoutine(float duration, bool endOff)
        {
            flickering = true;
            float end = Time.time + duration;
            while (Time.time < end)
            {
                if (ReduceFlashing)
                {
                    // Slow dim instead of flashing.
                    beam.intensity = Mathf.Lerp(beam.intensity, 0.2f, 2f * Time.deltaTime);
                    yield return null;
                }
                else
                {
                    beam.enabled = !beam.enabled;
                    yield return new WaitForSeconds(Random.Range(0.18f, 0.5f)); // irregular, max ~3/s, never "three-three"
                }
            }
            if (ReduceFlashing && !endOff)
            {
                float target = maxIntensity * Mathf.Lerp(0.35f, 1f, battery);
                for (float t = 0; t < 0.3f; t += Time.deltaTime) { beam.intensity = Mathf.Lerp(0.2f, target, t / 0.3f); yield return null; }
            }
            beam.enabled = !endOff;
            isOn = !endOff;
            flickering = false;
        }
    }
}
