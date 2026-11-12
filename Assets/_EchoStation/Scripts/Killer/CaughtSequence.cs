using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using EchoStation.Player;

namespace EchoStation.Killer
{
    /// <summary>
    /// Being caught: 2-3 seconds, never a gore close-up. The camera is pulled toward him, a cassette
    /// tape is wound into Ali's ear (the killers' signature), the world goes muffled, then black.
    /// Ali returns to the start of the current room (checkpoint) after ~4 s total. It costs little time.
    /// </summary>
    public class CaughtSequence : MonoBehaviour
    {
        [SerializeField] KillerController killer;
        [SerializeField] AliController ali;
        [SerializeField] Transform cameraPivot;
        [SerializeField] Image black;                       // full-screen fade (not HUD)
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioClip tapeWind, muffledThud;
        [SerializeField] AudioLowPassFilter listenerMuffle;
        [SerializeField] Transform checkpoint;              // set by room triggers (SetCheckpoint)

        public static event System.Action OnRespawn;
        bool running;

        void OnEnable() { if (killer) killer.OnCaughtAli += Caught; }
        void OnDisable() { if (killer) killer.OnCaughtAli -= Caught; }

        public void SetCheckpoint(Transform t) => checkpoint = t;

        void Caught() { if (!running) StartCoroutine(Run()); }

        IEnumerator Run()
        {
            running = true;
            ali.InputEnabled = false;
            Quaternion start = cameraPivot.rotation;
            Vector3 look = (killer.transform.position + Vector3.up * 1.6f) - cameraPivot.position;
            Quaternion toKiller = Quaternion.LookRotation(look);
            if (sfx && tapeWind) sfx.PlayOneShot(tapeWind);

            for (float t = 0; t < 1.2f; t += Time.deltaTime)
            {
                float k = t / 1.2f;
                cameraPivot.rotation = Quaternion.Slerp(start, toKiller, Mathf.SmoothStep(0, 1, k));
                if (listenerMuffle) listenerMuffle.cutoffFrequency = Mathf.Lerp(22000f, 500f, k);
                SetBlack(Mathf.Lerp(0f, 0.6f, k));
                yield return null;
            }
            if (sfx && muffledThud) sfx.PlayOneShot(muffledThud);
            for (float t = 0; t < 0.8f; t += Time.deltaTime) { SetBlack(Mathf.Lerp(0.6f, 1f, t / 0.8f)); yield return null; }
            yield return new WaitForSeconds(1.2f);

            // Respawn
            if (checkpoint) ali.Teleport(checkpoint.position, checkpoint.eulerAngles.y - ali.transform.eulerAngles.y);
            cameraPivot.localRotation = Quaternion.identity;
            killer.EndEncounter();
            OnRespawn?.Invoke();

            for (float t = 0; t < 1f; t += Time.deltaTime)
            {
                SetBlack(1f - t);
                if (listenerMuffle) listenerMuffle.cutoffFrequency = Mathf.Lerp(500f, 22000f, t);
                yield return null;
            }
            SetBlack(0f);
            if (listenerMuffle) listenerMuffle.cutoffFrequency = 22000f;
            ali.InputEnabled = true;
            running = false;
        }

        void SetBlack(float a)
        {
            if (!black) return;
            black.enabled = a > 0.001f;
            var c = black.color; c.a = a; black.color = c;
        }
    }
}
