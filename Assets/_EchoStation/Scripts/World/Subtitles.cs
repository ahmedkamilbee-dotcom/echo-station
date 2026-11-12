using System.Collections;
using UnityEngine;
using TMPro;

namespace EchoStation.World
{
    /// <summary>
    /// Subtitles: small, bottom of screen, no speaker name, Arabic or English (or off).
    /// Arabic needs letter shaping: install RTLTMPro (MIT, by sorencoder, via OpenUPM; check the licence) and use an
    /// RTLTextMeshPro component as 'label'. It derives from TMP_Text, so this script works with either.
    /// Without it, Arabic shows with disconnected letters (fine for grey-box testing).
    ///
    /// Distortion: when the tape plays backwards the subtitle is shown reversed and in brackets,
    /// so every sound clue also exists in writing (accessibility, docs/demo-design.md 9.1).
    /// </summary>
    public class Subtitles : MonoBehaviour
    {
        public static string Language = "ar";   // "ar" | "en" | "off"

        [SerializeField] TMP_Text label;
        [SerializeField] CanvasGroup group;
        [SerializeField] float fade = 0.2f;

        public void Show(string arabic, string english, float seconds) => Show(arabic, english, seconds, false);

        public void Show(string arabic, string english, float seconds, bool reversed)
        {
            if (Language == "off" || label == null) return;
            string line = Language == "en" ? english : arabic;
            if (reversed) { var c = line.ToCharArray(); System.Array.Reverse(c); line = "[" + new string(c) + "]"; }
            StopAllCoroutines();
            StartCoroutine(ShowRoutine(line, seconds));
        }

        /// <summary>Sound descriptions for the hard of hearing, e.g. [knocks behind the door].</summary>
        public void Describe(string arabic, string english, float seconds = 2.5f) =>
            Show("[" + arabic + "]", "[" + english + "]", seconds, false);

        IEnumerator ShowRoutine(string line, float seconds)
        {
            label.text = line;
            label.isRightToLeftText = Language == "ar" && label.GetType() == typeof(TextMeshProUGUI);
            yield return Fade(1f);
            yield return new WaitForSeconds(seconds);
            yield return Fade(0f);
        }

        IEnumerator Fade(float to)
        {
            if (!group) yield break;
            float from = group.alpha;
            for (float t = 0; t < fade; t += Time.unscaledDeltaTime) { group.alpha = Mathf.Lerp(from, to, t / fade); yield return null; }
            group.alpha = to;
        }
    }
}
