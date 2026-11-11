#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using EchoStation.Audio;
using EchoStation.Echo;
using EchoStation.Loop;
using EchoStation.Player;

namespace EchoStation.EditorTools
{
    /// <summary>
    /// Menu: EchoStation > Build Greybox Demo.
    /// Builds the WHOLE demo map in grey boxes (no ProBuilder needed) so the demo can be walked end to end:
    /// storeroom (wake-up) > vestibule A > corridor (14 m) > blind corner > vestibule B (loop trigger),
    /// workshop, control room, pump hall with grate, drain pipe, levee with reed rows, broken car, police car.
    /// Also: layers, Ali rig with all scripts wired, living tape, generator hum, HDRP volume (fog + fixed exposure).
    /// Re-running deletes the old scene. Every number here is a starting point for tuning.
    /// </summary>
    public static class DemoGreyboxBuilder
    {
        const string Root = "Assets/_EchoStation";
        const string ScenePath = Root + "/Scenes/Demo_Greybox.unity";
        const string GenAudio = Root + "/Audio/Generated/";
        const string MatDir = Root + "/Art/Greybox";

        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        static Transform world;

        [MenuItem("EchoStation/Build Greybox Demo", priority = 0)]
        public static void Build()
        {
            if (!EditorUtility.DisplayDialog("Echo Station", "Build a fresh greybox demo scene? (Demo_Greybox.unity will be replaced)", "Build", "Cancel")) return;

            EnsureLayer(8, "Player");
            EnsureLayer(9, "Hallucination");
            EnsureFolder(Root + "/Scenes"); EnsureFolder(MatDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            world = new GameObject("WORLD").transform;

            BuildVolume();
            var storeroom = Room("01_Storeroom", new Vector3(0, 0, -2.5f), new Vector3(4, 2.6f, 5), "concrete", EchoMaterial.Concrete, openZPlus: true);
            Box("VestibuleA_Floor", new Vector3(0, -0.05f, 0.5f), new Vector3(1.8f, 0.1f, 1), "concrete", EchoMaterial.Concrete, storeroom.parent);
            BuildCorridor(out Transform exitAnchor, out Transform entryAnchor, out BoxCollider loopTrigger);
            Room("04_Workshop", new Vector3(-2.9f, 0, 6.5f), new Vector3(4, 2.6f, 3), "concrete", EchoMaterial.Concrete, openXPlus: true);
            Room("05_ControlRoom", new Vector3(3.5f, 0, 16.4f), new Vector3(3, 2.6f, 3), "concrete", EchoMaterial.Concrete, openZMinus: true);
            BuildPumpHall();
            BuildPipeAndOutside();

            // Props on the bench: the living tape
            var bench = Box("Bench", new Vector3(0.65f, 0.25f, 5f), new Vector3(0.4f, 0.5f, 1.6f), "wood", EchoMaterial.Concrete, world);
            var recorder = Box("TapeRecorder", new Vector3(0.65f, 0.6f, 5f), new Vector3(0.3f, 0.2f, 0.45f), "dark", EchoMaterial.Tape, world);
            var tape = SetupTape(recorder.gameObject);
            Box("TeaGlass_RED", new Vector3(0.65f, 0.58f, 5.4f), new Vector3(0.06f, 0.1f, 0.06f), "tea", EchoMaterial.Glass, world);
            Box("WallClock_0547", new Vector3(-0.88f, 1.9f, 9f), new Vector3(0.05f, 0.4f, 0.4f), "paper", EchoMaterial.Glass, world);
            Box("Calendar_Jan1994", new Vector3(-0.88f, 1.5f, 3f), new Vector3(0.02f, 0.5f, 0.35f), "paper", EchoMaterial.Concrete, world);

            // Loop logic
            var directorGo = new GameObject("AnomalyDirector"); directorGo.transform.SetParent(world);
            var director = directorGo.AddComponent<AnomalyDirector>();
            var so = new SerializedObject(director);
            var seq = so.FindProperty("sequence");
            string[] ids = { "00","01","02","03","04","05","06","06b","07","08","09","empty","10","11","12","13","14","15","spare" };
            seq.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++) seq.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue = ids[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            var corridor = loopTrigger.gameObject.AddComponent<LoopCorridor>();
            Set(corridor, "exitAnchor", exitAnchor); Set(corridor, "entryAnchor", entryAnchor);
            Set(corridor, "director", director); Set(corridor, "tape", tape);
            var turnBack = GameObject.Find("TurnBackTrigger_DoorA").AddComponent<TurnBackTrigger>();
            Set(turnBack, "corridor", corridor);

            BuildGenerator();
            BuildLights();
            BuildAli(new Vector3(0.6f, 0.05f, -3.2f));

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[EchoStation] Greybox demo built: " + ScenePath + ". Press Play. WASD, mouse, Q flashlight, F echo pulse, E/R tape, Ctrl crouch.");
        }

        // ---------- geometry ----------

        static void BuildCorridor(out Transform exitAnchor, out Transform entryAnchor, out BoxCollider loopTrigger)
        {
            var c = new GameObject("02_Corridor").transform; c.SetParent(world);
            const float w = 1.8f, h = 2.5f;
            // segment 1: z 1..15 along +Z (14 m)
            Box("Floor1", new Vector3(0, -0.05f, 8), new Vector3(w, 0.1f, 14), "concrete", EchoMaterial.Concrete, c);
            Box("Ceil1", new Vector3(0, h, 8), new Vector3(w, 0.1f, 14), "concrete", EchoMaterial.Concrete, c);
            Box("WallL1a", new Vector3(-w / 2 - 0.05f, h / 2, 3), new Vector3(0.1f, h, 4), "paint", EchoMaterial.Concrete, c);      // gap at z 5..8 = workshop door
            Box("WallL1b", new Vector3(-w / 2 - 0.05f, h / 2, 11.5f), new Vector3(0.1f, h, 7), "paint", EchoMaterial.Concrete, c);
            Box("WallR1", new Vector3(w / 2 + 0.05f, h / 2, 7.1f), new Vector3(0.1f, h, 12.2f), "paint", EchoMaterial.Concrete, c);
            Box("WallEnd1", new Vector3(0, h / 2, 15.05f), new Vector3(w + 0.2f, h, 0.1f), "paint", EchoMaterial.Concrete, c);
            // segment 2 (after the blind corner): x 0.9..8 along +X at z 14.1
            Box("Floor2", new Vector3(4.45f, -0.05f, 14.1f), new Vector3(7.1f, 0.1f, w), "concrete", EchoMaterial.Concrete, c);
            Box("Ceil2", new Vector3(4.45f, h, 14.1f), new Vector3(7.1f, 0.1f, w), "concrete", EchoMaterial.Concrete, c);
            Box("WallFar2a", new Vector3(2.35f, h / 2, 15.05f), new Vector3(2.9f, h, 0.1f), "paint", EchoMaterial.Concrete, c); // gap = control room door
            Box("WallFar2b", new Vector3(6.4f, h / 2, 15.05f), new Vector3(3.2f, h, 0.1f), "paint", EchoMaterial.Concrete, c);
            Box("WallNear2", new Vector3(4.95f, h / 2, 13.15f), new Vector3(8.1f, h, 0.1f), "paint", EchoMaterial.Concrete, c);
            Box("DoorB", new Vector3(8.05f, 1.05f, 14.1f), new Vector3(0.1f, 2.1f, 1.1f), "rust", EchoMaterial.Metal, c);

            // Anchors: the player passing door B's threshold appears at door A's threshold, same relative pose.
            var exitGo = new GameObject("ExitAnchor_DoorB"); exitGo.transform.SetParent(c);
            exitGo.transform.SetPositionAndRotation(new Vector3(7.6f, 0, 14.1f), Quaternion.Euler(0, 90, 0));
            var entryGo = new GameObject("EntryAnchor_DoorA"); entryGo.transform.SetParent(c);
            entryGo.transform.SetPositionAndRotation(new Vector3(0, 0, 0.6f), Quaternion.Euler(0, 0, 0));
            exitAnchor = exitGo.transform; entryAnchor = entryGo.transform;

            var trig = new GameObject("LoopTrigger_BeforeDoorB"); trig.transform.SetParent(c);
            trig.transform.position = new Vector3(7.6f, 1.2f, 14.1f);
            loopTrigger = trig.AddComponent<BoxCollider>(); loopTrigger.isTrigger = true; loopTrigger.size = new Vector3(0.4f, 2.4f, w);

            var back = new GameObject("TurnBackTrigger_DoorA"); back.transform.SetParent(c);
            back.transform.position = new Vector3(0, 1.2f, 0.9f);
            var bb = back.AddComponent<BoxCollider>(); bb.isTrigger = true; bb.size = new Vector3(w, 2.4f, 0.3f);
        }

        static void BuildPumpHall()
        {
            var p = Room("06_PumpHall", new Vector3(14, -1, 14.1f), new Vector3(10, 4.5f, 10), "concrete", EchoMaterial.Concrete, openXMinus: true);
            Box("Water_KneeDeep", new Vector3(14, -0.55f, 14.1f), new Vector3(9.8f, 0.9f, 9.8f), "water", EchoMaterial.Water, p).GetComponent<Collider>().isTrigger = true;
            for (int i = 0; i < 9; i++)   // walkway grate bars, ~0.45 m above water
                Box("GrateBar" + i, new Vector3(9.4f + i * 0.6f, 0.0f, 14.1f), new Vector3(0.05f, 0.05f, 1.6f), "rust", EchoMaterial.Metal, p);
            Box("GrateFrameL", new Vector3(11.8f, 0.0f, 13.3f), new Vector3(5, 0.06f, 0.08f), "rust", EchoMaterial.Metal, p);
            Box("GrateFrameR", new Vector3(11.8f, 0.0f, 14.9f), new Vector3(5, 0.06f, 0.08f), "rust", EchoMaterial.Metal, p);
            Box("GiantPump", new Vector3(16.5f, 1.0f, 16.5f), new Vector3(2.5f, 3, 2.5f), "rust", EchoMaterial.Metal, p);
            var killer = Box("KILLER_Placeholder", new Vector3(17, 0.9f, 12), new Vector3(0.5f, 1.8f, 0.35f), "dark", EchoMaterial.Body, p);
            killer.gameObject.SetActive(false);   // task 3 (KillerController) brings him to life
        }

        static void BuildPipeAndOutside()
        {
            var o = new GameObject("07_Pipe_Levee_Car").transform; o.SetParent(world);
            // drain pipe: 20 m crawl, 0.9 m high
            Box("PipeFloor", new Vector3(29, -1.05f, 14.1f), new Vector3(20, 0.1f, 1.0f), "rust", EchoMaterial.Metal, o);
            Box("PipeTop", new Vector3(29, -0.1f, 14.1f), new Vector3(20, 0.1f, 1.0f), "rust", EchoMaterial.Metal, o);
            Box("PipeL", new Vector3(29, -0.55f, 13.55f), new Vector3(20, 0.9f, 0.1f), "rust", EchoMaterial.Metal, o);
            Box("PipeR", new Vector3(29, -0.55f, 14.65f), new Vector3(20, 0.9f, 0.1f), "rust", EchoMaterial.Metal, o);
            // dry cracked canal + levee
            Box("CanalBed", new Vector3(60, -1.2f, 14.1f), new Vector3(42, 0.2f, 30), "mud", EchoMaterial.Concrete, o);
            var rng = new System.Random(1994);
            for (int row = 0; row < 4; row++)
                for (int i = 0; i < 16; i++)
                {
                    float x = 44 + i * 2.2f + (float)rng.NextDouble(), z = 4 + row * 2.5f + (float)rng.NextDouble();
                    var r = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    r.name = "ReedBundle"; r.transform.SetParent(o);
                    r.transform.position = new Vector3(x, -0.25f, z);
                    r.transform.localScale = new Vector3(0.45f, 0.85f, 0.45f);
                    Paint(r, "reed"); r.AddComponent<EchoSurface>(); SetSurface(r, EchoMaterial.Reed);
                }
            Box("Sign_UmmAlShuwaich", new Vector3(70, 0.3f, 22), new Vector3(1.4f, 0.6f, 0.06f), "paper", EchoMaterial.Metal, o);
            var broken = Box("BrokenCar_Hazards", new Vector3(76, -0.4f, 20), new Vector3(1.8f, 1.3f, 4.2f), "dark", EchoMaterial.Metal, o);
            foreach (float dx in new[] { -0.75f, 0.75f }) HazardLight(broken, new Vector3(dx, 0, -2.15f));
            Box("PoliceCar_Brazili", new Vector3(80, -0.4f, 16), new Vector3(1.8f, 1.4f, 4.4f), "dark", EchoMaterial.Metal, o);
        }

        static void HazardLight(Transform parent, Vector3 local)
        {
            var go = new GameObject("Hazard"); go.transform.SetParent(parent, false); go.transform.localPosition = local;
            var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.55f, 0.15f); l.range = 8; l.intensity = 200;
            if (!go.GetComponent<HDAdditionalLightData>()) go.AddComponent<HDAdditionalLightData>();
        }

