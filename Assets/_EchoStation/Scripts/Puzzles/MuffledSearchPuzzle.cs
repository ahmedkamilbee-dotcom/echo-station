using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.World;

namespace EchoStation.Puzzles
{
    /// <summary>Pulse near metal things: everything rings except the "No smoking" sign (a dull thud). Pull it (E): a hidden speaker.</summary>
    public class MuffledSearchPuzzle : PuzzleBase, IInteractable
    {
        [SerializeField] float pulseRange = 3f;
        [SerializeField] Transform sign;
        [SerializeField] AudioSource hiddenSpeaker;     // loops "muffled" over the big tape
        public bool Focused { get; set; }
        bool revealed;

        void OnEnable() { EchoPulse.OnPulse += Heard; }
        void OnDisable() { EchoPulse.OnPulse -= Heard; }
        void Heard(Vector3 at, float r)
        {
            if (!Active || revealed || Vector3.Distance(at, transform.position) >= pulseRange) return;
            revealed = true;
            if (hiddenSpeaker && hiddenSpeaker.clip) hiddenSpeaker.Play();
        }

        public void Interact()
        {
            if (!Active || !revealed) return;
            if (sign) sign.localRotation = Quaternion.Euler(0, 0, 80f);   // swings open
            Solve();
        }

        protected override void OnSolvedInternal(bool bySelf)
        {
            if (sign && bySelf) sign.gameObject.SetActive(false);         // falls by itself
            if (hiddenSpeaker) hiddenSpeaker.Stop();
        }
    }
}
