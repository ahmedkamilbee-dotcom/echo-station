using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using EchoStation.Player;
using EchoStation.World;

namespace EchoStation.UI
{
    /// <summary>
    /// The only screens in the game (no in-game HUD):
    ///  1. Warning screen before the first frame: photosensitivity warning + "Reduce flashing" toggle + continue.
    ///  2. Pause menu (Esc): resume, options, quit.
    ///  3. Options: mouse sensitivity, English subtitles on/off, reduce flashing, breath hold as toggle.
    /// Settings are saved in PlayerPrefs. Build the canvases in the scene and wire the references.
    /// The demo UI is English-only because the current TMP setup does not shape Arabic text correctly.
    /// </summary>
    public class GameMenus : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] GameObject warningScreen;
        [SerializeField] GameObject pauseScreen;
        [SerializeField] GameObject optionsScreen;
        [SerializeField] MonoBehaviour[] disableWhilePaused;   // e.g. WakeSequence is fine; AliController input is handled below

        [Header("Warning")]
        [SerializeField] Toggle warningReduceFlashing;
        [SerializeField] Button warningContinue;

        [Header("Options")]
        [SerializeField] Slider sensitivity;           // 0.3 .. 2
        [SerializeField] Dropdown subtitles;           // optional legacy field; the demo exposes English / Off buttons
        [SerializeField] Toggle reduceFlashing;
        [SerializeField] Toggle breathToggleMode;

        [SerializeField] AliController ali;

        bool paused, warningDone;
        float warningStartedAt;

        void Awake()
        {
            Load();
            if (warningScreen)
            {
                warningScreen.SetActive(true);
                warningStartedAt = Time.unscaledTime;
                Time.timeScale = 0f;
                AudioListener.pause = true;
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                if (warningReduceFlashing) warningReduceFlashing.isOn = Flashlight.ReduceFlashing;
                if (warningContinue) warningContinue.onClick.AddListener(CloseWarning);
            }
            else warningDone = true;

            if (sensitivity) sensitivity.onValueChanged.AddListener(v => { AliController.SensitivityScale = v; Save(); });
            if (subtitles) subtitles.onValueChanged.AddListener(i => { Subtitles.Language = i == 0 ? "en" : "off"; Save(); });
            if (reduceFlashing) reduceFlashing.onValueChanged.AddListener(b => { Flashlight.ReduceFlashing = b; Save(); });
            if (breathToggleMode) breathToggleMode.onValueChanged.AddListener(b => { BreathHold.ToggleMode = b; Save(); });
            if (pauseScreen) pauseScreen.SetActive(false);
            if (optionsScreen) optionsScreen.SetActive(false);
        }

        void CloseWarning()
        {
            if (warningDone) return;
            if (warningReduceFlashing) Flashlight.ReduceFlashing = warningReduceFlashing.isOn;
            Save();
            if (warningScreen) warningScreen.SetActive(false);
            warningDone = true;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!warningDone)
            {
                // Avoid trapping the player at the opening if a platform drops UI input.
                if (Time.unscaledTime - warningStartedAt >= 8f)
                {
                    CloseWarning();
                    return;
                }

                // Keep the opening usable even if the EventSystem misses a pointer or submit event.
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame && warningContinue)
                {
                    var buttonRect = warningContinue.transform as RectTransform;
                    if (buttonRect != null && RectTransformUtility.RectangleContainsScreenPoint(
                            buttonRect, mouse.position.ReadValue(), null))
                    {
                        CloseWarning();
                        return;
                    }
                }

                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                    CloseWarning();
                return;
            }
            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (optionsScreen && optionsScreen.activeSelf) CloseOptions();
                else SetPaused(!paused);
            }
        }

        public void SetPaused(bool p)
        {
            paused = p;
            if (pauseScreen) pauseScreen.SetActive(p);
            Time.timeScale = p ? 0f : 1f;
            AudioListener.pause = p;
            Cursor.lockState = p ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = p;
            if (ali) ali.enabled = !p;
            if (disableWhilePaused != null)
                foreach (var m in disableWhilePaused) if (m) m.enabled = !p;
        }

        public void Resume() => SetPaused(false);

        public void OpenOptions()
        {
            if (pauseScreen) pauseScreen.SetActive(false);
            if (optionsScreen) optionsScreen.SetActive(true);
            if (sensitivity) sensitivity.SetValueWithoutNotify(AliController.SensitivityScale);
            if (subtitles) subtitles.SetValueWithoutNotify(Subtitles.Language == "en" ? 0 : 1);
            if (reduceFlashing) reduceFlashing.SetIsOnWithoutNotify(Flashlight.ReduceFlashing);
            if (breathToggleMode) breathToggleMode.SetIsOnWithoutNotify(BreathHold.ToggleMode);
        }

        public void CloseOptions()
        {
            if (optionsScreen) optionsScreen.SetActive(false);
            if (pauseScreen) pauseScreen.SetActive(true);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void Load()
        {
            AliController.SensitivityScale = PlayerPrefs.GetFloat("es_sens", 1f);
            // Older demo preferences may contain "ar"; map those saves to English because this TMP UI is English-only.
            string savedSubtitleMode = PlayerPrefs.GetString("es_subs", "en");
            Subtitles.Language = savedSubtitleMode == "off" ? "off" : "en";
            Flashlight.ReduceFlashing = PlayerPrefs.GetInt("es_reduceFlash", 0) == 1;
            BreathHold.ToggleMode = PlayerPrefs.GetInt("es_breathToggle", 0) == 1;
        }

        static void Save()
        {
            PlayerPrefs.SetFloat("es_sens", AliController.SensitivityScale);
            PlayerPrefs.SetString("es_subs", Subtitles.Language);
            PlayerPrefs.SetInt("es_reduceFlash", Flashlight.ReduceFlashing ? 1 : 0);
            PlayerPrefs.SetInt("es_breathToggle", BreathHold.ToggleMode ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