        static Transform Room(string name, Vector3 floorCenter, Vector3 size, string mat, EchoMaterial em,
                              bool openZPlus = false, bool openZMinus = false, bool openXPlus = false, bool openXMinus = false)
        {
            var r = new GameObject(name).transform; r.SetParent(world);
            float t = 0.1f;
            Box("Floor", floorCenter + Vector3.down * 0.05f, new Vector3(size.x, t, size.z), mat, em, r);
            Box("Ceiling", floorCenter + Vector3.up * size.y, new Vector3(size.x, t, size.z), mat, em, r);
            void Wall(string n, Vector3 off, Vector3 s, bool open)
            {
                if (!open) { Box(n, floorCenter + off, s, "paint", em, r); return; }
                // wall with a 1.1 m doorway in the middle
                bool alongX = s.x > s.z; float len = alongX ? s.x : s.z, side = (len - 1.1f) / 2f;
                Vector3 axis = alongX ? Vector3.right : Vector3.forward;
                Vector3 ss = alongX ? new Vector3(side, s.y, s.z) : new Vector3(s.x, s.y, side);
                Box(n + "_a", floorCenter + off - axis * (0.55f + side / 2), ss, "paint", em, r);
                Box(n + "_b", floorCenter + off + axis * (0.55f + side / 2), ss, "paint", em, r);
                Box(n + "_lintel", floorCenter + off + Vector3.up * (2.15f - s.y / 2 + (s.y - 2.1f) / 2),
                    alongX ? new Vector3(1.1f, s.y - 2.1f, s.z) : new Vector3(s.x, s.y - 2.1f, 1.1f), "paint", em, r);
            }
            float hy = size.y / 2;
            Wall("WallZ+", new Vector3(0, hy, size.z / 2), new Vector3(size.x, size.y, t), openZPlus);
            Wall("WallZ-", new Vector3(0, hy, -size.z / 2), new Vector3(size.x, size.y, t), openZMinus);
            Wall("WallX+", new Vector3(size.x / 2, hy, 0), new Vector3(t, size.y, size.z), openXPlus);
            Wall("WallX-", new Vector3(-size.x / 2, hy, 0), new Vector3(t, size.y, size.z), openXMinus);
            return r;
        }

