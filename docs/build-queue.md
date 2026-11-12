# طابور بناء كود الديمو

> **المشروع HDRP** (القرار 33): المواد HDRP/Lit، والضباب Local Volumetric Fog، والإعدادات بـ Volume. ولكل مهمة جرافيكس، 3 مستويات جودة.

**ترتيب الأولوية (حتى نوصل لنسخة رمادية تنلعب من أولها لآخرها بأسرع وقت):** 1 ← 9 ← 2 ← 3 ← 4 ← 5 ← 12 ← 8 ← 14 ← 6 ← 7 ← 10 ← 11 ← 18 ← 13 ← 16 ← 17 ← 15. ومهمة 1 لازم تبني **خريطة الديمو كلها رمادية** (المخزن، والممر، والورشة، وقاعة المضخة، والأنبوب، والدرب، والسيارة)، مو بس الممر.

كل جلسة تاخذ **أول مهمة ما معلّم عليها حسب ترتيب الأولوية**، تكتب كودها بـ `Assets/_EchoStation/Scripts/`، وتعلّم عليها `[x]`، وتسوي commit و push. **بدون وكلاء فرعيين** حتى نوفّر.
المرجع: `docs/demo-design.md`، و`CLAUDE.md`. وكل سكربت جديد ينضاف لقائمة السكربتات بـ `CLAUDE.md`.

- [x] 1. **(HDRP) أداة بناء الممر الرمادي** → `Editor/DemoGreyboxBuilder.cs` (قائمة EchoStation > Build Greybox Demo) (Editor script): قائمة `EchoStation > Build Greybox Corridor` تبني الممر 14×1.8 م، والزاوية العمياء، والبابين، والمخزن، والـ Anchors، والـ Triggers، وعلي (CharacterController + كاميرا + مصباح + Pulse + Interactor)، والمسجّل، والـ Layers. حتى صاحب اللعبة يجرّب بكبسة وحدة.
- [x] 2. **المولّد** → `World/GeneratorPower.cs`. **المولّد** (`GeneratorPower`): همهمة الأمان، والنيون مربوط بيه، وحدث الانقطاع (3 ثواني صمت) والرجوع، و`AudioMixer` snapshots.
- [x] 3. → `Killer/KillerController.cs`. **القاتل** (`KillerController`): لقاءات مبرمجة على NavMesh، يسمع النبضة والخطوات (`EchoPulse.OnPulse`)، وحالات: يتمشى / يشك (نَفَسه يختفي) / يدوّر / يمسك. ما يركض ولا يصيّح.
- [x] 4. → `Player/HidingSpot.cs`, `Player/BreathHold.cs` (Space). **التخبّي وحبس النفس** (`HidingSpot`, `BreathHold`): تحت الميز والمشبّك، وضغط مستمر (أو كبسة من الإعدادات)، والشاشة تعتم، ودگات القلب.
- [x] 5. → `Killer/CaughtSequence.cs`. **مشهد المسكة** (`CaughtSequence`): 2-3 ثواني، شريط بالأذن، كتم الصوت، سواد، والرجوع لأول الغرفة.
- [x] 6. → `Haunt/SiluwaReflection.cs`. **السعلوة** (`SiluwaReflection`): تبين بالزجاج والانعكاس بس، وتختفي إذا اللاعب باوع مباشرة، وقطرة بلا ماي، وما تتحرك قدّام العين.
- [x] 7. → `Haunt/TantalStalker.cs`. **الطنطل** (`TantalStalker`): يتحرك بس بإطارات الرمش أو لما ما ينشاف، وأقرب كل مرة، وأصوات المكان تختفي وحدة وحدة.
- [x] 8. → `World/DemoClock.cs`. **مدير وقت الديمو** (`DemoClock`): ميزانية الـ30 دقيقة، والأحداث المضمونة بالوقت، وسقف 60 ثانية للدورة، وشيل الدورات الفارغة إذا تأخر.
- [x] 9. **مشهد الصحوة** → `World/WakeSequence.cs` (يحتاج Canvas بصورة سودة كاملة للجفون؛ وأداة البناء تربطه بالمهمة الجاية). **مشهد الصحوة** (`WakeSequence`): أول 60 ثانية بالضبط حسب `demo-design.md` (الصوت قبل الصورة، فتح الجفون، الدگات بالثانية 45، أول نبضة بالثانية 55).
- [x] 10. → `Puzzles/` (`PuzzleBase` ويا سلّم المساعدة، و`ClockPuzzle`، و`KnockRhythmPuzzle`، و`CassetteFlipPuzzle`، و`MuffledSearchPuzzle`، و`RadioChannelPuzzle`). **الألغاز الخمسة** (`ClockPuzzle`, `KnockRhythmPuzzle`, `CassetteFlipPuzzle`, `MuffledSearchPuzzle`, `RadioChannelPuzzle`) ويا سلّم المساعدة بلا واجهة.
- [x] 11. → `Puzzles/SecretTracker.cs`. **الأسرار الأربعة** (`SecretTracker`) والكاسيت الخامس وتأثيره على جملة خلف.
- [x] 12. → `World/CarEnding.cs` (+ `World/Subtitles.cs` نسخة مبدئية). **النهاية** (`CarEnding`): السيارة، والقفل، والطنطل، والأذان، وثلاث دگات، وخلف (نص وجه)، والاستكان، والسواد، والاسم، وبطاقة قائمة الأمنيات.
- [x] 13. → `World/Subtitles.cs` (TMP، و RTLTMPro للعربي)، و`World/LineTable.cs`، و`Data/lines.json` (مستخرج من سيناريو سجاد). **الترجمة** (`Subtitles`, `Localization`): عربي وإنگليزي، صغيرة وبلا اسم المتكلم، ودعم RTL (RTLTMPro، رخصته MIT، يتأكد منها).
- [x] 14. → `UI/GameMenus.cs` (الشاشات تنبني بالمشهد وتنربط). **القوائم** (`WarningScreen`, `PauseMenu`, `OptionsMenu`): تحذير حساسية الضوء، وتقليل الوميض، وحساسية الماوس، والترجمة، وحبس النفس بكبسة.
- [ ] 16. **خامات مصنوعة بالكود** (Editor script `ProceduralTextures`): كونكريت مبلول، وصبغ حكومي بيج مقشّر، وحديد مصدّي، وطين مفطّر، وماي، وقصب. PNG بـ `Assets/_EchoStation/Art/Generated/` ومواد HDRP/Lit جاهزة.
- [ ] 17. **موديلات بسيطة بالكود** (Editor script `ProceduralProps`): حزم قصب، ومسجّل كاسيت، واستكان چاي، ومصطبة، وساعة حايط، وتقويم، ولوحة سيطرة، وبرميل، وفانوس، ومشبّك حديد. و"إنسان رمادي" للقاتل وخلف (low-poly) حتى يتبدل بموديل MakeHuman/Mixamo.
- [x] 18. → `Audio/AmbienceDirector.cs`. **مدير الأجواء الصوتية** (`AmbienceDirector`) حسب `docs/team/sound-study.md`: طبقة الخوف الواطية (−30 → −18 dB قبل الأحداث)، و Reverb Zones لكل غرفة، والهمس بأذن وحدة، وأصوات "مكان ثاني" عشوائية، وصنع الأصوات الناقصة بالكود.
- [ ] 15. **دليل تركيب المشهد** (`docs/scene-setup.md`): خطوات بالعربي لربط كل السكربتات بالمشهد وتجربة الديمو من أوله لآخره.
