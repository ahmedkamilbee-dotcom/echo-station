using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using EchoStation.Echo;
using EchoStation.Player;

namespace EchoStation.World
{
    /// <summary>
    /// The first 60 seconds of the demo (docs/demo-design.md, scene 1 "the wake-up").
    /// Sound before picture. Ali lies on wet concrete, cheek to the floor. No text, no HUD.
    ///  0.0  black, a drop on the cheek (dry: no echo, the first hallucination)
    ///  1.5  bulb filament crackle
    ///  3.0  cassette hiss from the small recorder by his hand
    ///  5.0  Ali's breathing (nose); a line of yellowish neon under the door
    ///  7.0  the tape: "...confess... and you'll rest." (Ali's own voice, the only time early on)
    ///  9.0  click, the tape stops; generator hum (first safety sound)
    /// 10.0  eyelids open (a fade, not a cut): hand, cracked watch at 4:52, warm tea glass
    /// 25.0  any movement key: Ali gets up (input enabled)
    /// 45.0  three quiet knocks on the pipes
    /// 50.0  bulb and flashlight die, 3 s of darkness (hum + breath only)
    /// 55.0  Ali's finger taps the flashlight once by itself: the first echo pulse.
    ///       In the door's window, a head tilted to the right returns a dull "thud" (real).
    /// The eyelid overlay is a full-screen black Image (a fade, not a HUD element).
    /// </summary>
    public class WakeSequence : MonoBehaviour
    {
        [Header("Ali")]
        [SerializeField] AliController ali;
        [SerializeField] Transform cameraPivot;
        [SerializeField] Flashlight flashlight;
        [SerializeField] EchoPulse pulse;
        [SerializeField] Vector3 lyingCameraLocalPos = new Vector3(0f, 0.18f, 0f);
        [SerializeField] float lyingRoll = 78f;

        [Header("World")]
        [SerializeField] Light bulb;
        [SerializeField] GeneratorPower generator;
        [SerializeField] GameObject tiltedHeadInDoorWindow;   // real body: EchoSurface Body; enabled only at 55 s

        [Header("Sound")]
        [SerializeField] AudioSource oneShots;                 // 2D, near the head
        [SerializeField] AudioSource recorder;                 // 3D, by the hand
        [SerializeField] AudioClip drip, bulbCrackle, tapeHiss, breathing, tapeLine, tapeStopClick, knocks;

        [Header("Eyelids")]
        [SerializeField] Image eyelids;                        // full-screen black on a Screen Space Overlay canvas

        [SerializeField] bool skipForTesting;

        bool started;

        IEnumerator Start()
        {
            if (skipForTesting) { Finish(); yield break; }
            ali.InputEnabled = false;
            var standPos = cameraPivot.localPosition;
            cameraPivot.localPosition = lyingCameraLocalPos;
            cameraPivot.localRotation = Quaternion.Euler(0f, 0f, lyingRoll);
            SetEyelids(1f);
            if (bulb) bulb.enabled = false;
            if (flashlight) flashlight.enabled = false;

            float t0 = Time.time;
            yield return At(t0, 0f);   Play(drip, 0.8f);
            yield return At(t0, 1.5f); if (bulb) bulb.enabled = true; Play(bulbCrackle, 0.6f);
            yield return At(t0, 3f);   if (recorder && tapeHiss) { recorder.clip = tapeHiss; recorder.loop = true; recorder.Play(); }
            yield return At(t0, 5f);   Play(breathing, 0.7f);
            yield return At(t0, 7f);   if (recorder && tapeLine) recorder.PlayOneShot(tapeLine);
            yield return At(t0, 9f);   if (recorder) { recorder.Stop(); recorder.PlayOneShot(tapeStopClick); }
            if (generator) generator.Restore();
            yield return At(t0, 10f);  yield return FadeEyelids(1f, 0.35f, 1.2f); yield return FadeEyelids(0.35f, 0.6f, 0.4f); yield return FadeEyelids(0.6f, 0f, 1.5f);

            // Wait for the player to try moving (but not before 25 s).
            yield return At(t0, 25f);
            while (!MoveKeyPressed()) yield return null;
            yield return GetUp(standPos);
            ali.InputEnabled = true;
            if (flashlight) flashlight.enabled = true;

            yield return At(t0, 45f); if (knocks) AudioSource.PlayClipAtPoint(knocks, cameraPivot.position + cameraPivot.right * 3f, 0.9f);
            yield return At(t0, 50f);
            if (bulb) bulb.enabled = false;
            if (flashlight) flashlight.Flicker(3f, false);
            yield return At(t0, 55f);
            if (tiltedHeadInDoorWindow) tiltedHeadInDoorWindow.SetActive(true);
            if (pulse) pulse.TryPulse();
            yield return new WaitForSeconds(2.2f);
            if (tiltedHeadInDoorWindow) tiltedHeadInDoorWindow.SetActive(false);
            if (bulb) bulb.enabled = true;
            Finish();
        }

        void Finish()
        {
            if (ali) ali.InputEnabled = true;
            if (flashlight) flashlight.enabled = true;
            if (eyelids) eyelids.enabled = false;
            started = true;
        }

        IEnumerator GetUp(Vector3 standPos)
        {
            Vector3 from = cameraPivot.localPosition; Quaternion fromRot = cameraPivot.localRotation;
            for (float t = 0; t < 2.2f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 2.2f);
                cameraPivot.localPosition = Vector3.Lerp(from, standPos, k);
                cameraPivot.localRotation = Quaternion.Slerp(fromRot, Quaternion.identity, k);
                yield return null;
            }
        }

        static bool MoveKeyPressed()
        {
            var kb = Keyboard.current;
            return kb != null && (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed);
        }

        static IEnumerator At(float t0, float second) { while (Time.time - t0 < second) yield return null; }

        void Play(AudioClip c, float v) { if (oneShots && c) oneShots.PlayOneShot(c, v); }

        void SetEyelids(float a) { if (eyelids) { eyelids.enabled = true; var c = eyelids.color; c.a = a; eyelids.color = c; } }

        IEnumerator FadeEyelids(float from, float to, float dur)
        {
            for (float t = 0; t < dur; t += Time.deltaTime) { SetEyelids(Mathf.Lerp(from, to, t / dur)); yield return null; }
            SetEyelids(to);
        }
    }
}
