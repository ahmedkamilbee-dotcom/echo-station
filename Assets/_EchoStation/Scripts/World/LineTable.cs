using System.Collections.Generic;
using UnityEngine;

namespace EchoStation.World
{
    /// <summary>
    /// All spoken lines, keyed by the codes in docs/team/tape-and-voice-script.md (A05, K01, ...).
    /// Data lives in a JSON TextAsset (Assets/_EchoStation/Data/lines.json):
    /// { "lines": [ { "code": "K01", "ar": "وين چنت أستاذ؟", "en": "Where were you, sir?" }, ... ] }
    /// Audio clips are found by the same code: Resources/Voice/K01 (optional during grey-box).
    /// </summary>
    public class LineTable : MonoBehaviour
    {
        [System.Serializable] public class Line { public string code, ar, en; }
        [System.Serializable] class Data { public Line[] lines; }

        [SerializeField] TextAsset json;
        [SerializeField] Subtitles subtitles;
        [SerializeField] AudioSource defaultSource;

        static readonly Dictionary<string, Line> table = new Dictionary<string, Line>();
        static LineTable instance;

        void Awake()
        {
            instance = this;
            table.Clear();
            if (json == null) return;
            var d = JsonUtility.FromJson<Data>(json.text);
            if (d?.lines != null) foreach (var l in d.lines) if (!string.IsNullOrEmpty(l.code)) table[l.code] = l;
        }

        public static Line Get(string code) => table.TryGetValue(code, out var l) ? l : null;

        /// <summary>Play a line by code with its subtitle. Returns its duration.</summary>
        public static float Say(string code, AudioSource source = null, bool reversedSubtitle = false)
        {
            if (instance == null) return 0f;
            var l = Get(code);
            var clip = Resources.Load<AudioClip>("Voice/" + code);
            var src = source ? source : instance.defaultSource;
            if (src && clip) src.PlayOneShot(clip);
            float dur = clip ? clip.length : 2.5f;
            if (l != null && instance.subtitles) instance.subtitles.Show(l.ar, l.en, dur + 0.4f, reversedSubtitle);
            return dur;
        }
    }
}
