using UnityEngine;
using EchoStation.Player;

namespace EchoStation.Loop
{
    /// <summary>
    /// The looping corridor. The player walks through door B and arrives at door A of the same corridor
    /// without noticing: a trigger at the blind corner teleports them to the matching point at the start,
    /// keeping their exact relative position and facing.
    ///
    /// Setup (grey box):
    ///  - Build ONE corridor with ProBuilder.
    ///  - Put an empty "ExitAnchor" at the blind corner and an "EntryAnchor" at the same spot near door A.
    ///    Both anchors must face the same way relative to the corridor.
    ///  - Put a BoxCollider (Is Trigger) at the blind corner and add this script to it.
    ///  - Optional: a second trigger near door A with TurnBackTrigger, for anomalies that require turning back.
    ///
    /// Progress is a STEP index into the AnomalyDirector's ordered sequence (0, 1..6, 6b, 7..9, empty, 10..15, spare).
    /// If the tape is stopped, the loop repeats with no progress (the corridor only moves if the tape moves).
    /// Turn off Motion Blur / TAA in the corridor volume, or the teleport frame shows a smear.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class LoopCorridor : MonoBehaviour
    {
        [SerializeField] Transform exitAnchor;
        [SerializeField] Transform entryAnchor;
        [SerializeField] AnomalyDirector director;
        [SerializeField] EchoStation.Audio.LivingTape tape;

        public int Step { get; private set; }
        public event System.Action<int> OnStepChanged;
        public event System.Action OnCorridorBroken;

        bool broken;

        void Reset() { GetComponent<BoxCollider>().isTrigger = true; }

        void Start()
        {
            if (director != null) director.ApplyStep(0);
        }

        void OnTriggerEnter(Collider other)
        {
            if (broken || exitAnchor == null || entryAnchor == null) return;
            var ali = other.GetComponent<AliController>();
            if (ali == null) return;

            bool tapeRunning = tape == null || (tape.HasEntryFor(Step) && tape.IsPlaying);
            bool canAdvance = tapeRunning && !EchoStation.Puzzles.PuzzleBase.AnyBlocking
                              && (director == null || director.ForwardPassIsCorrect());

            // The last pass breaks into the pump hall. Keep Ali on the far side of the
            // loop so he can walk through the newly revealed door instead of teleporting back.
            if (canAdvance && director != null && Step >= director.StepCount - 1)
            {
                broken = true;
                OnCorridorBroken?.Invoke();
                return;
            }

            // Relative position & yaw from exit -> entry.
            Vector3 local = exitAnchor.InverseTransformPoint(ali.transform.position);
            Vector3 target = entryAnchor.TransformPoint(local);
            float yawDelta = entryAnchor.eulerAngles.y - exitAnchor.eulerAngles.y;
            ali.Teleport(target, yawDelta);

            if (canAdvance) Advance();
        }

        /// <summary>Called by TurnBackTrigger when the player turns back on an anomaly that requires it.</summary>
        public void TurnBackPass()
        {
            if (broken || director == null || !director.CurrentRequiresTurnBack) return;
            Advance();
        }

        /// <summary>Used by DemoClock when the player lingers too long in one loop.</summary>
        public void ForceAdvance()
        {
            if (!broken && !EchoStation.Puzzles.PuzzleBase.AnyBlocking) Advance();
        }

        void Advance()
        {
            Step++;
            if (director != null && Step >= director.StepCount)
            {
                broken = true;
                OnCorridorBroken?.Invoke();
                return;
            }
            if (director != null) director.ApplyStep(Step);
            if (tape != null) tape.OnStep(Step);
            OnStepChanged?.Invoke(Step);
        }
    }
}
