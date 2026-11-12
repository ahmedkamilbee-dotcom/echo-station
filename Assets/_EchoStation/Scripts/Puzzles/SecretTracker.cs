using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.World;

namespace EchoStation.Puzzles
{
    /// <summary>
    /// The four optional secrets (docs/demo-design.md 10.3). Nothing on screen; everything is found with game tools.
    ///  1 the fifth cassette (after listening to the four victims' cassettes)
    ///  2 "while you were gone" (leave Ali's recorder recording for a full loop)
    ///  3 channel four
    ///  4 "this is yours, sir" (leave the station carrying the fifth cassette) -> changes the car ending
    /// </summary>
    public class SecretTracker : MonoBehaviour
    {
        static readonly HashSet<int> found = new HashSet<int>();
        static readonly HashSet<string> listened = new HashSet<string>();
        [SerializeField] GameObject fifthCassette;   // appears in the empty slot in the workshop
        static SecretTracker instance;

        void Awake() { instance = this; found.Clear(); listened.Clear(); CarEnding.HasFifthCassette = false; if (fifthCassette) fifthCassette.SetActive(false); }

        public static void Found(int id) { if (found.Add(id)) Debug.Log("[Secret] " + id); }
        public static bool Has(int id) => found.Contains(id);

        /// <summary>Call when a victim cassette (majid, jabbar, nouri, hassan) has been played in Ali's recorder.</summary>
        public static void ListenedVictimCassette(string id)
        {
            listened.Add(id);
            if (listened.Count >= 4 && !Has(1)) { Found(1); if (instance && instance.fifthCassette) instance.fifthCassette.SetActive(true); }
        }

        /// <summary>Call when Ali picks up the fifth cassette.</summary>
        public static void TookFifthCassette() => CarEnding.HasFifthCassette = true;

        /// <summary>Call when the player leaves the station (drain pipe) while carrying it.</summary>
        public static void LeftStation() { if (CarEnding.HasFifthCassette) Found(4); }

        /// <summary>Call when Ali's recorder was left recording for a whole loop and is played back.</summary>
        public static void RecorderLeftBehindPlayed() => Found(2);
    }
}
