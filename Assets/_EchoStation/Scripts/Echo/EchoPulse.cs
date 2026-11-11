using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoStation.Echo
{
    /// <summary>
    /// The echo pulse: Ali taps his flashlight (key F) and the world answers.
    /// Rays go out in a sphere; every hit becomes a pale-green point that fades, and plays a
    /// "click" delayed by distance (speed of sound, exaggerated). The material of the hit picks the sound:
    /// reed = hollow tick, metal = ring, body = dull thud.
    ///
    /// THE GOLDEN RULE: "The eye lies, the echo doesn't."
    /// Objects with an EchoSurface marked as Hallucination return NOTHING: no point, no sound.
    ///
    /// Every pulse is also a noise: it notifies listeners (the killer) through OnPulse.
    /// Grey-box first: this needs no custom shader, only a ParticleSystem created at runtime.
    /// </summary>
    public class EchoPulse : MonoBehaviour
    {
        [Header("Pulse")]
        [SerializeField] int rayCount = 900;
        [SerializeField] float maxDistance = 22f;
        [SerializeField] float cooldown = 0.35f;   // >= 0.34s: never more than 3 pulses per second (puzzle 2 needs fast taps)
        [SerializeField] LayerMask echoLayers = ~0;
        [Tooltip("Real speed of sound is 343 m/s; we slow it so the player can hear distance.")]
        [SerializeField] float audibleSoundSpeed = 343f / 3f;

        [Header("Look")]
        [SerializeField] Color echoColor = new Color(0.62f, 0.93f, 0.76f, 1f);   // pale green: the colour of sound
        [SerializeField] float pointSize = 0.045f;
        [SerializeField] float pointLifetime = 2.0f;

        [Header("Sound")]
        [SerializeField] AudioSource tapSource;
        [SerializeField] AudioClip tapClip;
        [SerializeField] int maxClickVoices = 12;

        public static event System.Action<Vector3, float> OnPulse;   // position, loudness radius

        [Tooltip("Make in the Editor: HDRP/Unlit, Surface Transparent, Blend Additive (enable Transparent Depth Prepass off).")]
        [SerializeField] Material pointMaterial;

        ParticleSystem points;
        readonly Dictionary<Collider, EchoSurface> surfaceCache = new Dictionary<Collider, EchoSurface>();

        EchoSurface GetSurface(Collider c)
        {
            if (!surfaceCache.TryGetValue(c, out var s)) { s = c.GetComponentInParent<EchoSurface>(); surfaceCache[c] = s; }
            return s;
        }

        void OnDestroy() { if (points) Destroy(points.gameObject); }
        float lastPulse = -99f;
        readonly List<AudioSource> clickPool = new List<AudioSource>();
        int clickIndex;

        void Awake()
        {
            BuildParticleSystem();
            for (int i = 0; i < maxClickVoices; i++)
            {
                var go = new GameObject("EchoClick_" + i);
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.maxDistance = maxDistance * 1.5f;
                clickPool.Add(src);
            }
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.fKey.wasPressedThisFrame) TryPulse();
        }

        public bool TryPulse()
        {
            if (Time.time - lastPulse < cooldown) return false;
            lastPulse = Time.time;
            if (tapSource != null && tapClip != null) tapSource.PlayOneShot(tapClip);
            OnPulse?.Invoke(transform.position, maxDistance);
            StartCoroutine(PulseRoutine());
            return true;
        }

        IEnumerator PulseRoutine()
        {
            Vector3 origin = transform.position;
            var hits = new List<(Vector3 point, float dist, EchoSurface surface)>(rayCount);

            // Fibonacci sphere: evenly spread rays.
            float golden = Mathf.PI * (3f - Mathf.Sqrt(5f));
            for (int i = 0; i < rayCount; i++)
            {
                float y = 1f - (i / (float)(rayCount - 1)) * 2f;
                float r = Mathf.Sqrt(1f - y * y);
                float theta = golden * i;
                Vector3 dir = new Vector3(Mathf.Cos(theta) * r, y, Mathf.Sin(theta) * r);
                // Hallucinations sit on an excluded layer (see AnomalyDirector), but in case one is not,
                // keep the ray going through anything marked IsHallucination: lies return no echo and no shadow.
                Vector3 o = origin; float left = maxDistance, travelled = 0f;
                while (left > 0f && Physics.Raycast(o, dir, out RaycastHit hit, left, echoLayers, QueryTriggerInteraction.Ignore))
                {
                    var surface = GetSurface(hit.collider);
                    if (surface == null || !surface.IsHallucination) { hits.Add((hit.point, travelled + hit.distance, surface)); break; }
                    travelled += hit.distance + 0.01f; left -= hit.distance + 0.01f; o = hit.point + dir * 0.01f;
                }
            }

            // Reveal points as the wave front expands, and play a few clicks (nearest of each material).
            hits.Sort((a, b) => a.dist.CompareTo(b.dist));
            var played = new HashSet<EchoMaterial>();
            float waveSpeed = 30f;   // visual speed of the ring, metres/second
            int idx = 0;
            float start = Time.time;
            while (idx < hits.Count)
            {
                float radius = (Time.time - start) * waveSpeed;
                while (idx < hits.Count && hits[idx].dist <= radius)
                {
                    var h = hits[idx];
                    EmitPoint(h.point, 1f - h.dist / maxDistance);
                    var mat = h.surface != null ? h.surface.Material : EchoMaterial.Concrete;
                    if (!played.Contains(mat) && h.surface != null && h.surface.Clip != null)
                    {
                        played.Add(mat);
                        StartCoroutine(PlayClickDelayed(h.point, h.surface.Clip, h.dist * 2f / audibleSoundSpeed));
                    }
                    idx++;
                }
                yield return null;
            }
        }

        IEnumerator PlayClickDelayed(Vector3 at, AudioClip clip, float delay)
        {
            yield return new WaitForSeconds(delay);
            var src = clickPool[clickIndex++ % clickPool.Count];
            src.transform.position = at;
            src.PlayOneShot(clip);
        }

        void EmitPoint(Vector3 pos, float strength)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = pos,
                startSize = pointSize * Mathf.Lerp(0.6f, 1.2f, strength),
                startLifetime = pointLifetime,
                startColor = new Color(echoColor.r, echoColor.g, echoColor.b, Mathf.Lerp(0.35f, 1f, strength)),
                velocity = Vector3.zero,
            };
            points.Emit(p, 1);
        }

        void BuildParticleSystem()
        {
            var go = new GameObject("EchoPoints");
            go.transform.SetParent(null);   // world space, independent of the player
            points = go.AddComponent<ParticleSystem>();
            points.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = points.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = rayCount * 4;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;

            var emission = points.emission; emission.enabled = false;
            var shape = points.shape; shape.enabled = false;

            var col = points.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (pointMaterial != null) renderer.material = pointMaterial;
            else Debug.LogWarning("[EchoPulse] Assign Point Material (HDRP/Unlit, Transparent, Additive).", this);
            points.Play();
        }
    }
}
