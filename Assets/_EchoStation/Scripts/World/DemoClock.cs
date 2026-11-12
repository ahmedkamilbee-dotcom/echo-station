using UnityEngine;
using UnityEngine.Events;
using EchoStation.Loop;

namespace EchoStation.World
{
    /// <summary>
    /// Keeps the demo at ~30 minutes without forcing the player and without any HUD
    /// (docs/demo-design.md, time budget). Target: 85% of players finish between 28 and 32 minutes.
    ///  - Timed beats: important events fire at a time, not on an action (UnityEvents in 'beats').
    ///  - Loop cap: if the player stays in one corridor step longer than 60 s, the next one triggers.
    ///  - Late player: past 19:00 and still before step 10, the empty/spare steps are removed.
    /// </summary>
    public class DemoClock : MonoBehaviour
    {
        [System.Serializable]
        public class Beat
        {
            public string name;
            [Tooltip("Seconds since the demo started")] public float at;
            public UnityEvent action;
            [HideInInspector] public bool fired;
        }

        [SerializeField] LoopCorridor corridor;
        [SerializeField] AnomalyDirector director;
        [SerializeField] float loopCapSeconds = 60f;
        [SerializeField] float lateCheckAt = 19f * 60f;
        [SerializeField] int lateStepThreshold = 12;   // index of anomaly "10" in the sequence
        [SerializeField] Beat[] beats;

        public static float Elapsed { get; private set; }
        public bool CorridorActive { get; set; }        // set by room triggers while the player is in the loop

        float stepTimer;
        int lastStep = -1;
        bool lateHandled;

        void Start() { Elapsed = 0f; }

        void Update()
        {
            Elapsed += Time.deltaTime;

            if (beats != null)
                foreach (var b in beats)
                    if (!b.fired && Elapsed >= b.at) { b.fired = true; b.action?.Invoke(); }

            if (!CorridorActive || corridor == null) return;
            if (corridor.Step != lastStep) { lastStep = corridor.Step; stepTimer = 0f; }
            stepTimer += Time.deltaTime;
            if (stepTimer > loopCapSeconds) { stepTimer = 0f; corridor.ForceAdvance(); }

            if (!lateHandled && Elapsed > lateCheckAt && corridor.Step < lateStepThreshold && director != null)
            {
                lateHandled = true;
                director.RemoveEmptyStepsAfter(corridor.Step);
            }
        }
    }
}
