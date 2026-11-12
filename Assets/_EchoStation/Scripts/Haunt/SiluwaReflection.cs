using UnityEngine;

namespace EchoStation.Haunt
{
    /// <summary>
    /// The Si'luwa (Ali's guilt). A HALLUCINATION: she lives only in reflections, never moves in front of the eye,
    /// returns no echo (put her on the Hallucination layer), and makes no voice: only drips with no water.
    ///
    /// Cheap fake reflection (works without planar reflections):
    /// a "reflected" figure is placed BEHIND the glass at the mirror position of a spot behind Ali.
    /// While the player looks at the glass, she is there with her long fingers on Ali's shoulder.
    /// The moment the player turns around toward where she should be, she is gone (and stays gone).
    /// She stays until the player turns (team decision 5.2).
    /// </summary>
    public class SiluwaReflection : MonoBehaviour
    {
        [SerializeField] Transform glass;              // the door window; its forward points toward the corridor (toward Ali)
        [SerializeField] GameObject reflectedFigure;   // tall woman, wet hair covering the face, reed-root fingers
        [SerializeField] Transform cam;
        [SerializeField] float behindAli = 0.45f;      // where she "stands" behind Ali
        [SerializeField] float lookAtGlassDot = 0.8f;
        [SerializeField] float turnAroundDot = 0.2f;
        [SerializeField] AudioSource dripSource;       // dry drip, 2D, no reverb (hallucination = no echo)
        [SerializeField] AudioClip drip, shoulderPress;
        [SerializeField] float dripEvery = 2.2f;

        bool active, done, pressPlayed;
        float dripTimer;

        void OnEnable()
        {
            active = true; done = false; pressPlayed = false;
            if (reflectedFigure) reflectedFigure.SetActive(false);
            if (dripSource) { dripSource.spatialBlend = 0f; dripSource.bypassReverbZones = true; }
        }

        void Update()
        {
            if (!active || done || cam == null || glass == null) return;

            // Mirror of "a point just behind Ali" across the glass plane.
            Vector3 behind = cam.position - cam.forward * behindAli;
            behind.y = cam.position.y - 1.6f;
            Vector3 n = glass.forward;
            float dist = Vector3.Dot(behind - glass.position, n);
            Vector3 mirrored = behind - 2f * dist * n;

            Vector3 toGlass = (glass.position - cam.position).normalized;
            bool lookingAtGlass = Vector3.Dot(cam.forward, toGlass) > lookAtGlassDot;

            if (reflectedFigure)
            {
                // Only ever changes while not looked at directly (the rule: hallucinations never move in front of the eye).
                if (lookingAtGlass && !reflectedFigure.activeSelf)
                {
                    reflectedFigure.transform.position = mirrored;
                    reflectedFigure.transform.rotation = Quaternion.LookRotation(-n);
                    reflectedFigure.SetActive(true);
                    if (!pressPlayed && dripSource && shoulderPress) { dripSource.PlayOneShot(shoulderPress, 0.7f); pressPlayed = true; }
                }
            }

            // Turning around to face the real spot behind: she is gone for good.
            Vector3 toBehind = (behind + Vector3.up * 1.6f - cam.position).normalized;
            if (reflectedFigure && reflectedFigure.activeSelf && Vector3.Dot(cam.forward, toBehind) > turnAroundDot)
            {
                reflectedFigure.SetActive(false);
                done = true;
                enabled = false;
            }

            dripTimer += Time.deltaTime;
            if (dripTimer > dripEvery && dripSource && drip) { dripTimer = Random.Range(-0.6f, 0.4f); dripSource.PlayOneShot(drip, 0.5f); }
        }
    }
}
