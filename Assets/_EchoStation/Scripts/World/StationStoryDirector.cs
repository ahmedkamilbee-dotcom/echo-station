using System.Collections;
using UnityEngine;
using EchoStation.Audio;
using EchoStation.Haunt;
using EchoStation.Killer;
using EchoStation.Loop;
using EchoStation.Player;
using EchoStation.Puzzles;

namespace EchoStation.World
{
    /// <summary>
    /// Connects the room-loop to the story beats. The demo stays quiet, but every loop
    /// changes what the tape remembers and moves Ali closer to the pump hall encounter.
    /// </summary>
    public sealed class StationStoryDirector : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] LoopCorridor corridor;
        [SerializeField] AnomalyDirector anomalies;
        [SerializeField] LivingTape tape;
        [SerializeField] Subtitles subtitles;
        [SerializeField] AudioClip radioStatic, pipeKnocks, generatorDie;
        [SerializeField] GeneratorPower generator;
        [SerializeField] AutoDoor pumpHallDoor;
        [SerializeField] GameObject thresholdFigure;

        [Header("Mandatory loop puzzles")]
        [SerializeField] ClockPuzzle clockPuzzle;
        [SerializeField] KnockRhythmPuzzle knockPuzzle;
        [SerializeField] CassetteFlipPuzzle cassettePuzzle;
        [SerializeField] MuffledSearchPuzzle muffledPuzzle;

        [Header("Threats")]
        [SerializeField] KillerController killer;
        [SerializeField] CaughtSequence caughtSequence;
        [SerializeField] TantalStalker tantal;
        [SerializeField] Transform pumpCheckpoint;
        [SerializeField] DemoClock demoClock;

        bool finalPass;
        bool killerStarted, stationExited;

        void OnEnable()
        {
            if (corridor != null)
            {
                corridor.OnStepChanged += StepChanged;
                corridor.OnCorridorBroken += CorridorBroken;
            }
        }

        void OnDisable()
        {
            if (corridor != null)
            {
                corridor.OnStepChanged -= StepChanged;
                corridor.OnCorridorBroken -= CorridorBroken;
            }
        }

        void Start()
        {
            if (demoClock) demoClock.CorridorActive = true;
            if (pumpHallDoor) pumpHallDoor.Locked = true;
            if (thresholdFigure) thresholdFigure.SetActive(false);
            if (tantal) tantal.gameObject.SetActive(false);
            if (killer) killer.EndEncounter();
            StepChanged(corridor ? corridor.Step : 0);
        }

        void StepChanged(int step)
        {
            // Clues are short enough to read while walking and never become a persistent HUD.
            string code = step switch
            {
                1 => "A01", 2 => "A02", 4 => "D-rep", 6 => "A03", 8 => "A09",
                9 => "A06", 10 => "A12", 12 => "E03", 13 => "D-majid1", 15 => "A18",
                17 => "A19", _ => null
            };
            if (!string.IsNullOrEmpty(code)) StartCoroutine(PlayClue(code));

            if (step == 1 && clockPuzzle)
            {
                clockPuzzle.Begin();
                if (subtitles) subtitles.Describe("طابق عقرب الساعة ويا ساعة علي", "Set the wall clock to match Ali's watch", 4f);
            }
            if (step == 4 && knockPuzzle)
            {
                knockPuzzle.Begin();
                if (subtitles) subtitles.Describe("ثلاث نبضات متساوية... وبعدها اسكت", "Three even pulses, then wait in silence", 4f);
            }
            if (step == 7 && cassettePuzzle)
            {
                cassettePuzzle.Begin();
                if (subtitles) subtitles.Describe("أخرج الشريط، اقلبه، ورجّعه", "Eject the cassette, flip it, and put it back", 4f);
            }
            if (step == 12 && muffledPuzzle)
            {
                muffledPuzzle.Begin();
                if (subtitles) subtitles.Describe("النبضة تكشف الصوت المكتوم", "Use an echo pulse to find the muffled source", 4f);
            }

            if (step == 6 && generator) generator.Blackout(7f, true);
            if (step == 14 && generator) generator.Blackout(10f, true);
            if (step == 15 && thresholdFigure) thresholdFigure.SetActive(true);
            if (step == 16 && thresholdFigure) thresholdFigure.SetActive(false);
        }

        IEnumerator PlayClue(string code)
        {
            if (radioStatic && tape) tape.PlayCue(radioStatic, 0.28f);
            yield return new WaitForSeconds(0.75f);
            var line = LineTable.Get(code);
            if (line != null && subtitles) subtitles.Show(line.ar, line.en, 3.1f);
        }

        void CorridorBroken()
        {
            if (finalPass) return;
            finalPass = true;
            if (demoClock) demoClock.CorridorActive = false;
            if (pumpHallDoor) pumpHallDoor.Locked = false;
            if (subtitles) subtitles.Describe("الباب اللي كان مسدود انفتح", "The door that was sealed is open", 3f);
            if (generator) generator.Blackout(8f, true);
        }

        public void EnterPumpHall(AliController ali)
        {
            if (ali == null || killerStarted) return;
            killerStarted = true;
            if (caughtSequence && pumpCheckpoint) caughtSequence.SetCheckpoint(pumpCheckpoint);
            if (killer) killer.StartEncounter();
        }

        public void LeaveStation(AliController ali)
        {
            if (ali == null || stationExited) return;
            stationExited = true;
            EchoStation.Puzzles.SecretTracker.LeftStation();
            if (tantal) tantal.gameObject.SetActive(true);
            if (subtitles) subtitles.Describe("لا صوت للريح الآن", "The wind has gone silent", 2.6f);
        }

        public void ReachCar()
        {
            if (tantal) tantal.Blink();
        }
    }
}
