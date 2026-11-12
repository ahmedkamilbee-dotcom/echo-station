using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.World;

namespace EchoStation.Puzzles
{
    /// <summary>Ali's wrist watch: stopped at 4:52 until the first pulse, then it runs in real time.</summary>
    public static class AliWatch
    {
        public static float StartedAtElapsed = 55f;
        public static float MinutesPastFour => 52f + Mathf.Max(0f, DemoClock.Elapsed - StartedAtElapsed) / 60f;
    }

    /// <summary>Open the wall clock's glass (E), hold E and move the mouse to set it to Ali's watch (±2 min).</summary>
    public class ClockPuzzle : PuzzleBase, IInteractable
    {
        [SerializeField] Transform minuteHand, hourHand;
        [SerializeField] AudioSource tick;
        [SerializeField] float degreesPerMouse = 0.4f;
        public bool Focused { get; set; }
        float minutes = 46f;   // shows 5:46 -> jumps to 5:47 when the loop starts
        bool opened;

        public override void Begin() { base.Begin(); minutes = 47f; if (tick) tick.Stop(); Draw(); }
        public void Interact() { opened = true; }

        protected override void Update()
        {
            base.Update();
            if (!Active || !opened || !Focused) return;
            var kb = Keyboard.current; var m = Mouse.current;
            if (kb == null || m == null || !kb.eKey.isPressed) return;
            minutes += m.delta.ReadValue().x * degreesPerMouse / 6f;   // 6 degrees per minute
            Draw();
            float target = 4f * 60f + AliWatch.MinutesPastFour;        // 4:5x
            float shown = 5f * 60f + minutes;                           // "minutes" are counted from 5:00 (negative = before 5)
            if (Mathf.Abs(Mathf.DeltaAngle(shown * 6f, target * 6f)) / 6f <= 2f) Solve();
        }

        void Draw()
        {
            if (minuteHand) minuteHand.localRotation = Quaternion.Euler(0, 0, -minutes * 6f);
            if (hourHand) hourHand.localRotation = Quaternion.Euler(0, 0, -(5f * 30f + minutes * 0.5f));
        }

        protected override void OnSolvedInternal(bool bySelf) { if (tick) tick.Play(); }
    }
}
