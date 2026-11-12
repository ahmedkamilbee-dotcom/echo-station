using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.World;

namespace EchoStation.Puzzles
{
    /// <summary>The tape plays backwards: eject (E on the recorder slot), flip, insert. Side B starts.</summary>
    public class CassetteFlipPuzzle : PuzzleBase, IInteractable
    {
        [SerializeField] AudioSource recorder;
        [SerializeField] AudioClip ejectClick, insertClick, sideB;
        [SerializeField] GameObject cassetteOut;   // visible cassette on the bench when ejected
        public bool Focused { get; set; }
        int stage;   // 0 in, 1 ejected, 2 flipped

        public void Interact()
        {
            if (!Active) return;
            stage++;
            if (stage == 1) { if (recorder) { recorder.Stop(); recorder.PlayOneShot(ejectClick); } if (cassetteOut) cassetteOut.SetActive(true); }
            else if (stage == 2) { if (cassetteOut) cassetteOut.transform.Rotate(0, 180, 0); }
            else if (stage >= 3) { if (cassetteOut) cassetteOut.SetActive(false); Solve(); }
        }

        protected override void OnSolvedInternal(bool bySelf)
        {
            if (!recorder) return;
            recorder.PlayOneShot(insertClick);
            if (sideB) { recorder.clip = sideB; recorder.Play(); }
        }
    }
}
