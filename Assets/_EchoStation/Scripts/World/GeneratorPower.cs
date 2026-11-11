using System.Collections;
using UnityEngine;

namespace EchoStation.World
{
    /// <summary>
    /// The diesel generator: its hum is the ONLY safety sound in the demo. The neon lights breathe with it.
    /// When it dies (anomaly 12, scripted moments), lights go out and there is total silence for a moment:
    /// the player learns that losing the hum means the killer is near.
    /// Others listen through OnPowerChanged (the killer, the ambience director).
    /// </summary>
    public class GeneratorPower : MonoBehaviour
    {
        [SerializeField] AudioSource hum;                 // generator_hum_loop
        [SerializeField] AudioClip dieClip;               // generator_die
        [SerializeField] Light[] neonLights;
        [SerializeField] float flickerDepth = 0.12f;      // neon wobble with the engine
        [SerializeField] AudioSource[] silencedDuringBlackout;   // ambience beds to mute (not the tape)

        public static event System.Action<bool> OnPowerChanged;
        public static bool PowerOn { get; private set; } = true;

        float[] baseIntensity;
        float[] baseVolumes;
        bool busy;

        void Awake()
        {
            baseIntensity = new float[neonLights.Length];
            for (int i = 0; i < neonLights.Length; i++) baseIntensity[i] = neonLights[i] ? neonLights[i].intensity : 0f;
            baseVolumes = new float[silencedDuringBlackout.Length];
            for (int i = 0; i < silencedDuringBlackout.Length; i++) baseVolumes[i] = silencedDuringBlackout[i] ? silencedDuringBlackout[i].volume : 0f;
            PowerOn = true;
        }

        void Update()
        {
            if (!PowerOn) return;
            // Neon follows the engine's uneven rhythm (slow, never a strobe).
            float wobble = 1f - flickerDepth * (0.5f + 0.5f * Mathf.PerlinNoise(Time.time * 1.7f, 0.3f));
            for (int i = 0; i < neonLights.Length; i++) if (neonLights[i]) neonLights[i].intensity = baseIntensity[i] * wobble;
        }

        /// <summary>Generator dies, stays off for 'darkSeconds' (first 3 s are total silence), then coughs back to life.</summary>
        public void Blackout(float darkSeconds = 8f, bool restart = true)
        {
            if (!busy) StartCoroutine(BlackoutRoutine(darkSeconds, restart));
        }

        IEnumerator BlackoutRoutine(float darkSeconds, bool restart)
        {
            busy = true;
            if (hum && dieClip) hum.PlayOneShot(dieClip);
            // Lights sag over ~1.5 s then cut.
            for (float t = 0; t < 1.5f; t += Time.deltaTime)
            {
                float k = 1f - t / 1.5f;
                for (int i = 0; i < neonLights.Length; i++) if (neonLights[i]) neonLights[i].intensity = baseIntensity[i] * k;
                if (hum) hum.pitch = Mathf.Lerp(0.6f, 1f, k);
                yield return null;
            }
            SetLights(false);
            if (hum) { hum.Stop(); hum.pitch = 1f; }
            PowerOn = false;
            OnPowerChanged?.Invoke(false);

            // Total silence: the loudest moment in the demo is the quietest.
            for (int i = 0; i < silencedDuringBlackout.Length; i++) if (silencedDuringBlackout[i]) silencedDuringBlackout[i].volume = 0f;
            yield return new WaitForSeconds(3f);
            for (int i = 0; i < silencedDuringBlackout.Length; i++) if (silencedDuringBlackout[i]) silencedDuringBlackout[i].volume = baseVolumes[i];

            if (!restart) { busy = false; yield break; }
            yield return new WaitForSeconds(Mathf.Max(0f, darkSeconds - 3f));
            Restore();
            busy = false;
        }

        public void Restore()
        {
            SetLights(true);
            for (int i = 0; i < neonLights.Length; i++) if (neonLights[i]) neonLights[i].intensity = baseIntensity[i];
            if (hum) hum.Play();
            PowerOn = true;
            OnPowerChanged?.Invoke(true);
        }

        void SetLights(bool on) { foreach (var l in neonLights) if (l) l.enabled = on; }
    }
}
