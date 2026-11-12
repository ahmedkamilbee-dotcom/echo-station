using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.World;

namespace EchoStation.Puzzles
{
    /// <summary>Pulse three times at an even rhythm (0.3-1 s apart) next to door B, then 2 s silence: the door answers.</summary>
    public class KnockRhythmPuzzle : PuzzleBase
    {
        [SerializeField] float range = 2.2f;
        [SerializeField] AudioSource behindDoor;
        [SerializeField] AudioClip answerKnocks;
        [SerializeField] GameObject doorLock;   // disabled when solved
        readonly List<float> taps = new List<float>();

        void OnEnable() { EchoPulse.OnPulse += Heard; }
        void OnDisable() { EchoPulse.OnPulse -= Heard; }

        void Heard(Vector3 at, float r)
        {
            if (!Active || Vector3.Distance(at, transform.position) > range) return;
            taps.Add(Time.time);
            if (taps.Count > 3) taps.RemoveAt(0);
        }

        protected override void Update()
        {
            base.Update();
            if (!Active || taps.Count < 3) return;
            if (Time.time - taps[2] < 2f) return;                  // wait for the silence
            float a = taps[1] - taps[0], b = taps[2] - taps[1];
            bool even = a >= 0.3f && a <= 1f && b >= 0.3f && b <= 1f && Mathf.Abs(a - b) < 0.25f;
            taps.Clear();
            if (even) Solve();
        }

        protected override void OnSolvedInternal(bool bySelf)
        {
            if (behindDoor && answerKnocks) behindDoor.PlayOneShot(answerKnocks);
            if (doorLock) doorLock.SetActive(false);
        }
    }
}
