# محطة الصدى: دليل Claude

لعبة رعب نفسي عراقية، بمنظور الشخص الأول، بالأهوار سنة 1994. **Unity 6 + HDRP** (أقوى جرافيكس، ويا 3 مستويات جودة)، للـ PC، **أوفلاين**، عربي وإنگليزي. فريق من 3، وصاحب اللعبة يبرمج ويا Claude Code. **ميزانية صفر.**

## اقرأ قبل أي شغل
1. `docs/story-bible.md`: قسم **"قرارات صاحب اللعبة"** بأوله يتقدّم على كلشي.
2. `docs/demo-design.md`: تصميم الديمو (30 دقيقة). كل الشغل الحالي على الديمو.

## قواعد ثابتة
- **علي ما يحچي أبداً.** ماكو موسيقى، وماكو واجهة (HUD)، والكاميرا ما تقطع.
- **القاتل إنسان.** السعلوة والطنطل هلوسات، والقاعدة: **الهلوسة ما إلها صدى** (`EchoSurface.IsHallucination`).
- **الألوان:** الأخضر الشاحب للصوت والصدى **بس**، والأحمر للچاي وأشياء خلف **بس**، والنيون أبيض مصفر.
- **الأولوية: نخلّص الديمو.** نبني بالمادة الرمادية أول، والجماليات بعدين. إذا ما خوّف وهو رمادي، ما راح يخوّف بالفن.
- **مجاني بس:** CC0 / MIT / Apache / OFL / CC-BY، وحزم Unity المجانية. **وكل أصل ينكتب بـ `docs/credits.md`.** ولا GPL، ولا أصول ألعاب تجارية.
- **الحساسية:** أهل الأهوار ما يكونون مصدر الرعب أبداً، ولا صور سياسية، والأذان ما يتشوّه.
- **حساسية الضوء:** الرمش ما يتعدى 3 ومضات بالثانية، ويحترم `Flashlight.ReduceFlashing`.

## الجرافيكس (القرار 33)
- **HDRP** على Unity 6. جهاز التطوير: Quadro P5200 (بدون Ray Tracing، فلا نعتمد عليه).
- **3 مستويات جودة** (Low / Medium / High): الضباب الحجمي، والظلال، و SSR تنزل بالمستويات الواطية.
- أدوات HDRP المهمة: **Volumetric Fog** (Local Volumetric Fog بكل غرفة)، و**Physically Based Sky** للهور، و**Decals** للطين والصدأ، و**Water System** لماي الهور والقاعة، و**Exposure** ثابت (الظلام لازم يبقى ظلام).

## الكود
- كل الكود بـ `Assets/_EchoStation/Scripts/`، بـ namespace `EchoStation.*`.
- **Input System الجديد** (`UnityEngine.InputSystem`).
- السكربتات الموجودة: `AliController`، `Flashlight`، `Interactor`، `EchoPulse`، `EchoSurface`، `LoopCorridor`، `TurnBackTrigger`، `AnomalyDirector`، `LivingTape`، `OldMediaFilter` (راديو/لاسلكي/شريط)، `GeneratorPower` (المولّد = الأمان، والانقطاع)، `WakeSequence` (أول 60 ثانية)، `KillerController` (لقاءات مبرمجة، يسمع، ونَفَسه يختفي لما يشك)، `HidingSpot` و`BreathHold` (Space)، `CaughtSequence` (المسكة والرجوع)، `CarEnding` (السيارة وثلاث دگات)، `Subtitles` (ترجمة عربي/إنگليزي، TMP + RTLTMPro)، `LineTable` (كل الجمل برموزها من `Data/lines.json`)، `AmbienceDirector` (طبقة الخوف، وأصوات مكان ثاني، والهمس، والضفادع)، `DemoClock` (ميزانية الـ30 دقيقة)، `GameMenus` (التحذير، والإيقاف، والإعدادات)، `SiluwaReflection` (بالانعكاس بس، وتختفي إذا التفت)، `TantalStalker` (يقرب بالرمش، ويسرق الأصوات)، والألغاز بـ `Puzzles/` (كلها ترث `PuzzleBase`، واللغز الإجباري يعلّگ الممر لحد ما ينحل)، و`SecretTracker` (الأسرار الأربعة). وأي شي ينستعمل بـ E يطبّق `IInteractable`. **أداة المحرر:** `Editor/DemoGreyboxBuilder.cs`: من القائمة **EchoStation > Build Greybox Demo** تنبني خريطة الديمو كلها رمادية، ويا علي والمشاهد والطبقات والضباب، فتضغط Play وتجرّب. وأداة `tools/oldmedia.sh` تحوّل أي تسجيل لصوت راديو أو شريط. لازم Layers باسم `Player` و`Hallucination` (والثاني مستثنى من `EchoPulse.echoLayers`)، ومادة للنبضة (HDRP/Unlit، Transparent، Additive). انكتبت بدون ما تنجرّب داخل Unity، فأول شي: افتح المشروع، صلّح أي خطأ يطلع بالـ Console، وجرّب.

## الأسبوع الأول (من خطة البناء)
1. **ممر رمادي** بـ ProBuilder: 14 متر طول، و1.8 متر عرض، وزاوية عمياء 90 درجة، وباب A وباب B.
2. **علي:** Capsule + CharacterController + كاميرا + `AliController` + مصباح (Spot Light) عليه `Flashlight`.
3. **النبضة:** `EchoPulse` على الكاميرا. حط `EchoSurface` على الحيطان والأرض بأصوات مختلفة.
4. **الدورة:** Trigger بالزاوية العمياء عليه `LoopCorridor`، ويا ExitAnchor و EntryAnchor.
5. **الشريط:** مسجّل على المصطبة عليه `LivingTape`، ويا تسجيل تجريبي.
6. **الهدف:** تمشي بالممر بالظلام، وتنبض، وتسمع، وتلف 5 دورات بدون ما تحس بالنقلة.
