# تركيب مشهد الديمو: خطوة بخطوة

> هذا الدليل لصاحب اللعبة، ولـ Claude Code على اللابتوب. **الكود كله انكتب بدون ما ينجرّب داخل Unity**، فأكيد تطلع أخطاء بالأول. صلّحها خطوة خطوة.
> **نصيحة:** دز هذا الملف لـ Claude Code باللابتوب، وگلّه: *"طبّق docs/scene-setup.md خطوة خطوة، وصلّح أي خطأ بالـ Console"*.

---

## 0. قبل كلشي
1. المشروع **HDRP** على **Unity 6 LTS** (شوف `README.md`).
2. من **Package Manager**: ProBuilder، و Cinemachine، و Input System، و **AI Navigation**، و TextMeshPro (يجي ويا uGUI).
3. **Edit ← Project Settings ← Player ← Active Input Handling ← Input System Package (New)** (أو Both).
4. **افتح الـ Console**، ولازم يكون **فارغ من الأخطاء الحمرة** قبل ما تكمل. إذا طلع خطأ، صلّحه أول.

## 1. ابنِ الخريطة بكبسة
1. **EchoStation ← Generate Textures and Props**: يصنع الخامات والموديلات المبدئية (`Art/Generated`).
2. **EchoStation ← Build Greybox Demo**: يبني المشهد `Scenes/Demo_Greybox.unity`، وبيه الخريطة كلها، وعلي، والشريط، والدورة، والضباب، والطبقات.
3. اضغط **Play**: لازم تمشي (WASD)، وتباوع (الماوس)، وتشغّل المصباح (Q)، وتنبض (F)، وتشغّل الشريط وتوقفه (E)، وترجّعه (R).

✅ **اختبار 1:** تمشي بالممر، وتعبر الزاوية العمياء، فترجع لأول الممر **بدون ما تحس بالنقلة**. إذا حسّيت، عدّل `ExitAnchor_DoorB` و `EntryAnchor_DoorA` حتى يكونون بنفس المكان النسبي بالضبط.

## 2. ربط الأنظمة (مرة وحدة)
أداة البناء تحط الأساس. هاي اللي تنضاف يدوياً (أو خلّي Claude Code باللابتوب يضيفها):

| النظام | وين | شنو تربط |
|---|---|---|
| `GeneratorPower` | على `Generator_Hum` | `hum` = الـ AudioSource، و `dieClip` = `generator_die`، و`neonLights` = أضوية `Neon_*` |
| `AmbienceDirector` | GameObject جديد | `dread` = `Dread_Drone`، و`elsewhereClips` = `metal_groan`، و`distant_splash_thud`، و`knock_pipe_x3`. و`elsewherePoints` = نقاط ورا الحيطان |
| `WakeSequence` | GameObject جديد بالمخزن | علي، والكاميرا، والمصباح، والنبضة، و`Bulb_Storeroom`، والمولّد، والأصوات (`drip`، و`bulb_crackle`، و`tape_hiss_loop`...)، و**Canvas** بصورة سودة كاملة للجفون |
| `BreathHold` | على علي | Canvas بصورة سودة للتعتيم، و`heartbeat` loop، و`AudioLowPassFilter` على الكاميرا |
| `HidingSpot` | Box Trigger تحت ميز غرفة السيطرة، وتحت المشبّك | علّم `underwater` على اللي تحت المشبّك |
| `KillerController` | على `KILLER_Placeholder` (بدّله بـ `GreyHuman_175`) | **NavMeshAgent** + **Bake NavMesh** (AI Navigation ← NavMesh Surface على `WORLD`)، ومسار نقاط بقاعة المضخة، و`breath`، و`steps`، و`boot_water_step`. وحطّله `EchoSurface` = Body |
| `CaughtSequence` | GameObject جديد | القاتل، وعلي، والكاميرا، وصورة سودة، و`checkpoint` بأول كل غرفة |
| `SiluwaReflection` | على باب غرفة السيطرة | `glass` = زجاج الباب، و`reflectedFigure` = `GreyHuman_175` أطول بشبرين (طبقة **Hallucination**)، و`drip` |
| `TantalStalker` | مكعب أسود طويل (طبقة **Hallucination**) | الكاميرا، والهدف (علي أو السيارة)، والأصوات اللي يسرقها (الريح، والطين، ونَفَس علي) |
| `DemoClock` | GameObject جديد | الممر، والـ Director، والـ Beats (أحداث بالوقت) |
| `Subtitles` + `LineTable` | Canvas تحت الشاشة بـ TMP Text (أو RTLTextMeshPro للعربي) | `LineTable.json` = `Data/lines.json` |
| `GameMenus` | Canvas للتحذير، والإيقاف، والإعدادات | الأزرار والـ Toggles |
| `CarEnding` | Box Trigger يم باب `PoliceCar_Brazili` | كاميرا المقعد، و`khalaf` (نص وجه)، والاستكان، والأصوات، وكروت النهاية |
| الألغاز | شوف الجدول تحت | |

### الألغاز
| اللغز | على شنو | ملاحظة |
|---|---|---|
| `ClockPuzzle` | `WallClock_0547` | `minuteHand` و`hourHand` من الـ Prefab |
| `KnockRhythmPuzzle` | باب B | `doorLock` = شي يسد الباب لحد ما ينحل |
| `CassetteFlipPuzzle` | المسجّل الكبير | `cassetteOut` = كاسيت على المصطبة |
| `MuffledSearchPuzzle` | لافتة "ممنوع التدخين" (EchoSurface = Body) | `hiddenSpeaker` ورا اللافتة |
| `RadioChannelPuzzle` | `ControlPanel` بغرفة السيطرة | اختياري |
| `SecretTracker` | GameObject جديد | `fifthCassette` بالورشة |

**كل لغز إجباري ينبدي بـ `Begin()`** لما توصل دورته. اربطها من `AnomalyDirector` أو `DemoClock` (UnityEvent).

## 3. الطبقات (Layers)
- **Player** (8): علي.
- **Hallucination** (9): السعلوة، والطنطل، والغرقى. **ومستثناة** من `EchoPulse.echoLayers` (أداة البناء تضبطها).

## 4. اختبارات الأسبوع الأول (من خطة البناء)
1. ✅ تمشي بالظلام، وتنبض، وتسمع صدى مختلف من الحديد والكونكريت والقصب.
2. ✅ تلف 5 دورات بدون ما تحس بالنقلة.
3. ✅ توقف الشريط، فالدورة ما تتقدم.
4. ✅ تحط شي على طبقة Hallucination وتنبض: **ما يرجّع ولا نقطة**.
5. ✅ الديمو من الصحوة للسيارة **ينلعب من أوله لآخره**، حتى لو رمادي.

## 5. إذا شي ما اشتغل
- **ماكو تحكم:** Active Input Handling مو على Input System.
- **النقاط الخضرة ما تبين:** `EchoPulse.pointMaterial` فارغ، أو مادة HDRP/Unlit مو Transparent.
- **الممر ما يتقدّم:** الشريط واگف، أو لغز إجباري بعده ما انحل (`PuzzleBase.AnyBlocking`).
- **القاتل ما يتحرك:** ماكو NavMesh مخبوز.
- **أي خطأ ثاني:** صوّر الـ Console، ودزه لـ Claude.
