using System.Collections;
using UnityEngine;

namespace EchoStation.World
{
    /// <summary>
    /// Minimal subtitles stub (full version: build-queue task 13, with RTL Arabic support).
    /// Small, bottom of screen, no speaker name. Language from Subtitles.Language ("ar" / "en" / "off").
    /// Uses TextMeshPro if present; this stub logs to the console so the demo runs without it.
    /// </summary>
    public class Subtitles : MonoBehaviour
    {
        public static string Language = "ar";

        public void Show(string arabic, string english, float seconds)
        {
            if (Language == "off") return;
            StopAllCoroutines();
            StartCoroutine(ShowRoutine(Language == "en" ? english : arabic, seconds));
        }

        protected virtual IEnumerator ShowRoutine(string line, float seconds)
        {
            Debug.Log("[Subtitle] " + line);
            yield return new WaitForSeconds(seconds);
        }
    }
}
