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
        float minutes = 47f;   // shows 5:47 -> the watch points back to 4:52
        bool opened;

        public override void Begin() { base.Begin(); minutes = 47f; if (tick) tick.Stop(); Draw(); }
        public void Interact() { opened = true; }

        protected override void Update()
        {
            base.Update();
            if (!Active || !opened || !Focused) return;
            var kb = Keyboard.current; var m = Mouse.current;
            if (kb == null || m == null || !kb.eKey.isPressed) return;
            minutes = Mathf.Clamp(minutes + m.delta.ReadValue().x * degreesPerMouse / 6f, -60f, 60f);
            Draw();
            // The dial is authored around 5:00, so 4:52 is -8 minutes from that origin.
            // Compare the full clock position; checking only the minute hand also accepted 5:52.
            float targetMinutes = AliWatch.MinutesPastFour - 60f;
            if (Mathf.Abs(minutes - targetMinutes) <= 2f) Solve();
        }

        void Draw()
        {
            if (minuteHand) minuteHand.localRotation = Quaternion.Euler(0, 0, -minutes * 6f);
            if (hourHand) hourHand.localRotation = Quaternion.Euler(0, 0, -(5f * 30f + minutes * 0.5f));
        }

        protected override void OnSolvedInternal(bool bySelf) { if (tick) tick.Play(); }
    }
}
