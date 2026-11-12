# البرومبت الرئيسي: محطة الصدى

> انسخ هذا النص كله، والصقه بأول رسالة لأي أداة ذكاء اصطناعي تشتغل وياها على اللعبة. وإذا تحتاج تفاصيل، ارفع وياه `docs/story-bible.md` (القصة والقرارات) و`docs/demo-design.md` (الديمو).

---

You are helping build **Echo Station (محطة الصدى)**, an original Iraqi first-person psychological horror game. Reply to me in Iraqi Arabic.

## The game
- **Setting:** the drained Mesopotamian Marshes, southern Iraq, January 1994. An abandoned pumping station on a drainage canal.
- **Platform:** PC (Steam + itch.io), offline, single-player, Arabic + English subtitles. **Unity 6 LTS + HDRP**, new Input System. Dev GPU: Quadro P5200 (no ray tracing). Three quality levels.
- **Team:** 3 people, **zero budget**. Only free assets: CC0 / MIT / Apache / OFL / CC-BY and free Unity packages. No GPL, no assets from commercial games. Log every asset in `docs/credits.md`.
- **Inspiration:** P.T. and Resident Evil 7, for **genre ideas only** (looping corridor, a radio that degrades, a figure that is never there, silence as a scare). No copied code, art, sound, names or levels.

## The story (short)
- **Ali**, 38, a Baghdad interrogator nicknamed "the Ear of Baghdad". He breaks suspects by recording them and playing their voices back. In 1989 he forced a confession from a young man, **Hussein Abd al-Hassan**, who was executed in 1990; the real killer was found in 1991 and the file was buried. Ali still listens to that tape on nights he can't sleep. Childhood trauma: his little brother **Murtadha** drowned in the Hilla river in 1966 while Ali, aged 10, covered his ears.
- **Operation Echo (1993):** the station's towers fire a sound pulse at dawn, always at **5:47**. A **"muffled"** echo means people hiding in the reeds; the location is radioed in. Marsh people call these the **"Nights of the Sound"**.
- **The killer is human:** **Khalaf**, a quiet, polite police radio operator and driver who stutters a little, makes tea for everyone and knocks **three times** on things. He is Hussein's brother. His victims, the operation's men, are found **drowned in dry places**, ears sealed with cassette tape of their own confessions, watches stopped at 5:47.
- **The Si'luwa and the Tantal** (Iraqi folklore monsters) are **Ali's trauma hallucinations**, not real. Rule: **a hallucination has no echo.**
- The ending is realistic. Secret 4 hints at Khalaf without revealing him.

## The 30-minute demo
- Night of 8 to 9 January 1994, from **4:52 to 5:47**. Ali wakes on the wet floor of a storeroom inside the station. The player knows nothing about the story and discovers everything through tapes, radio, notes and objects. No intro, no tutorial text.
- A **looping corridor** that changes every loop. A **living tape** on a bench plays, changes and distorts; the corridor only advances while it plays. Five puzzles (clock, knock rhythm, cassette flip, muffled search, radio channel). A human killer stalks the pump hall (hide, hold breath). Exit through a drain pipe, the dead marsh, the Tantal following, an empty police car. Three knocks on the window: Khalaf with tea. "وين چنت أستاذ؟ ساعة كاملة أدوّر عليك."

## Fixed rules
1. **Ali never speaks.** No music, no HUD, no camera cuts.
2. **Colours:** pale green only for sound and echo; red only for tea and Khalaf's things; neon is yellowish white.
3. **Sensitivity:** marsh people are never the source of horror. No political images or officials. The call to prayer is never distorted.
4. **Photosensitivity:** flicker never above 3 flashes per second; respect a "reduce flashing" option.
5. **Greybox first:** if it isn't scary in grey, art won't save it. But the greybox must already look like a **dark, wet, abandoned pumping station**, not a bright hallway.

## Controls
WASD move, mouse look, Ctrl crouch, Q flashlight, F echo pulse (pale green points show surfaces), E interact / tape play-stop, R rewind tape, Space hold breath.

## The project files
- Code: `Assets/_EchoStation/Scripts/` (namespace `EchoStation.*`), 38 C# files with the editor tools.
- Editor tools: **EchoStation > Generate Textures and Props**, then **EchoStation > Build Greybox Demo** (builds `Scenes/Demo_Greybox.unity`).
- Wiring guide: `docs/scene-setup.md`. Voice scripts: `docs/team/tape-and-voice-script.md` and `docs/team/radio-script.md`.
- The code was written without being tested inside Unity. Expect errors: fix them one by one from the Console.

## Known problems to fix first
- Several systems are not wired into the scene yet (wake-up sequence, generator power, killer, puzzles, subtitles, menus, car ending). See `docs/scene-setup.md` section 2.
- No real voices are recorded yet; all sounds are placeholders generated in code.
- After the last loop there is no path to the pump hall yet.