        static Transform Box(string name, Vector3 center, Vector3 size, string mat, EchoMaterial em, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent);
            go.transform.position = center; go.transform.localScale = size;
            Paint(go, mat);
            go.AddComponent<EchoSurface>(); SetSurface(go, em);
            return go.transform;
        }

        static void SetSurface(GameObject go, EchoMaterial em)
        {
            var s = go.GetComponent<EchoSurface>();
            string clip = em switch
            {
                EchoMaterial.Metal => "echo_metal", EchoMaterial.Reed => "echo_reed", EchoMaterial.Water => "echo_water",
                EchoMaterial.Body => "echo_body", _ => "echo_concrete"
            };
            Set(s, "material", (int)em);
            Set(s, "clip", AssetDatabase.LoadAssetAtPath<AudioClip>(GenAudio + clip + ".ogg"));
        }

        static void Paint(GameObject go, string key)
        {
            if (!mats.TryGetValue(key, out var m) || m == null)
            {
                string path = $"{MatDir}/GB_{key}.mat";
                m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(Shader.Find("HDRP/Lit"));
                    Color c = key switch
                    {
                        "concrete" => new Color(0.32f, 0.33f, 0.31f), "paint" => new Color(0.55f, 0.50f, 0.40f),   // faded government beige
                        "rust" => new Color(0.30f, 0.20f, 0.14f), "dark" => new Color(0.08f, 0.08f, 0.08f),
                        "wood" => new Color(0.25f, 0.18f, 0.12f), "tea" => new Color(0.60f, 0.18f, 0.07f),        // the only saturated red
                        "paper" => new Color(0.75f, 0.72f, 0.62f), "water" => new Color(0.05f, 0.09f, 0.08f),
                        "mud" => new Color(0.22f, 0.19f, 0.15f), "reed" => new Color(0.36f, 0.34f, 0.24f),
                        _ => Color.gray
                    };
                    m.SetColor("_BaseColor", c);
                    m.SetFloat("_Smoothness", key == "water" ? 0.95f : key == "rust" ? 0.25f : 0.15f);
                    AssetDatabase.CreateAsset(m, path);
                }
                mats[key] = m;
            }
            go.GetComponent<Renderer>().sharedMaterial = m;
        }

        // ---------- systems ----------

        static LivingTape SetupTape(GameObject recorder)
        {
            var src = recorder.AddComponent<AudioSource>();
            src.spatialBlend = 1; src.maxDistance = 12; src.rolloffMode = AudioRolloffMode.Linear;
            var tape = recorder.AddComponent<LivingTape>();
            var so = new SerializedObject(tape);
            var entries = so.FindProperty("entries");
            entries.arraySize = 1;
            var e = entries.GetArrayElementAtIndex(0);
            e.FindPropertyRelative("step").intValue = 0;
            e.FindPropertyRelative("normal").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(GenAudio + "tape_hiss_loop.ogg");
            so.FindProperty("stopClick").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(GenAudio + "cassette_stop.ogg");
            so.FindProperty("playClick").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(GenAudio + "cassette_play.ogg");
            so.FindProperty("rewindClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(GenAudio + "cassette_rewind.ogg");
            so.ApplyModifiedPropertiesWithoutUndo();
            recorder.AddComponent<OldMediaFilter>();   // cassette feel; set preset in inspector
            return tape;
        }

        static void BuildGenerator()
        {
            var g = new GameObject("Generator_Hum (safety)"); g.transform.SetParent(world);
            g.transform.position = new Vector3(-1.5f, 1, -6);
            var s = g.AddComponent<AudioSource>();
            s.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GenAudio + "generator_hum_loop.ogg");
            s.loop = true; s.playOnAwake = true; s.spatialBlend = 0.7f; s.volume = 0.35f; s.maxDistance = 40;

            var d = new GameObject("Dread_Drone (under everything)"); d.transform.SetParent(world);
            var ds = d.AddComponent<AudioSource>();
            ds.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GenAudio + "dread_drone_loop.ogg");
            ds.loop = true; ds.playOnAwake = true; ds.spatialBlend = 0; ds.volume = 0.08f;
        }

        static void BuildLights()
        {
            var l = new GameObject("LIGHTS").transform; l.SetParent(world);
            void Point(string n, Vector3 p, Color c, float range, float intensity)
            {
                var go = new GameObject(n); go.transform.SetParent(l); go.transform.position = p;
                var li = go.AddComponent<Light>(); li.type = LightType.Point; li.color = c; li.range = range; li.intensity = intensity;
                li.shadows = LightShadows.Soft;
                if (!go.GetComponent<HDAdditionalLightData>()) go.AddComponent<HDAdditionalLightData>();
            }
            Color neon = new Color(1f, 0.95f, 0.8f);      // yellowish white, never green
            Point("Bulb_Storeroom", new Vector3(0.3f, 2.3f, -2.5f), new Color(1f, 0.85f, 0.6f), 5, 120);
            Point("Neon_Corridor_1", new Vector3(0, 2.35f, 4), neon, 6, 90);
            Point("Neon_Corridor_2", new Vector3(0, 2.35f, 11), neon, 6, 60);
            Point("Neon_Segment2", new Vector3(4.5f, 2.35f, 14.1f), neon, 5, 40);
            Point("Lantern_Glow_PumpHall", new Vector3(17, 1.3f, 12), new Color(1f, 0.7f, 0.35f), 6, 80);
        }

        static void BuildVolume()
        {
            var go = new GameObject("HDRP_Volume (fog + fixed exposure)"); go.transform.SetParent(world);
            var v = go.AddComponent<Volume>(); v.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var fog = profile.Add<Fog>(true);
            fog.enabled.Override(true);
            fog.meanFreePath.Override(9f);          // thick, cheap to tune
            fog.enableVolumetricFog.Override(true);
            var exp = profile.Add<Exposure>(true);
            exp.mode.Override(ExposureMode.Fixed);
            exp.fixedExposure.Override(5.5f);       // darkness stays dark
            AssetDatabase.CreateAsset(profile, Root + "/Scenes/Demo_Greybox_Volume.asset");
            v.sharedProfile = profile;
        }

        static void BuildAli(Vector3 pos)
        {
            var ali = new GameObject("ALI");
            ali.layer = LayerMask.NameToLayer("Player");
            ali.transform.position = pos;
            var cc = ali.AddComponent<CharacterController>(); cc.height = 1.75f; cc.radius = 0.3f; cc.center = Vector3.up * 0.875f;

            var camGo = new GameObject("Camera"); camGo.tag = "MainCamera";
            camGo.transform.SetParent(ali.transform, false); camGo.transform.localPosition = Vector3.up * 1.6f;
            var cam = camGo.AddComponent<Camera>(); cam.nearClipPlane = 0.05f; cam.fieldOfView = 68;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<Interactor>();

            var lightGo = new GameObject("Flashlight"); lightGo.transform.SetParent(camGo.transform, false);
            lightGo.transform.localPosition = new Vector3(0.18f, -0.15f, 0.2f);
            var spot = lightGo.AddComponent<Light>(); spot.type = LightType.Spot; spot.spotAngle = 48; spot.range = 18; spot.shadows = LightShadows.Soft;
            if (!lightGo.GetComponent<HDAdditionalLightData>()) lightGo.AddComponent<HDAdditionalLightData>();
            var clickSrc = lightGo.AddComponent<AudioSource>(); clickSrc.playOnAwake = false;
            var fl = lightGo.AddComponent<Flashlight>();
            Set(fl, "clickSource", clickSrc);

            var pulse = camGo.AddComponent<EchoPulse>();
            var tapSrc = camGo.AddComponent<AudioSource>(); tapSrc.playOnAwake = false;
            Set(pulse, "tapSource", tapSrc);
            Set(pulse, "tapClip", AssetDatabase.LoadAssetAtPath<AudioClip>(GenAudio + "flashlight_tap.ogg"));
            Set(pulse, "echoLayers", ~(LayerMask.GetMask("Player", "Hallucination")));
            var pm = new Material(Shader.Find("HDRP/Unlit")); pm.SetColor("_UnlitColor", new Color(0.62f, 0.93f, 0.76f));
            AssetDatabase.CreateAsset(pm, MatDir + "/EchoPoint.mat");
            Set(pulse, "pointMaterial", pm);

            var stepSrc = ali.AddComponent<AudioSource>(); stepSrc.playOnAwake = false; stepSrc.spatialBlend = 0;
            var ctrl = ali.AddComponent<AliController>();
            Set(ctrl, "cameraPivot", camGo.transform);
            Set(ctrl, "footstepSource", stepSrc);
            var steps = new SerializedObject(ctrl);
            var arr = steps.FindProperty("footstepClips"); arr.arraySize = 1;
            arr.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(GenAudio + "shoe_concrete_step.ogg");
            steps.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- utils ----------

        static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[EchoStation] Field '{field}' not found on {target.GetType().Name}"); return; }
            switch (value)
            {
                case Object o: p.objectReferenceValue = o; break;
                case int i when p.propertyType == SerializedPropertyType.Enum: p.enumValueIndex = i; break;
                case int i when p.propertyType == SerializedPropertyType.LayerMask: p.intValue = i; break;
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case bool b: p.boolValue = b; break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureLayer(int index, string name)
        {
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tm.FindProperty("layers");
            var p = layers.GetArrayElementAtIndex(index);
            if (string.IsNullOrEmpty(p.stringValue)) { p.stringValue = name; tm.ApplyModifiedPropertiesWithoutUndo(); }
            else if (p.stringValue != name) Debug.LogWarning($"[EchoStation] Layer {index} is '{p.stringValue}', expected '{name}'.");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
#endif
