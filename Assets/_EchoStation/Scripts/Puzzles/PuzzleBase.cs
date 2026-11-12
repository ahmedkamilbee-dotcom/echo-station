using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EchoStation.Puzzles
{
    /// <summary>
    /// Base for the demo puzzles (docs/demo-design.md 9.1). While a mandatory puzzle is active the tape is "stuck"
    /// and the corridor does not advance. A four-step help ladder runs with no UI at all:
    ///  1 the world points (light, drip, sound grows near the right thing)
    ///  2 the tape repeats the clue more clearly
    ///  3 the killer "helps" (knocks behind it, a glass already open, a tea glass next to the answer)
    ///  4 it solves itself while the player looks away, and the reward moment is missed.
    /// "Faster help" in the options halves the times.
    /// </summary>
    public abstract class PuzzleBase : MonoBehaviour
    {
        public static bool FasterHelp;
        static readonly HashSet<PuzzleBase> blocking = new HashSet<PuzzleBase>();
        public static bool AnyBlocking => blocking.Count > 0;

        [SerializeField] protected bool mandatory = true;
        [SerializeField] float[] helpTimes = { 25f, 45f, 75f, 110f };
        [SerializeField] UnityEvent[] helpSteps = new UnityEvent[4];
        [SerializeField] protected UnityEvent onSolved;
        [SerializeField] protected Transform cam;

        public bool Active { get; private set; }
        public bool Solved { get; private set; }
        public event System.Action OnSolved;

        float timer; int helpIndex;

        /// <summary>Called by the anomaly director / tape when this puzzle's loop starts.</summary>
        public virtual void Begin()
        {
            if (Solved) return;
            Active = true; timer = 0f; helpIndex = 0;
            if (mandatory) blocking.Add(this);
        }

        protected virtual void Update()
        {
            if (!Active || Solved) return;
            timer += Time.deltaTime * (FasterHelp ? 2f : 1f);
            if (helpIndex < helpTimes.Length && timer >= helpTimes[helpIndex])
            {
                if (helpIndex < helpSteps.Length) helpSteps[helpIndex]?.Invoke();
                if (helpIndex == helpTimes.Length - 1 && mandatory) StartCoroutine(SelfSolveWhenUnseen());
                helpIndex++;
            }
        }

        System.Collections.IEnumerator SelfSolveWhenUnseen()
        {
            // Solves itself only while the player is looking away from it.
            while (!Solved)
            {
                if (cam == null || Vector3.Dot(cam.forward, (transform.position - cam.position).normalized) < 0.2f) { Solve(true); yield break; }
                yield return null;
            }
        }

        protected void Solve(bool bySelf = false)
        {
            if (Solved) return;
            Solved = true; Active = false;
            blocking.Remove(this);
            OnSolvedInternal(bySelf);
            onSolved?.Invoke();
            OnSolved?.Invoke();
        }

        protected virtual void OnSolvedInternal(bool bySelf) { }

        void OnDestroy() { blocking.Remove(this); }
    }
}
