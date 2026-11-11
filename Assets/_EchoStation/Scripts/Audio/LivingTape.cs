using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoStation.Audio
{
    /// <summary>
    /// The living tape: the recorder on the bench in the middle of the corridor. It never stops by itself,
    /// it changes every loop, and sometimes it distorts into a horrifying line or a puzzle clue.
    /// The corridor only advances while the tape is playing (see LoopCorridor).
    ///
    /// Distortions (keep them unpredictable, never the same type two loops in a row):
    ///  Slow (pitch drops), Reverse (use a pre-reversed clip made in Audacity: negative pitch is unreliable),
    ///  Stutter (re-trigger short slices), Silence (cut, then a whisper), Answer (tape replies to the player).
    ///
    /// The clue is repeatable by rewinding (key R, 10 seconds back); the scare is not.
    /// Two sources: "source" is the tape itself, "fxSource" plays clicks and distorted inserts,
    /// so pausing and distortions never fight over the same AudioSource.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class LivingTape : MonoBehaviour
    {
        public enum Distortion { None, Slow, Reverse, Stutter, Silence, Answer }

        [System.Serializable]
        public class LoopEntry
        {
            public int step;                     // index in the corridor's loop sequence (0 = first pass)
            public AudioClip normal;
            public Distortion distortion = Distortion.None;
            public AudioClip distortedClip;      // the scare or the clue (Reverse: the reversed file)
            [Range(0f, 1f)] public float distortAt = 0.5f;   // when in the clip it breaks
            public bool isClue;                  // clues replay on rewind, scares don't
        }

        [SerializeField] LoopEntry[] entries;
        [SerializeField] AudioSource fxSource;
        [SerializeField] AudioClip stopClick, playClick, rewindClip;
        [SerializeField] float rewindSeconds = 10f;

        /// <summary>True while the tape is running (not paused by the player). Drives corridor progress.</summary>
        public bool IsPlaying => (source.isPlaying || distorting) && !userPaused;

        AudioSource source;
        LoopEntry current;
        int currentStep = -1;
        bool distorting, scarePlayed, stoppedOnce, userPaused, rewinding;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.loop = true;
            if (fxSource == null)
            {
                fxSource = gameObject.AddComponent<AudioSource>();
                fxSource.spatialBlend = 1f;
                fxSource.playOnAwake = false;
            }
            if (!HasEntryFor(0)) Debug.LogError("[LivingTape] No entry for step 0: the corridor would never advance.", this);
            OnStep(0);
        }

        public bool HasEntryFor(int step) =>
            entries != null && System.Array.Exists(entries, e => e.step == step && e.normal != null);

        /// <summary>Called by the corridor only when the step actually changes.</summary>
        public void OnStep(int step)
        {
            if (step == currentStep) return;
            currentStep = step;
            StopAllCoroutines();
            distorting = false; rewinding = false; scarePlayed = false; stoppedOnce = false;
            source.pitch = 1f;

            current = entries == null ? null : System.Array.Find(entries, e => e.step == step);
            if (current == null || current.normal == null) return;
            source.clip = current.normal;
            source.time = 0f;
            if (!userPaused) source.Play();
            if (current.distortion != Distortion.None && current.distortion != Distortion.Answer)
                StartCoroutine(DistortLater(current));
        }

        /// <summary>Called by the Interactor when the player presses E while looking at the recorder.</summary>
        public void Interact()
        {
            if (distorting || rewinding) return;   // you can't stop it mid-scream
            if (!userPaused)
            {
                userPaused = true;
                source.Pause();
                PlayFx(stopClick);
                stoppedOnce = true;
            }
            else
            {
                userPaused = false;
                PlayFx(playClick);
                if (source.clip != null) { if (source.time > 0f) source.UnPause(); else source.Play(); }
                // "Answer": once, after the player stopped it and started it again.
                if (stoppedOnce && current != null && current.distortion == Distortion.Answer && !scarePlayed)
                    StartCoroutine(PlayDistorted(current));
            }
        }

        public void Rewind()
        {
            if (rewinding || distorting || source.clip == null) return;
            StartCoroutine(RewindRoutine());
        }

        void Update()
        {
            // R works while the player is near and looking (the Interactor sets this flag).
            var kb = Keyboard.current;
            if (kb != null && Focused && kb.rKey.wasPressedThisFrame) Rewind();
        }

        /// <summary>Set by the Interactor while the player looks at the recorder.</summary>
        public bool Focused { get; set; }

        IEnumerator DistortLater(LoopEntry e)
        {
            // Wait in tape-time, so pausing the tape also delays the distortion.
            float target = e.normal.length * e.distortAt;
            while (source.time < target || userPaused) yield return null;
            yield return PlayDistorted(e);
        }

        IEnumerator PlayDistorted(LoopEntry e)
        {
            if (!e.isClue && scarePlayed) yield break;
            distorting = true;
            switch (e.distortion)
            {
                case Distortion.Slow:
                    for (float t = 0; t < 1.2f; t += Time.deltaTime) { source.pitch = Mathf.Lerp(1f, 0.55f, t / 1.2f); yield return null; }
                    source.pitch = 1f;
                    yield return InsertClip(e.distortedClip);
                    break;
                case Distortion.Reverse:
                case Distortion.Answer:
                    yield return InsertClip(e.distortedClip);
                    break;
                case Distortion.Stutter:
                    float pos = source.time;
                    for (int i = 0; i < 7; i++) { source.time = pos; yield return new WaitForSeconds(Random.Range(0.08f, 0.2f)); }
                    yield return InsertClip(e.distortedClip);
                    break;
                case Distortion.Silence:
                    source.Pause();
                    yield return new WaitForSeconds(2.5f);
                    if (e.distortedClip) { fxSource.PlayOneShot(e.distortedClip); yield return new WaitForSeconds(e.distortedClip.length); }
                    if (!userPaused) source.UnPause();
                    break;
            }
            if (!e.isClue) scarePlayed = true;
            distorting = false;
        }

        IEnumerator InsertClip(AudioClip clip)
        {
            if (clip == null) yield break;
            source.Pause();
            fxSource.PlayOneShot(clip);
            yield return new WaitForSeconds(clip.length);
            if (!userPaused) source.UnPause();
        }

        IEnumerator RewindRoutine()
        {
            rewinding = true;
            bool wasPaused = userPaused;
            source.Pause();
            PlayFx(rewindClip);
            yield return new WaitForSeconds(rewindClip ? rewindClip.length : 0.6f);
            source.time = Mathf.Max(0f, source.time - rewindSeconds);
            if (!wasPaused) source.UnPause();
            rewinding = false;
            // Clues come back on rewind; scares never repeat.
            if (current != null && current.isClue && current.distortion != Distortion.None)
                yield return PlayDistorted(current);
        }

        void PlayFx(AudioClip clip) { if (clip != null) fxSource.PlayOneShot(clip); }
    }
}
