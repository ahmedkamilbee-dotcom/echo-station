using UnityEngine;

namespace EchoStation.Audio
{
    /// <summary>
    /// Turns any clean recording into a 1994 radio, a field walkie-talkie, or a worn cassette, in real time.
    /// Put it on the same GameObject as the AudioSource (it must come AFTER the AudioSource in the inspector).
    /// Record lines clean on a phone, drop them in, pick a preset. No external plugins.
    ///
    /// Radio: narrow band (300-3400 Hz), soft clipping, static bed, occasional squelch dropouts.
    /// Walkie: even narrower, harder clipping, louder hiss.
    /// Cassette: wider band, gentle hiss, wow & flutter (slow pitch wobble via AudioSource.pitch).
    /// "Corruption" (0-1) can be driven by the living tape when it distorts.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class OldMediaFilter : MonoBehaviour
    {
        public enum Preset { Radio, Walkie, Cassette }

        [SerializeField] Preset preset = Preset.Radio;
        [Range(0f, 1f)] public float corruption = 0f;
        [SerializeField, Range(0f, 0.2f)] float noiseLevel = 0.012f;

        AudioSource source;
        float sampleRate;
        float lpState1, lpState2, hpState, hpPrev;
        float lowCut, highCut, drive;
        uint rng = 2463534242u;
        float squelchTimer;
        bool squelched;
        float basePitch = 1f;
        volatile bool active;   // audio thread can't call source.isPlaying
        float gain = 1f;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            sampleRate = AudioSettings.outputSampleRate;
            basePitch = source.pitch;
            Configure();
        }

        void OnValidate() { Configure(); }

        void Configure()
        {
            switch (preset)
            {
                case Preset.Radio:    lowCut = 300f; highCut = 3400f; drive = 2.2f; break;
                case Preset.Walkie:   lowCut = 450f; highCut = 2600f; drive = 3.5f; break;
                case Preset.Cassette: lowCut = 80f;  highCut = 7000f; drive = 1.3f; break;
            }
        }

        void Update()
        {
            if (source != null) { active = source.isPlaying; gain = source.volume; }
            if (preset == Preset.Cassette && source != null)
            {
                // wow (slow) + flutter (fast), stronger when corrupted
                float wow = Mathf.Sin(Time.time * 2f * Mathf.PI * 0.6f) * 0.006f;
                float flutter = Mathf.Sin(Time.time * 2f * Mathf.PI * 7f) * 0.002f;
                source.pitch = basePitch * (1f + (wow + flutter) * (1f + corruption * 6f));
            }
            if (preset != Preset.Cassette)
            {
                // squelch dropouts: rare when clean, frequent when corrupted
                squelchTimer -= Time.deltaTime;
                if (squelchTimer <= 0f)
                {
                    squelched = Random.value < 0.08f + corruption * 0.5f;
                    squelchTimer = squelched ? Random.Range(0.05f, 0.25f) : Random.Range(0.8f, 3f);
                }
            }
        }

        float Noise()
        {
            rng ^= rng << 13; rng ^= rng >> 17; rng ^= rng << 5;
            return (rng / (float)uint.MaxValue) * 2f - 1f;
        }

        // Runs on the audio thread.
        void OnAudioFilterRead(float[] data, int channels)
        {
            if (sampleRate <= 0f || !active) return;   // no hiss from a silent source (was a constant loud white noise)
            float hpA = Mathf.Exp(-2f * Mathf.PI * lowCut / sampleRate);
            float lpA = 1f - Mathf.Exp(-2f * Mathf.PI * highCut / sampleRate);
            float d = drive * (1f + corruption * 2f);
            float nl = noiseLevel * (1f + corruption * 4f);
            bool mute = squelched;

            for (int i = 0; i < data.Length; i += channels)
            {
                float x = 0f;
                for (int c = 0; c < channels; c++) x += data[i + c];
                x /= channels;

                // high-pass (one pole)
                float hp = hpA * (hpState + x - hpPrev);
                hpPrev = x; hpState = hp;
                // low-pass (two poles)
                lpState1 += lpA * (hp - lpState1);
                lpState2 += lpA * (lpState1 - lpState2);
                float y = lpState2;

                // soft clip + bit of crunch when corrupted
                y = (float)System.Math.Tanh(y * d) / (float)System.Math.Tanh(d);
                if (corruption > 0.5f) y = Mathf.Round(y * 24f) / 24f;

                y += Noise() * nl * gain;
                if (mute) y *= 0.05f;

                for (int c = 0; c < channels; c++) data[i + c] = y;
            }
        }
    }
}
