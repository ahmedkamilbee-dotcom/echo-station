using UnityEngine;

namespace EchoStation.Haunt
{
    /// <summary>
    /// A sound that starts when Ali comes near, and stops dead the moment he gets close
    /// (a door being beaten from inside, a phone ringing in an empty room). The silence is the scare.
    /// Optionally jolts a transform (the door) with the loudness of the sound.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ApproachSound : MonoBehaviour
    {
        [SerializeField] float startDistance = 8f;
        [SerializeField] float stopDistance = 1.7f;
        [SerializeField] Transform shake;
        [SerializeField] float shakeAngle = 3f;

        AudioSource src;
        bool started, done, hasRest;
        Quaternion rest;
        readonly float[] buf = new float[64];

        void OnEnable()
        {
            src = GetComponent<AudioSource>();
            src.Stop();
            started = done = false;
            if (shake) { rest = shake.localRotation; hasRest = true; }
        }

        void OnDisable() { if (shake && hasRest) shake.localRotation = rest; }

        void Update()
        {
            var cam = Camera.main;
            if (cam == null || done) return;
            float d = Vector3.Distance(cam.transform.position, transform.position);
            if (!started && d < startDistance) { started = true; src.Play(); }
            if (!started) return;
            if (d < stopDistance)
            {
                done = true; src.Stop();
                if (shake) shake.localRotation = rest;
                return;
            }
            if (shake)
            {
                src.GetOutputData(buf, 0);
                float amp = 0f;
                for (int i = 0; i < buf.Length; i++) amp = Mathf.Max(amp, Mathf.Abs(buf[i]));
                shake.localRotation = rest * Quaternion.Euler(0f, Mathf.Clamp01(amp * 4f) * shakeAngle, 0f);
            }
        }
    }
}
