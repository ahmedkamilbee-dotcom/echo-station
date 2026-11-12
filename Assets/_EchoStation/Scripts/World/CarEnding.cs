using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using EchoStation.Player;
using EchoStation.Haunt;

namespace EchoStation.World
{
    /// <summary>
    /// Scene 7: the police car and the three knocks (docs/demo-design.md).
    /// Ali gets in (trigger at the driver door + E) and the doors lock. Look-only from here.
    /// Tantal peak (hooked via onTantalPeak) > hands over ears 6 s of silence > the adhan (clean, never distorted)
    /// > 10 s of quiet > KNOCK KNOCK KNOCK (second loud hit) > Khalaf at the window, half his face lit
    /// by the orange hazards > his lines (K01-K04, subtitled) > he offers the tea glass in silence
    /// > black > the tape keeps turning for 3 s > title > wishlist card.
    /// If the player carries the fifth cassette (secret 4), Khalaf takes it, puts it back, and says K06.
    /// </summary>
    public class CarEnding : MonoBehaviour
    {
        public static bool HasFifthCassette;   // set by SecretTracker

        [SerializeField] AliController ali;
        [SerializeField] Transform seatCamera;            // camera pose inside the car
        [SerializeField] Transform cameraPivot;
        [SerializeField] GameObject khalaf;               // half-face model at the driver window, starts disabled
        [SerializeField] GameObject teaGlassOffer;        // starts disabled
        [SerializeField] AudioSource car;                 // inside the car (2D-ish)
        [SerializeField] AudioSource window;              // at the driver window (3D)
        [SerializeField] AudioClip doorLock, adhan, knocks, tapeSpin;
        [SerializeField] AudioClip k01, k02, k03, k04, k06;
        [SerializeField] Image black, titleCard, wishlistCard;   // end cards, not HUD
        [SerializeField] TMP_Text titleText, wishlistText;
        [SerializeField] Subtitles subtitles;             // optional
        [SerializeField] TantalStalker tantal;
        [SerializeField] UnityEvent onTantalPeak, onHandsOverEars, onEnd;

        bool started, near;

        void OnTriggerEnter(Collider c) { if (c.GetComponent<AliController>()) near = true; }
        void OnTriggerExit(Collider c) { if (c.GetComponent<AliController>()) near = false; }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (!started && near && kb != null && kb.eKey.wasPressedThisFrame) StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            started = true;
            ali.InputEnabled = false;
            if (cameraPivot && seatCamera) cameraPivot.SetPositionAndRotation(seatCamera.position, seatCamera.rotation);
            Play(car, doorLock);
            yield return new WaitForSeconds(2f);

            onTantalPeak?.Invoke();                 // TantalStalker drives the blink approach + sound removal
            if (tantal) tantal.Blink();
            yield return new WaitForSeconds(6f);
            onHandsOverEars?.Invoke();              // 6 s, player loses control (2nd of 2 times)
            yield return new WaitForSeconds(6f);

            Play(car, adhan);                       // far, muffled, clean: the anchor of reality
            yield return new WaitForSeconds(adhan ? Mathf.Min(adhan.length, 14f) : 6f);
            yield return new WaitForSeconds(10f);   // quiet

            Play(window, knocks);                   // LOUD HIT #2
            yield return new WaitForSeconds(1.6f);
            if (khalaf) khalaf.SetActive(true);
            yield return Say(k01, "وين چنت أستاذ؟", "Where were you, sir?", 0.6f);
            yield return Say(k02, "ساعة كاملة أدوّر عليك.", "A whole hour I've been looking for you.", 0.8f);
            yield return Say(k03, "چـ... چنت... چنت تصيّح. سمعتك من بعيد.", "You... you were screaming. I heard you from far away.", 3f);
            yield return Say(k04, "طنطل.", "Tantal.", 1.5f);

            if (HasFifthCassette) yield return Say(k06, "هذا... مالك أستاذ.", "This... is yours, sir.", 1.5f);

            if (teaGlassOffer) teaGlassOffer.SetActive(true);   // offered in silence, no line
            yield return new WaitForSeconds(4f);

            yield return Fade(black, 0f, 1f, 0.8f);
            Play(car, tapeSpin);
            yield return new WaitForSeconds(3f);
            yield return Fade(titleCard, 0f, 1f, 1.5f);
            if (titleText) titleText.gameObject.SetActive(true);
            yield return new WaitForSeconds(4f);
            yield return Fade(titleCard, 1f, 0f, 1f);
            yield return Fade(wishlistCard, 0f, 1f, 1f);
            if (wishlistText) wishlistText.gameObject.SetActive(true);
            onEnd?.Invoke();
        }

        IEnumerator Say(AudioClip clip, string ar, string en, float pauseAfter)
        {
            if (subtitles) subtitles.Show(ar, en, clip ? clip.length + 0.4f : 3.4f);
            Play(window, clip);
            yield return new WaitForSeconds((clip ? clip.length : 3.4f) + pauseAfter);
        }

        static void Play(AudioSource s, AudioClip c) { if (s && c) s.PlayOneShot(c); }

        static IEnumerator Fade(Image img, float from, float to, float dur)
        {
            if (!img) yield break;
            img.enabled = true;
            for (float t = 0; t < dur; t += Time.deltaTime) { var c = img.color; c.a = Mathf.Lerp(from, to, t / dur); img.color = c; yield return null; }
            var cc = img.color; cc.a = to; img.color = cc;
        }
    }
}
