#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.AI;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine.InputSystem.UI;
using EchoStation.Audio;
using EchoStation.Echo;
using EchoStation.Haunt;
using EchoStation.Killer;
using EchoStation.Loop;
using EchoStation.Player;
using EchoStation.Puzzles;
using EchoStation.World;

namespace EchoStation.EditorTools
{
    /// <summary>
    /// Menu: EchoStation > Build Story Demo.
    /// Builds a complete playable narrative slice in HDRP primitives so the demo can be walked end to end:
    /// storeroom (wake-up) > vestibule A > corridor (14 m) > blind corner > vestibule B (loop trigger),
    /// workshop, control room, pump hall with grate, drain pipe, levee with reed rows, broken car, police car.
    /// Also: layers, Ali rig with all scripts wired, living tape, generator hum, HDRP volume (fog + fixed exposure).
    /// Re-running deletes the old scene. Every number here is a starting point for tuning.
    /// </summary>
    public static class DemoGreyboxBuilder
    {
        const string Root = "Assets/_EchoStation";
        const string ScenePath = Root + "/Scenes/Demo_Story.unity";
        const string GreyboxScenePath = Root + "/Scenes/Demo_Greybox.unity";
        const string GenAudio = Root + "/Audio/Generated/";
        const string MatDir = Root + "/Art/Greybox";

        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        static Transform world;
        static TMP_FontAsset demoFont;

        [MenuItem("EchoStation/Build Story Demo", priority = 0)]
        public static void Build()
        {
            if (!EditorUtility.DisplayDialog("Echo Station", "Build the playable story demo? The earlier greybox scene will be kept.", "Build", "Cancel")) return;

            // The built-in English TMP font is used for the complete demo UI and subtitles.
            demoFont = TMP_Settings.GetFontAsset();
            if (demoFont == null)
            {
                const string message = "TextMesh Pro Essentials are missing. Import TMP Essential Resources from Window > TextMeshPro, then build the demo again.";
                Debug.LogError("[EchoStation] " + message);
                EditorUtility.DisplayDialog("Echo Station", message, "OK");
                return;
            }

            EnsureLayer(8, "Player");
            EnsureLayer(9, "Hallucination");
            EnsureLayer(10, "Stalker");
            EnsureFolder(Root + "/Scenes"); EnsureFolder(MatDir);
            mats.Clear();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            world = new GameObject("WORLD").transform;

            BuildVolume();
            var storeroom = Room("01_Storeroom", new Vector3(0, 0, -4.4f), new Vector3(5.6f, 2.8f, 7f), "concrete", EchoMaterial.Concrete, openZPlus: true);
            // Vestibule A: a sealed 2.5 m box between two doors. Vestibule B (after door B) is its exact twin: the loop swaps them.
            Box("VestibuleA_Floor", new Vector3(0, -0.05f, -0.25f), new Vector3(1.8f, 0.1f, 2.5f), "concrete", EchoMaterial.Concrete, storeroom);
            Box("VestibuleA_Ceil", new Vector3(0, 2.5f, -0.25f), new Vector3(2.0f, 0.1f, 2.6f), "concrete", EchoMaterial.Concrete, storeroom);
            Box("VestibuleA_WallL", new Vector3(-0.95f, 1.3f, -0.25f), new Vector3(0.1f, 2.6f, 2.5f), "paint", EchoMaterial.Concrete, storeroom);
            Box("VestibuleA_WallR", new Vector3(0.95f, 1.3f, -0.25f), new Vector3(0.1f, 2.6f, 2.5f), "paint", EchoMaterial.Concrete, storeroom);
            DoorFrame("StoreFrame", new Vector3(0, 0, -0.9f), false, storeroom);
            Door("StoreDoor", new Vector3(0.55f, 0, -0.9f), new Vector3(-0.55f, 1.05f, 0), new Vector3(1.1f, 2.1f, 0.08f), Vector3.forward, -95f, storeroom);
            BuildCorridor(out Transform exitAnchor, out Transform entryAnchor, out BoxCollider loopTrigger);
            Room("04_Workshop", new Vector3(-2.9f, 0, 6.5f), new Vector3(4, 2.6f, 3), "concrete", EchoMaterial.Concrete, openXPlus: true);
            Room("05_ControlRoom", new Vector3(3.5f, 0, 16.6f), new Vector3(3, 2.6f, 3), "concrete", EchoMaterial.Concrete, openZMinus: true);
            BuildPumpHall();
            BuildPipeAndOutside();

            // Props on the bench: the living tape
            var bench = Box("Bench", new Vector3(-0.85f, 0.39f, -3.9f), new Vector3(1.15f, 0.08f, 0.58f), "wood", EchoMaterial.Concrete, world);
            var recorder = Box("TapeRecorder", new Vector3(-0.95f, 0.53f, -3.9f), new Vector3(0.3f, 0.2f, 0.45f), "dark", EchoMaterial.Tape, world);
            var tape = SetupTape(recorder.gameObject);
            Box("TeaGlass_RED", new Vector3(-0.48f, 0.48f, -3.98f), new Vector3(0.06f, 0.1f, 0.06f), "tea", EchoMaterial.Glass, world);
            Box("WallClock_0547", new Vector3(-4.82f, 1.9f, 6.55f), new Vector3(0.05f, 0.4f, 0.4f), "paper", EchoMaterial.Glass, world);
            var calendar = Box("Calendar_Jan1994", new Vector3(0.8f, 1.5f, -7.82f), new Vector3(0.02f, 0.5f, 0.35f), "paper", EchoMaterial.Concrete, world);
            calendar.rotation = Quaternion.Euler(0f, 90f, 0f);

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
            WireAnomalies(director);
            BuildAmbience();
            DressStation();
            DressRooms();
            var ali = BuildAli(new Vector3(0.6f, 0.05f, -4.7f), out Transform cameraPivot, out Flashlight flashlight, out EchoPulse pulse);
            BuildTapeEntries(tape);
            BuildStationPuzzles(tape, ali, cameraPivot, out ClockPuzzle clockPuzzle, out KnockRhythmPuzzle knockPuzzle,
                out CassetteFlipPuzzle cassettePuzzle, out MuffledSearchPuzzle muffledPuzzle);
            var ui = BuildPlayerUI(ali, cameraPivot, flashlight, pulse, out Subtitles subtitles, out Image eyelids, out Image caughtBlack,
                out GameObject warningScreen, out Toggle reduceFlash, out Button continueButton, out Transform titleText, out Transform wishlistText);
            BuildWake(ali, cameraPivot, flashlight, pulse, eyelids);
            BuildStoryAndThreats(ali, cameraPivot, tape, director, loopTrigger.gameObject.GetComponent<LoopCorridor>(),
                subtitles, caughtBlack, titleText, wishlistText, clockPuzzle, knockPuzzle, cassettePuzzle, muffledPuzzle,
                out CarEnding carEnding, out TantalStalker tantal);
            BuildGameMenus(ali, warningScreen, reduceFlash, continueButton);
            EditorSceneManager.SaveScene(scene, ScenePath);
            // SaveScene creates the meta file; now the build profile can bind the real scene GUID.
            ConfigureBuildEntryPoint();
            Debug.Log("[EchoStation] Story demo built: " + ScenePath + ". The old greybox scene is untouched.");
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
            Box("WallFar2a", new Vector3(1.925f, h / 2, 15.05f), new Vector3(2.05f, h, 0.1f), "paint", EchoMaterial.Concrete, c); // gap = control room door
            Box("WallFar2b", new Vector3(6.075f, h / 2, 15.05f), new Vector3(4.05f, h, 0.1f), "paint", EchoMaterial.Concrete, c);
            Box("WallNear2", new Vector3(4.45f, h / 2, 13.15f), new Vector3(7.1f, h, 0.1f), "paint", EchoMaterial.Concrete, c);
            // Door A (vestibule A -> corridor) and door B (corridor -> vestibule B): real doors that open and shut behind you.
            DoorFrame("FrameA", new Vector3(0, 0, 1.0f), false, c);
            Door("DoorA", new Vector3(-0.55f, 0, 1.0f), new Vector3(0.55f, 1.05f, 0), new Vector3(1.1f, 2.1f, 0.08f), Vector3.forward, -95f, c);
            DoorFrame("FrameB", new Vector3(8.05f, 0, 14.1f), true, c);
            Door("DoorB", new Vector3(8.05f, 0, 14.65f), new Vector3(0, 1.05f, -0.55f), new Vector3(0.08f, 2.1f, 1.1f), Vector3.right, -95f, c);
            // Vestibule B mirrors vestibule A on each pass, but its far door opens after the final loop.
            Box("VestibuleB_Floor", new Vector3(9.3f, -0.05f, 14.1f), new Vector3(2.5f, 0.1f, 1.8f), "concrete", EchoMaterial.Concrete, c);
            Box("VestibuleB_Ceil", new Vector3(9.3f, 2.5f, 14.1f), new Vector3(2.6f, 0.1f, 2.0f), "concrete", EchoMaterial.Concrete, c);
            Box("VestibuleB_WallL", new Vector3(9.3f, 1.3f, 15.05f), new Vector3(2.5f, 2.6f, 0.1f), "paint", EchoMaterial.Concrete, c);
            Box("VestibuleB_WallR", new Vector3(9.3f, 1.3f, 13.15f), new Vector3(2.5f, 2.6f, 0.1f), "paint", EchoMaterial.Concrete, c);
            DoorFrame("FrameB_Far", new Vector3(10.55f, 0, 14.1f), true, c);
            Door("PumpHall_Door", new Vector3(10.55f, 0, 13.55f), new Vector3(0, 1.05f, 0.55f),
                new Vector3(0.08f, 2.1f, 1.1f), Vector3.right, -95f, c);

            // Anchors: same spot inside each vestibule (1.25 m in from the door behind you).
            var exitGo = new GameObject("ExitAnchor_VestibuleB"); exitGo.transform.SetParent(c);
            exitGo.transform.SetPositionAndRotation(new Vector3(9.3f, 0, 14.1f), Quaternion.Euler(0, 90, 0));
            var entryGo = new GameObject("EntryAnchor_VestibuleA"); entryGo.transform.SetParent(c);
            entryGo.transform.SetPositionAndRotation(new Vector3(0, 0, -0.25f), Quaternion.Euler(0, 0, 0));
            exitAnchor = exitGo.transform; entryAnchor = entryGo.transform;

            var trig = new GameObject("LoopTrigger_VestibuleB"); trig.transform.SetParent(c);
            trig.transform.position = new Vector3(9.7f, 1.2f, 14.1f);
            loopTrigger = trig.AddComponent<BoxCollider>(); loopTrigger.isTrigger = true; loopTrigger.size = new Vector3(0.3f, 2.4f, w);

            var back = new GameObject("TurnBackTrigger_DoorA"); back.transform.SetParent(c);
            back.transform.position = new Vector3(0, 1.2f, 1.7f);
            var bb = back.AddComponent<BoxCollider>(); bb.isTrigger = true; bb.size = new Vector3(w, 2.4f, 0.3f);
        }

        static void BuildPumpHall()
        {
            var p = Room("06_PumpHall", new Vector3(14, -1, 14.1f), new Vector3(10, 4.5f, 10), "concrete", EchoMaterial.Concrete, openXMinus: true);
            p.position += Vector3.right * 2f; // align the entrance with Vestibule B before laying out machinery
            Box("Water_KneeDeep", new Vector3(16, -0.55f, 14.1f), new Vector3(9.8f, 0.9f, 9.8f), "water", EchoMaterial.Water, p).GetComponent<Collider>().isTrigger = true;

            // A narrow, raised catwalk leaves the stagnant water visible on both sides.
            for (int i = 0; i < 38; i++)
                Box("Catwalk_GrateBar_" + i, new Vector3(11.55f + i * 0.24f, -0.015f, 14.1f), new Vector3(0.15f, 0.065f, 1.8f), "steel", EchoMaterial.Metal, p);
            Box("Catwalk_Girder_L", new Vector3(16f, -0.11f, 13.18f), new Vector3(9.3f, 0.12f, 0.10f), "rust", EchoMaterial.Metal, p);
            Box("Catwalk_Girder_R", new Vector3(16f, -0.11f, 15.02f), new Vector3(9.3f, 0.12f, 0.10f), "rust", EchoMaterial.Metal, p);
            // A grated service spur gives the player a loop around the pump and a real hiding pocket.
            for (int i = 0; i < 13; i++)
                Box("PumpPlatform_GrateBar_" + i, new Vector3(18.05f, -0.015f, 15.25f + i * 0.23f), new Vector3(3.0f, 0.065f, 0.15f), "steel", EchoMaterial.Metal, p);
            Box("PumpPlatform_Girder_L", new Vector3(16.55f, -0.11f, 16.7f), new Vector3(0.10f, 0.12f, 3.3f), "rust", EchoMaterial.Metal, p);
            Box("PumpPlatform_Girder_R", new Vector3(19.55f, -0.11f, 16.7f), new Vector3(0.10f, 0.12f, 3.3f), "rust", EchoMaterial.Metal, p);
            AddCatwalkRails(p);

            // The former cube placeholder is now a floor-mounted centrifugal pump and electric motor.
            Box("Pump_Skid", new Vector3(18.35f, -0.84f, 16.35f), new Vector3(3.65f, 0.34f, 1.75f), "steel", EchoMaterial.Metal, p);
            VisualBox("Pump_MotorFoot_L", new Vector3(18.95f, -0.60f, 16.01f), new Vector3(0.58f, 0.22f, 0.14f), "steel", p);
            VisualBox("Pump_MotorFoot_R", new Vector3(18.95f, -0.60f, 16.69f), new Vector3(0.58f, 0.22f, 0.14f), "steel", p);
            VisualBox("Pump_HousingFoot_L", new Vector3(20.10f, -0.72f, 15.98f), new Vector3(0.42f, 0.18f, 0.14f), "steel", p);
            VisualBox("Pump_HousingFoot_R", new Vector3(20.10f, -0.72f, 16.72f), new Vector3(0.42f, 0.18f, 0.14f), "steel", p);
            VisualCylinder("Pump_Motor", new Vector3(18.70f, -0.10f, 16.35f), new Vector3(19.75f, -0.10f, 16.35f), 0.43f, "steel", p);
            VisualCylinder("Pump_Coupling", new Vector3(19.68f, -0.10f, 16.35f), new Vector3(20.02f, -0.10f, 16.35f), 0.22f, "rust", p);
            VisualSphere("Pump_VoluteHousing", new Vector3(20.18f, -0.32f, 16.35f), new Vector3(0.74f, 1.15f, 1.12f), "rust", p);
            VisualCylinder("Pump_SuctionPipe", new Vector3(20.20f, -0.45f, 16.35f), new Vector3(20.90f, -0.45f, 16.35f), 0.25f, "rust", p);
            VisualCylinder("Pump_DischargePipe", new Vector3(20.50f, -0.25f, 16.35f), new Vector3(20.50f, 2.90f, 16.35f), 0.22f, "rust", p);
            VisualCylinder("Pump_DischargeElbow", new Vector3(20.50f, 2.85f, 16.35f), new Vector3(19.20f, 2.85f, 16.35f), 0.22f, "rust", p);
            AddFlange("Pump_Flange_Lower", new Vector3(20.50f, 0.15f, 16.35f), true, 0.34f, p);
            AddFlange("Pump_Flange_Upper", new Vector3(20.50f, 2.15f, 16.35f), true, 0.34f, p);
            for (int i = 0; i < 8; i++)
            {
                float x = 18.68f + i * 0.14f;
                VisualBox("Motor_CoolingFin_" + i, new Vector3(x, -0.08f, 16.35f), new Vector3(0.035f, 0.63f, 0.88f), "dark", p);
            }
            for (int i = 0; i < 6; i++)
            {
                float z = 12.0f + i * 1.35f;
                VisualCylinder("Overhead_WaterMain_" + i, new Vector3(12.0f, 2.93f, z), new Vector3(20.55f, 2.93f, z), 0.085f, "rust", p);
            }

            // Corroded ladder, wall braces and waterline stains make the room read as a working utility space.
            for (int i = 0; i < 5; i++)
                VisualCylinder("WallLadder_Rung_" + i, new Vector3(20.78f, -0.45f + i * 0.42f, 11.0f), new Vector3(20.78f, -0.45f + i * 0.42f, 11.62f), 0.025f, "steel", p);
            VisualCylinder("WallLadder_Rail_L", new Vector3(20.78f, -0.8f, 10.95f), new Vector3(20.78f, 1.35f, 10.95f), 0.035f, "rust", p);
            VisualCylinder("WallLadder_Rail_R", new Vector3(20.78f, -0.8f, 11.67f), new Vector3(20.78f, 1.35f, 11.67f), 0.035f, "rust", p);
            VisualBox("Waterline_Stain_Back", new Vector3(16f, -0.17f, 19.04f), new Vector3(8.9f, 0.12f, 0.025f), "dado", p);
            RoomPlaque("PumpHall_Plaque", "PUMP HALL  /  06", new Vector3(11.08f, 2.18f, 12.9f), new Vector3(1.25f, 0.32f, 0.06f), Quaternion.Euler(0, -90, 0), p);

            var killer = Box("KILLER_Placeholder", new Vector3(19f, 0.9f, 12), new Vector3(0.5f, 1.8f, 0.35f), "dark", EchoMaterial.Body, p);
            killer.gameObject.SetActive(false);   // task 3 (KillerController) brings him to life
            var hide = new GameObject("Hide_BehindPump"); hide.transform.SetParent(p);
            hide.transform.position = new Vector3(17.15f, 0.32f, 17.45f);
            var hideVolume = hide.AddComponent<BoxCollider>(); hideVolume.isTrigger = true; hideVolume.size = new Vector3(1.2f, 0.6f, 1.1f);
            hide.AddComponent<HidingSpot>();
        }

        static void BuildPipeAndOutside()
        {
            var o = new GameObject("07_Pipe_Levee_Car").transform; o.SetParent(world); o.position = Vector3.right * 2f;
            // drain pipe: 20 m crawl, 0.9 m high
            Box("Culvert_Floor", new Vector3(31, -1.05f, 14.1f), new Vector3(20, 0.12f, 1.08f), "concrete", EchoMaterial.Concrete, o);
            Box("Culvert_Crown", new Vector3(31, -0.16f, 14.1f), new Vector3(20, 0.12f, 1.08f), "concrete", EchoMaterial.Concrete, o);
            Box("Culvert_WallL", new Vector3(31, -0.60f, 13.53f), new Vector3(20, 0.92f, 0.12f), "concrete", EchoMaterial.Concrete, o);
            Box("Culvert_WallR", new Vector3(31, -0.60f, 14.67f), new Vector3(20, 0.92f, 0.12f), "concrete", EchoMaterial.Concrete, o);
            for (int i = 0; i < 8; i++)
            {
                float x = 22.0f + i * 2.55f;
                VisualBox("Culvert_Rib_" + i, new Vector3(x, -0.59f, 14.1f), new Vector3(0.08f, 0.77f, 1.03f), "rust", o);
                VisualBox("Culvert_FloorSeam_" + i, new Vector3(x, -0.98f, 14.1f), new Vector3(0.04f, 0.015f, 0.92f), "dark", o);
            }
            for (int i = 0; i < 13; i++)
                VisualCylinder("Culvert_CableBracket_" + i, new Vector3(21.5f + i * 1.55f, -0.29f, 13.90f), new Vector3(21.5f + i * 1.55f, -0.29f, 14.30f), 0.025f, "steel", o);

            // Dry canal bed, raised service road, and sloped levees give the outside a readable landform.
            Box("CanalBed", new Vector3(62, -1.2f, 14.1f), new Vector3(42, 0.2f, 30), "mud", EchoMaterial.Concrete, o);
            Box("Levee_Road", new Vector3(62, -1.02f, 14.1f), new Vector3(42, 0.16f, 5.6f), "concrete", EchoMaterial.Concrete, o);
            for (int i = 0; i < 9; i++)
            {
                float x = 43f + i * 4.7f;
                VisualBox("Road_EdgeMark_L_" + i, new Vector3(x, -0.934f, 11.42f), new Vector3(2.2f, 0.012f, 0.045f), "paper", o);
                VisualBox("Road_EdgeMark_R_" + i, new Vector3(x, -0.934f, 16.78f), new Vector3(2.2f, 0.012f, 0.045f), "paper", o);
            }
            CreateLevee("Levee_NorthBank", new Vector3(62, -1.1f, 25.7f), 42f, 9.5f, 3.2f, 2.1f, o);
            CreateLevee("Levee_SouthBank", new Vector3(62, -1.1f, 2.0f), 42f, 8.2f, 2.6f, 1.55f, o);
            BuildReedField();

            // Roadside infrastructure and a proper route marker anchor the marsh in place.
            VisualCylinder("RoadMarker_Post", new Vector3(71.0f, -0.90f, 20.4f), new Vector3(71.0f, 1.55f, 20.4f), 0.075f, "rust", o);
            RoomPlaque("Sign_UmmAlShuwaich", "UMM AL-SHUWAICH  /  04", new Vector3(71.0f, 1.25f, 20.4f), new Vector3(1.75f, 0.66f, 0.10f), Quaternion.Euler(0, 180, 0), o);
            for (int i = 0; i < 7; i++)
            {
                float x = 46f + i * 5.7f;
                VisualCylinder("Levee_ReflectorPost_" + i, new Vector3(x, -0.8f, 18.75f), new Vector3(x, -0.12f, 18.75f), 0.035f, "paper", o);
            }

            BuildVehicle("BrokenCar_Hazards", new Vector3(76f, -1.08f, 20.0f), -9f, true, o);
            BuildVehicle("PoliceCar_Brazili", new Vector3(80f, -0.94f, 16.0f), 2f, false, o);
            BuildDeadFish(o);
        }

        static void AddCatwalkRails(Transform parent)
        {
            const float deckY = 0.02f, railY = 0.68f;
            // Main handrail along the open-water side; the pump side has a deliberate access gap.
            VisualCylinder("Catwalk_Handrail_West", new Vector3(11.55f, railY, 13.18f), new Vector3(20.45f, railY, 13.18f), 0.035f, "steel", parent);
            VisualCylinder("Catwalk_Handrail_East_A", new Vector3(11.55f, railY, 15.02f), new Vector3(16.15f, railY, 15.02f), 0.035f, "steel", parent);
            VisualCylinder("Catwalk_Handrail_East_B", new Vector3(19.95f, railY, 15.02f), new Vector3(20.45f, railY, 15.02f), 0.035f, "steel", parent);
            for (int i = 0; i <= 15; i++)
            {
                float x = 11.55f + i * 0.58f;
                foreach (float z in new[] { 13.18f, 15.02f })
                {
                    if (z > 14f && x > 16.1f && x < 19.9f) continue;
                    VisualCylinder($"Catwalk_Post_{i}_{z}", new Vector3(x, deckY, z), new Vector3(x, railY, z), 0.035f, "rust", parent);
                }
            }
            // Safety rails around the service spur, with an opening back to the main catwalk.
            VisualCylinder("PumpPlatform_Handrail_Back", new Vector3(16.55f, railY, 18.35f), new Vector3(19.55f, railY, 18.35f), 0.035f, "steel", parent);
            VisualCylinder("PumpPlatform_Handrail_Left", new Vector3(16.55f, railY, 17.1f), new Vector3(16.55f, railY, 18.35f), 0.035f, "steel", parent);
            VisualCylinder("PumpPlatform_Handrail_Right", new Vector3(19.55f, railY, 17.1f), new Vector3(19.55f, railY, 18.35f), 0.035f, "steel", parent);
            for (int i = 0; i < 4; i++)
            {
                float x = 16.55f + i;
                VisualCylinder("PumpPlatform_Post_Back_" + i, new Vector3(x, deckY, 18.35f), new Vector3(x, railY, 18.35f), 0.035f, "rust", parent);
            }
        }

        static void AddFlange(string name, Vector3 center, bool verticalPipe, float radius, Transform parent)
        {
            Vector3 axis = verticalPipe ? Vector3.up : Vector3.right;
            VisualCylinder(name + "_Plate", center - axis * 0.055f, center + axis * 0.055f, radius, "steel", parent);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                Vector3 offset = verticalPipe
                    ? new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius * 0.82f
                    : new Vector3(0, Mathf.Cos(a), Mathf.Sin(a)) * radius * 0.82f;
                VisualSphere(name + "_Bolt_" + i, center + offset, Vector3.one * 0.055f, "dark", parent);
            }
        }

        static void DressRooms()
        {
            // Workshop benches carry the recorded evidence; the wake-up tea and recorder remain in the storeroom.
            VisualBox("Workshop_BenchTop", new Vector3(-2.75f, 0.48f, 6.5f), new Vector3(1.65f, 0.08f, 0.62f), "wood", world);
            foreach (float x in new[] { -3.42f, -2.08f })
                foreach (float z in new[] { 6.23f, 6.77f })
                    VisualBox("Workshop_BenchLeg", new Vector3(x, 0.26f, z), new Vector3(0.07f, 0.52f, 0.07f), "steel", world);
            VisualBox("Workshop_BenchApron", new Vector3(-2.75f, 0.34f, 6.5f), new Vector3(1.53f, 0.12f, 0.50f), "wood", world);
            VisualBox("Workshop_ToolShelf", new Vector3(-2.85f, 1.26f, 7.82f), new Vector3(1.75f, 0.08f, 0.22f), "wood", world);
            VisualBox("Workshop_ToolShelf_BracketL", new Vector3(-3.58f, 1.04f, 7.80f), new Vector3(0.05f, 0.46f, 0.08f), "rust", world);
            VisualBox("Workshop_ToolShelf_BracketR", new Vector3(-2.12f, 1.04f, 7.80f), new Vector3(0.05f, 0.46f, 0.08f), "rust", world);
            for (int i = 0; i < 4; i++)
            {
                float x = -3.45f + i * 0.42f;
                VisualBox("Workshop_TapeSpine_" + i, new Vector3(x, 1.28f, 7.67f), new Vector3(0.30f, 0.06f, 0.13f), i == 3 ? "paper" : "dark", world);
                VisualBox("Workshop_TapeLabel_" + i, new Vector3(x, 1.315f, 7.67f), new Vector3(0.22f, 0.018f, 0.08f), "paper", world);
            }
            // The story's tapes hang on a line, with one empty place in the middle.
            VisualCylinder("Workshop_TapeLine", new Vector3(-4.45f, 2.05f, 5.65f), new Vector3(-1.25f, 2.05f, 5.65f), 0.012f, "steel", world);
            VisualBox("Workshop_BulkheadLamp", new Vector3(-2.85f, 2.47f, 6.35f), new Vector3(0.86f, 0.045f, 0.18f), "paper", world);
            for (int i = 0; i < 5; i++)
            {
                float x = -4.0f + i * 0.62f;
                if (i != 2)
                {
                    VisualBox("HangingCassette_" + i, new Vector3(x, 1.78f, 5.65f), new Vector3(0.32f, 0.20f, 0.055f), "dark", world);
                    VisualBox("HangingCassette_Label_" + i, new Vector3(x, 1.79f, 5.615f), new Vector3(0.21f, 0.07f, 0.012f), "paper", world);
                }
                VisualCylinder("CassetteString_" + i, new Vector3(x, 1.90f, 5.65f), new Vector3(x, 2.05f, 5.65f), 0.009f, "steel", world);
            }
            VisualCylinder("Workshop_WaterDrum", new Vector3(-4.22f, 0.0f, 6.35f), new Vector3(-4.22f, 0.85f, 6.35f), 0.34f, "rust", world);
            VisualCylinder("Workshop_Drum_RimTop", new Vector3(-4.22f, 0.80f, 6.35f), new Vector3(-4.22f, 0.87f, 6.35f), 0.36f, "steel", world);
            // An abandoned chair and table are set slightly off-axis, leaving a clear player route.
            VisualBox("Workshop_ChairSeat", new Vector3(-1.45f, 0.58f, 7.18f), new Vector3(0.46f, 0.08f, 0.44f), "wood", world);
            VisualBox("Workshop_ChairBack", new Vector3(-1.45f, 1.05f, 7.38f), new Vector3(0.46f, 0.82f, 0.08f), "wood", world);
            foreach (float x in new[] { -1.63f, -1.27f })
                foreach (float z in new[] { 7.02f, 7.34f })
                    VisualBox("Workshop_ChairLeg", new Vector3(x, 0.27f, z), new Vector3(0.045f, 0.55f, 0.045f), "rust", world);
            VisualCylinder("Workshop_ChairTie", new Vector3(-1.70f, 0.98f, 7.10f), new Vector3(-1.20f, 0.98f, 7.10f), 0.018f, "paper", world);
            RoomPlaque("Workshop_Plaque", "WORKSHOP  /  03", new Vector3(-0.84f, 2.20f, 6.5f), new Vector3(1.15f, 0.28f, 0.045f), Quaternion.Euler(0, 90, 0), world);

            // Control room: operator's console, dead radio, observation pane, and gauges.
            VisualBox("Control_DeskTop", new Vector3(3.50f, 0.88f, 17.45f), new Vector3(2.35f, 0.12f, 0.72f), "wood", world);
            VisualBox("Control_LightHousing", new Vector3(3.45f, 2.47f, 16.45f), new Vector3(0.95f, 0.045f, 0.16f), "paper", world);
            foreach (float x in new[] { 2.48f, 4.52f })
                foreach (float z in new[] { 17.18f, 17.72f })
                    VisualBox("Control_DeskLeg", new Vector3(x, 0.43f, z), new Vector3(0.07f, 0.86f, 0.07f), "steel", world);
            VisualBox("Control_Console", new Vector3(3.52f, 1.22f, 17.45f), new Vector3(1.75f, 0.47f, 0.34f), "steel", world);
            VisualBox("Control_ConsoleFace", new Vector3(3.52f, 1.18f, 17.265f), new Vector3(1.57f, 0.30f, 0.025f), "dark", world);
            for (int i = 0; i < 5; i++)
            {
                float x = 2.92f + i * 0.30f;
                VisualSphere("Control_Indicator_" + i, new Vector3(x, 1.28f, 17.245f), Vector3.one * 0.055f, i == 1 ? "amber" : "dark", world);
                VisualBox("Control_Switch_" + i, new Vector3(x, 1.06f, 17.235f), new Vector3(0.035f, 0.09f, 0.035f), "steel", world);
            }
            VisualBox("Control_ObservationWindow_Frame", new Vector3(2.08f, 1.70f, 16.55f), new Vector3(0.10f, 0.92f, 1.48f), "rust", world);
            VisualBox("Control_ObservationWindow_Glass", new Vector3(2.02f, 1.70f, 16.55f), new Vector3(0.025f, 0.74f, 1.27f), "glass", world);
            for (int i = 0; i < 4; i++)
            {
                float z = 16.05f + i * 0.38f;
                VisualBox("Control_Cabinet", new Vector3(4.75f, 0.93f, z), new Vector3(0.38f, 1.85f, 0.31f), "steel", world);
                VisualBox("Control_Cabinet_Door", new Vector3(4.54f, 0.93f, z), new Vector3(0.025f, 1.67f, 0.24f), "rust", world);
                VisualBox("Control_Cabinet_Latch", new Vector3(4.51f, 0.93f, z + 0.07f), new Vector3(0.04f, 0.18f, 0.025f), "paper", world);
            }
            VisualBox("Control_OperatorChair_Seat", new Vector3(3.50f, 0.58f, 16.42f), new Vector3(0.52f, 0.10f, 0.48f), "wood", world);
            VisualBox("Control_OperatorChair_Back", new Vector3(3.50f, 1.05f, 16.62f), new Vector3(0.52f, 0.82f, 0.08f), "wood", world);
            RoomPlaque("ControlRoom_Plaque", "CONTROL  /  02", new Vector3(3.50f, 2.22f, 15.04f), new Vector3(1.12f, 0.28f, 0.045f), Quaternion.Euler(0, 180, 0), world);

            // Corridor details break the long sightline into believable maintenance bays.
            for (int i = 0; i < 7; i++)
            {
                float z = 2.0f + i * 1.85f;
                VisualBox("Corridor_CeilingCrossbeam_" + i, new Vector3(0f, 2.39f, z), new Vector3(1.67f, 0.06f, 0.10f), "steel", world);
                VisualBox("Corridor_ServiceMarker_" + i, new Vector3(0.86f, 1.78f, z), new Vector3(0.035f, 0.28f, 0.34f), i % 2 == 0 ? "paper" : "rust", world);
            }
            // Low table beside the waking player's hand, with clay vessels and a control box on the wall.
            VisualBox("Storeroom_TableApron", new Vector3(-0.85f, 0.34f, -3.9f), new Vector3(1.03f, 0.12f, 0.48f), "wood", world);
            foreach (float x in new[] { -1.31f, -0.39f })
                foreach (float z in new[] { -4.12f, -3.68f })
                    VisualBox("Storeroom_TableLeg", new Vector3(x, 0.14f, z), new Vector3(0.055f, 0.3f, 0.055f), "steel", world);
            for (int i = 0; i < 3; i++)
            {
                float z = -5.25f + i * 0.70f;
                VisualSphere("Storeroom_ClayJarBody_" + i, new Vector3(1.68f, 0.54f, z), new Vector3(0.30f, 0.32f, 0.29f), "mud", world);
                VisualCylinder("Storeroom_ClayJarNeck_" + i, new Vector3(1.68f, 0.69f, z), new Vector3(1.68f, 0.83f, z), 0.11f, "mud", world);
                VisualCylinder("Storeroom_ClayJarLip_" + i, new Vector3(1.68f, 0.82f, z), new Vector3(1.68f, 0.87f, z), 0.145f, "rust", world);
            }
            VisualBox("Storeroom_DeadPanel", new Vector3(2.68f, 1.18f, -4.45f), new Vector3(0.16f, 1.18f, 1.12f), "steel", world);
            VisualBox("Storeroom_DeadPanel_Face", new Vector3(2.585f, 1.18f, -4.45f), new Vector3(0.025f, 0.93f, 0.90f), "dark", world);
            for (int i = 0; i < 4; i++)
            {
                VisualSphere("Storeroom_DeadPanel_Button_" + i, new Vector3(2.56f, 1.45f - i * 0.18f, -4.60f + (i % 2) * 0.28f), Vector3.one * 0.07f, i == 0 ? "amber" : "rust", world);
            }
            VisualCylinder("Storeroom_PanelConduit", new Vector3(2.70f, 1.77f, -4.45f), new Vector3(2.70f, 2.46f, -4.45f), 0.035f, "rust", world);
            foreach (float x in new[] { 1.53f, 1.92f })
                foreach (float z in new[] { -5.55f, -3.55f })
                    VisualBox("Storeroom_ShelfUpright", new Vector3(x, 1.0f, z), new Vector3(0.045f, 2.0f, 0.045f), "steel", world);
            for (int i = 0; i < 4; i++)
            {
                float y = 0.35f + i * 0.47f;
                VisualBox("Storeroom_Shelf", new Vector3(1.72f, y, -4.55f), new Vector3(0.44f, 0.045f, 2.02f), "wood", world);
            }
            RoomPlaque("Storeroom_Plaque", "STORE  /  01", new Vector3(0.0f, 2.18f, -0.84f), new Vector3(1.05f, 0.28f, 0.045f), Quaternion.identity, world);
        }

        static void BuildVehicle(string name, Vector3 groundPosition, float yaw, bool damaged, Transform parent)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent); root.position = groundPosition; root.rotation = Quaternion.Euler(0f, yaw, 0f);
            string bodyMat = damaged ? "steel" : "white";
            LocalBox("Chassis", root, new Vector3(0, 0.50f, 0), new Vector3(1.70f, 0.42f, 3.85f), "dark");
            LocalBox("Body_Lower", root, new Vector3(0, 0.72f, 0), new Vector3(1.88f, 0.54f, 4.10f), bodyMat);
            LocalBox("Hood", root, new Vector3(0, 1.02f, 1.22f), new Vector3(1.82f, 0.25f, 1.33f), bodyMat);
            LocalBox("Trunk", root, new Vector3(0, 0.99f, -1.47f), new Vector3(1.80f, 0.24f, 0.91f), bodyMat);
            LocalBox("Cabin", root, new Vector3(0, 1.45f, -0.10f), new Vector3(1.56f, 0.76f, 1.95f), bodyMat);
            LocalBox("Roof", root, new Vector3(0, 1.86f, -0.08f), new Vector3(1.43f, 0.10f, 1.52f), bodyMat);
            LocalBox("Windshield", root, new Vector3(0, 1.48f, 0.90f), new Vector3(1.39f, 0.55f, 0.055f), "glass", Quaternion.Euler(18f, 0f, 0f));
            LocalBox("RearWindow", root, new Vector3(0, 1.47f, -1.10f), new Vector3(1.37f, 0.52f, 0.055f), "glass", Quaternion.Euler(-18f, 0f, 0f));
            foreach (float side in new[] { -1f, 1f })
            {
                LocalBox("FrontDoorWindow", root, new Vector3(side * 0.795f, 1.49f, 0.42f), new Vector3(0.035f, 0.43f, 0.67f), "glass");
                LocalBox("RearDoorWindow", root, new Vector3(side * 0.795f, 1.49f, -0.40f), new Vector3(0.035f, 0.43f, 0.62f), "glass");
                LocalBox("DoorHandle", root, new Vector3(side * 0.96f, 0.92f, -0.02f), new Vector3(0.025f, 0.04f, 0.22f), "steel");
                LocalBox("SideMirror", root, new Vector3(side * 0.95f, 1.35f, 0.76f), new Vector3(0.15f, 0.11f, 0.20f), "dark");
            }
            LocalBox("FrontBumper", root, new Vector3(0, 0.51f, 2.08f), new Vector3(1.94f, 0.18f, 0.16f), "steel");
            LocalBox("RearBumper", root, new Vector3(0, 0.51f, -2.08f), new Vector3(1.94f, 0.18f, 0.16f), "steel");
            LocalBox("FrontGrille", root, new Vector3(0, 0.78f, 1.94f), new Vector3(0.60f, 0.24f, 0.035f), "dark");
            foreach (float x in new[] { -0.67f, 0.67f })
            {
                LocalBox("Headlamp", root, new Vector3(x, 0.91f, 1.94f), new Vector3(0.34f, 0.18f, 0.035f), "paper");
                LocalBox("TailLamp", root, new Vector3(x, 0.91f, -2.06f), new Vector3(0.29f, 0.17f, 0.035f), "red");
            }
            foreach (float x in new[] { -0.93f, 0.93f })
                foreach (float z in new[] { -1.35f, 1.32f })
                {
                    LocalCylinder("Tyre", root, new Vector3(x, 0.36f, z), 0.36f, 0.23f, Quaternion.Euler(0f, 0f, -90f), "rubber");
                    LocalCylinder("WheelHub", root, new Vector3(x * 1.015f, 0.36f, z), 0.18f, 0.24f, Quaternion.Euler(0f, 0f, -90f), "steel");
                }
            if (damaged)
            {
                LocalBox("MissingDoor_Gap", root, new Vector3(-0.90f, 0.93f, -0.05f), new Vector3(0.035f, 0.56f, 0.98f), "dark", Quaternion.Euler(0f, 7f, 0f));
                LocalBox("BentBonnet", root, new Vector3(0.08f, 1.18f, 1.24f), new Vector3(1.80f, 0.12f, 1.22f), "rust", Quaternion.Euler(0f, 3f, -3f));
                var box = root.gameObject.AddComponent<BoxCollider>(); box.center = new Vector3(0f, 0.95f, 0f); box.size = new Vector3(1.95f, 1.7f, 4.25f);
                HazardLight(root, new Vector3(-0.72f, 0.85f, -2.0f)); HazardLight(root, new Vector3(0.72f, 0.85f, -2.0f));
            }
            else
            {
                LocalBox("PoliceDoorStripe_L", root, new Vector3(-0.949f, 0.82f, -0.02f), new Vector3(0.025f, 0.13f, 1.56f), "blue");
                LocalBox("PoliceDoorStripe_R", root, new Vector3(0.949f, 0.82f, -0.02f), new Vector3(0.025f, 0.13f, 1.56f), "blue");
                LocalBox("LightbarBase", root, new Vector3(0f, 1.96f, -0.08f), new Vector3(0.78f, 0.08f, 0.24f), "dark");
                LocalBox("LightbarBlue", root, new Vector3(-0.22f, 2.05f, -0.08f), new Vector3(0.32f, 0.12f, 0.20f), "blue");
                LocalBox("LightbarRed", root, new Vector3(0.22f, 2.05f, -0.08f), new Vector3(0.32f, 0.12f, 0.20f), "red");
                LocalBox("PoliceRearMark", root, new Vector3(0f, 0.88f, -2.10f), new Vector3(0.68f, 0.17f, 0.035f), "paper");
                var box = root.gameObject.AddComponent<BoxCollider>(); box.center = new Vector3(0f, 0.95f, 0f); box.size = new Vector3(2.0f, 1.7f, 4.35f);
            }
        }

        static void BuildDeadFish(Transform parent)
        {
            var rng = new System.Random(47);
            for (int i = 0; i < 14; i++)
            {
                float x = 46f + (float)rng.NextDouble() * 23f;
                float z = (i % 2 == 0 ? 8.5f : 19.2f) + (float)rng.NextDouble() * 2.1f;
                var fish = VisualSphere("DeadFish_Body_" + i, new Vector3(x, -1.085f, z), new Vector3(0.34f, 0.055f, 0.12f), "paper", parent);
                fish.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                var tail = VisualBox("DeadFish_Tail_" + i, new Vector3(x - 0.18f, -1.082f, z), new Vector3(0.10f, 0.018f, 0.18f), "steel", parent);
                tail.rotation = fish.rotation;
            }
        }

        static void BuildReedField()
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            var rng = new System.Random(1994);
            for (int i = 0; i < 34; i++)
                foreach (bool north in new[] { false, true })
                {
                    float x = 44.0f + i * 1.12f + (float)rng.NextDouble() * 0.55f;
                    float baseZ = north ? 22.0f + (float)rng.NextDouble() * 3.6f : 3.7f + (float)rng.NextDouble() * 3.0f;
                    int stalks = 7 + rng.Next(7);
                    for (int j = 0; j < stalks; j++)
                    {
                        Vector3 basePos = new Vector3(x + (float)rng.NextDouble() * 0.75f, -1.08f, baseZ + (float)rng.NextDouble() * 0.8f);
                        float height = 0.38f + (float)rng.NextDouble() * 1.18f;
                        float width = 0.018f + (float)rng.NextDouble() * 0.022f;
                        float angle = (float)rng.NextDouble() * Mathf.PI;
                        AddReedBlade(vertices, uv, triangles, basePos, height, width, angle);
                        AddReedBlade(vertices, uv, triangles, basePos, height * 0.93f, width * 0.72f, angle + Mathf.PI * 0.5f);
                    }
                }
            var mesh = new Mesh { name = "DeadMarsh_ReedMesh" };
            mesh.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject("DeadMarsh_ReedField"); go.transform.SetParent(world);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>(); Paint(go, "reed");
        }

        static void AddReedBlade(List<Vector3> vertices, List<Vector2> uv, List<int> triangles, Vector3 b, float h, float w, float angle)
        {
            Vector3 across = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (w * 0.5f);
            int start = vertices.Count;
            vertices.Add(b - across); vertices.Add(b + across); vertices.Add(b + Vector3.up * h + across * 0.35f); vertices.Add(b + Vector3.up * h - across * 0.35f);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2); triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
            triangles.Add(start + 2); triangles.Add(start + 1); triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2); triangles.Add(start);
        }

        static void CreateLevee(string name, Vector3 center, float length, float baseWidth, float topWidth, float height, Transform parent)
        {
            float bx = length * 0.5f, bz = baseWidth * 0.5f, tz = topWidth * 0.5f;
            var mesh = new Mesh { name = name + "_Mesh" };
            mesh.vertices = new[]
            {
                new Vector3(-bx, 0, -bz), new Vector3(-bx, 0, bz), new Vector3(-bx, height, tz), new Vector3(-bx, height, -tz),
                new Vector3(bx, 0, -bz), new Vector3(bx, 0, bz), new Vector3(bx, height, tz), new Vector3(bx, height, -tz)
            };
            mesh.triangles = new[] { 0,1,2, 0,2,3, 4,6,5, 4,7,6, 0,4,5, 0,5,1, 3,2,6, 3,6,7, 0,3,7, 0,7,4, 1,5,6, 1,6,2 };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name); go.transform.SetParent(parent); go.transform.position = center;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>(); Paint(go, "mud");
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        static Transform RoomPlaque(string name, string label, Vector3 position, Vector3 plateSize, Quaternion rotation, Transform parent)
        {
            var plate = VisualBox(name + "_Plate", position, plateSize, "steel", parent);
            plate.rotation = rotation;
            var go = new GameObject(name + "_Text"); go.transform.SetParent(plate, false);
            go.transform.localPosition = new Vector3(0f, 0f, plateSize.z * 0.5f + 0.006f);
            go.transform.localRotation = Quaternion.identity;
            var text = go.AddComponent<TextMeshPro>(); text.font = demoFont; text.text = label;
            text.fontSize = 14f; text.color = new Color(0.78f, 0.75f, 0.64f, 1f);
            text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(plateSize.x * 0.94f, plateSize.y * 0.80f);
            text.margin = Vector4.zero;
            return plate;
        }

        static Transform VisualBox(string name, Vector3 center, Vector3 size, string mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent);
            go.transform.position = center; go.transform.localScale = size; Paint(go, mat);
            var collider = go.GetComponent<Collider>(); if (collider) Object.DestroyImmediate(collider);
            return go.transform;
        }

        static Transform VisualSphere(string name, Vector3 center, Vector3 size, string mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = name; go.transform.SetParent(parent);
            go.transform.position = center; go.transform.localScale = size; Paint(go, mat);
            var collider = go.GetComponent<Collider>(); if (collider) Object.DestroyImmediate(collider);
            return go.transform;
        }

        static Transform VisualCylinder(string name, Vector3 a, Vector3 b, float radius, string mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = name; go.transform.SetParent(parent);
            go.transform.position = (a + b) * 0.5f;
            go.transform.up = b == a ? Vector3.up : (b - a).normalized;
            go.transform.localScale = new Vector3(radius * 2f, Mathf.Max(0.001f, (b - a).magnitude * 0.5f), radius * 2f);
            Paint(go, mat);
            var collider = go.GetComponent<Collider>(); if (collider) Object.DestroyImmediate(collider);
            return go.transform;
        }

        static void LocalBox(string name, Transform parent, Vector3 position, Vector3 size, string mat, Quaternion rotation = default)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = size;
            if (rotation != default) go.transform.localRotation = rotation;
            Paint(go, mat); var collider = go.GetComponent<Collider>(); if (collider) Object.DestroyImmediate(collider);
        }

        static void LocalCylinder(string name, Transform parent, Vector3 position, float radius, float length, Quaternion rotation, string mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localRotation = rotation;
            go.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
            Paint(go, mat); var collider = go.GetComponent<Collider>(); if (collider) Object.DestroyImmediate(collider);
        }

        static void HazardLight(Transform parent, Vector3 local)
        {
            var go = new GameObject("Hazard"); go.transform.SetParent(parent, false); go.transform.localPosition = local;
            var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.55f, 0.15f); l.range = 8; l.intensity = 1600;
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
                if (m == null) { m = new Material(Shader.Find("HDRP/Lit")); AssetDatabase.CreateAsset(m, path); }
                // An abandoned pumping station at night: wet, stained, almost colourless. Nothing bright except paper and the tea.
                (Color c, string tex, float smooth) = key switch
                {
                    "concrete" => (new Color(0.13f, 0.135f, 0.13f), "WetConcrete", 0.62f),
                    "paint" => (new Color(0.20f, 0.185f, 0.15f), "GovBeigePeeling", 0.2f),      // government beige, 20 years of damp
                    "dado" => (new Color(0.055f, 0.06f, 0.055f), "WetConcrete", 0.35f),         // dark oil-paint band on the lower wall
                    "rust" => (new Color(0.20f, 0.12f, 0.08f), "Rust", 0.25f),
                    "steel" => (new Color(0.10f, 0.105f, 0.11f), "Rust", 0.45f),
                    "dark" => (new Color(0.03f, 0.03f, 0.03f), (string)null, 0.35f),
                    "wood" => (new Color(0.14f, 0.10f, 0.07f), "OldWood", 0.2f),
                    "tea" => (new Color(0.60f, 0.18f, 0.07f), (string)null, 0.9f),                      // the only saturated red
                    "paper" => (new Color(0.50f, 0.47f, 0.38f), (string)null, 0.1f),
                    "water" => (new Color(0.02f, 0.035f, 0.03f), "DarkWater", 0.96f),
                    "mud" => (new Color(0.16f, 0.14f, 0.11f), "CrackedMud", 0.1f),
                    "reed" => (new Color(0.24f, 0.23f, 0.16f), "Reed", 0.15f),
                    "glass" => (new Color(0.045f, 0.065f, 0.07f), (string)null, 0.88f),
                    "rubber" => (new Color(0.025f, 0.028f, 0.03f), (string)null, 0.30f),
                    "white" => (new Color(0.48f, 0.48f, 0.44f), (string)null, 0.38f),
                    "blue" => (new Color(0.035f, 0.09f, 0.24f), (string)null, 0.45f),
                    "red" => (new Color(0.28f, 0.025f, 0.015f), (string)null, 0.38f),
                    "amber" => (new Color(0.55f, 0.25f, 0.035f), (string)null, 0.35f),
                    _ => (Color.gray, (string)null, 0.2f)
                };
                var t = tex == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Art/Generated/T_{tex}.png");
                if (t != null)
                {
                    m.SetTexture("_BaseColorMap", t);
                    m.SetTextureScale("_BaseColorMap", new Vector2(2.5f, 2.5f));
                    c = c / 0.45f; c.a = 1f;   // textures are mid-grey already: the tint only pulls them down to night levels
                    var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Root}/Art/Generated/N_{tex}.png");
                    if (normal)
                    {
                        m.SetTexture("_NormalMap", normal);
                        m.SetTextureScale("_NormalMap", new Vector2(2.5f, 2.5f));
                        m.SetFloat("_NormalScale", 0.65f);
                        m.EnableKeyword("_NORMALMAP");
                    }
                }
                m.SetColor("_BaseColor", c);
                m.SetFloat("_Smoothness", smooth);
                if (key == "blue" || key == "red" || key == "amber")
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissiveColor", c * (key == "amber" ? 2.5f : 1.5f));
                }
                EditorUtility.SetDirty(m);
                mats[key] = m;
            }
            go.GetComponent<Renderer>().sharedMaterial = m;
        }

        // ---------- doors ----------

        /// <summary>Wall pieces around a 1.1 x 2.1 m doorway in a 1.8 m corridor. alongX = the corridor runs along X.</summary>
        static void DoorFrame(string n, Vector3 floorCenter, bool alongX, Transform parent)
        {
            const float h = 2.6f;
            Vector3 side = alongX ? Vector3.forward : Vector3.right;
            Vector3 S(float across, float up) => alongX ? new Vector3(0.1f, up, across) : new Vector3(across, up, 0.1f);
            Box(n + "_L", floorCenter - side * 0.75f + Vector3.up * h / 2, S(0.4f, h), "paint", EchoMaterial.Concrete, parent);
            Box(n + "_R", floorCenter + side * 0.75f + Vector3.up * h / 2, S(0.4f, h), "paint", EchoMaterial.Concrete, parent);
            Box(n + "_Lintel", floorCenter + Vector3.up * 2.35f, S(1.1f, 0.5f), "paint", EchoMaterial.Concrete, parent);
        }

        /// <summary>A heavy steel door on a hinge. Opens as Ali comes from the front, slams once he is through.</summary>
        static AutoDoor Door(string n, Vector3 hinge, Vector3 leafOffset, Vector3 leafSize, Vector3 passDir, float openAngle, Transform parent)
        {
            var pivot = new GameObject(n).transform; pivot.SetParent(parent); pivot.position = hinge;
            Box(n + "_Leaf", hinge + leafOffset, leafSize, "rust", EchoMaterial.Metal, pivot);
            var a = pivot.gameObject.AddComponent<AudioSource>();
            a.playOnAwake = false; a.spatialBlend = 1f; a.minDistance = 1f; a.maxDistance = 14f; a.rolloffMode = AudioRolloffMode.Linear;
            var d = pivot.gameObject.AddComponent<AutoDoor>();
            Set(d, "passDirection", passDir); Set(d, "openAngle", openAngle); Set(d, "audioSource", a);
            Set(d, "openClip", Clip("door_creak")); Set(d, "slamClip", Clip("door_slam"));
            return d;
        }

        // ---------- station dressing ----------

        static Transform Cyl(string n, Vector3 a, Vector3 b, float radius, string mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = n; go.transform.SetParent(parent);
            go.transform.position = (a + b) / 2f; go.transform.up = (b - a).normalized;
            go.transform.localScale = new Vector3(radius * 2f, (b - a).magnitude / 2f, radius * 2f);
            Paint(go, mat);
            go.AddComponent<EchoSurface>(); SetSurface(go, EchoMaterial.Metal);
            return go.transform;
        }

        /// <summary>What makes it a pumping station and not a hallway: pipes, valves, gauges, cabinets, wet floor, grime.</summary>
        static void DressStation()
        {
            var d = new GameObject("DRESSING_Station").transform; d.SetParent(world);
            // Corridor, first leg (x -0.9..0.9, z 1..15): pipe runs under the ceiling
            Cyl("Pipe_Main", new Vector3(0.70f, 2.22f, 1.15f), new Vector3(0.70f, 2.22f, 13.1f), 0.10f, "rust", d);
            Cyl("Pipe_Thin", new Vector3(0.46f, 2.34f, 1.15f), new Vector3(0.46f, 2.34f, 14.9f), 0.045f, "steel", d);
            Cyl("Pipe_Left", new Vector3(-0.76f, 2.18f, 1.15f), new Vector3(-0.76f, 2.18f, 14.9f), 0.07f, "rust", d);
            for (int i = 0; i < 5; i++)   // pipe clamps
                Box("Clamp" + i, new Vector3(0.70f, 2.33f, 2.5f + i * 2.6f), new Vector3(0.28f, 0.25f, 0.06f), "steel", EchoMaterial.Metal, d);
            // Risers with valve wheels on the right wall
            foreach (float z in new[] { 3.6f, 8.8f })
            {
                Cyl("Riser", new Vector3(0.78f, 0, z), new Vector3(0.78f, 2.22f, z), 0.07f, "rust", d);
                Cyl("ValveWheel", new Vector3(0.66f, 1.15f, z), new Vector3(0.69f, 1.15f, z), 0.17f, "dark", d);
                Cyl("ValveStem", new Vector3(0.62f, 1.15f, z), new Vector3(0.78f, 1.15f, z), 0.02f, "steel", d);
            }
            // Gauge board
            Box("GaugeBoard", new Vector3(0.86f, 1.5f, 11f), new Vector3(0.05f, 0.5f, 0.9f), "steel", EchoMaterial.Metal, d);
            for (int i = -1; i <= 1; i++)
                Cyl("Gauge", new Vector3(0.83f, 1.52f, 11f + i * 0.28f), new Vector3(0.815f, 1.52f, 11f + i * 0.28f), 0.10f, "paper", d);
            // Electrical cabinets: they also narrow the corridor
            Box("Cabinet_A", new Vector3(-0.72f, 0.9f, 12.4f), new Vector3(0.32f, 1.8f, 0.7f), "steel", EchoMaterial.Metal, d);
            Box("Cabinet_B", new Vector3(-0.74f, 0.75f, 2.2f), new Vector3(0.28f, 1.5f, 0.6f), "steel", EchoMaterial.Metal, d);
            Cyl("Conduit", new Vector3(-0.72f, 1.8f, 12.4f), new Vector3(-0.72f, 2.5f, 12.4f), 0.03f, "dark", d);
            // Dark oil-paint band on the lower walls
            Box("Dado_L1", new Vector3(-0.893f, 0.6f, 3f), new Vector3(0.02f, 1.2f, 4f), "dado", EchoMaterial.Concrete, d);
            Box("Dado_L2", new Vector3(-0.893f, 0.6f, 11.5f), new Vector3(0.02f, 1.2f, 7f), "dado", EchoMaterial.Concrete, d);
            Box("Dado_R1", new Vector3(0.893f, 0.6f, 7.1f), new Vector3(0.02f, 1.2f, 12.2f), "dado", EchoMaterial.Concrete, d);
            Box("Dado_Far2a", new Vector3(1.925f, 0.6f, 14.993f), new Vector3(2.05f, 1.2f, 0.02f), "dado", EchoMaterial.Concrete, d);
            Box("Dado_Far2b", new Vector3(6.025f, 0.6f, 14.993f), new Vector3(3.95f, 1.2f, 0.02f), "dado", EchoMaterial.Concrete, d);
            Box("Dado_Near2", new Vector3(4.45f, 0.6f, 13.207f), new Vector3(7.1f, 1.2f, 0.02f), "dado", EchoMaterial.Concrete, d);
            // Floor: drain channel and standing water
            Box("DrainGrate", new Vector3(0, 0.004f, 8f), new Vector3(0.24f, 0.012f, 13.6f), "steel", EchoMaterial.Metal, d);
            foreach (var (x, z, sx, sz) in new[] { (0.35f, 6.4f, 0.9f, 1.5f), (-0.3f, 11.6f, 0.8f, 1.9f), (4.2f, 14.2f, 2.2f, 1.1f) })
            {
                var w = Box("Puddle", new Vector3(x, 0.003f, z), new Vector3(sx, 0.008f, sz), "water", EchoMaterial.Water, d);
                Object.DestroyImmediate(w.GetComponent<Collider>());
            }
            // Second leg: pipes turn the corner with you
            Cyl("Pipe_Main2", new Vector3(0.9f, 2.22f, 14.8f), new Vector3(8.0f, 2.22f, 14.8f), 0.10f, "rust", d);
            Cyl("Pipe_Elbow", new Vector3(0.70f, 2.22f, 13.1f), new Vector3(0.9f, 2.22f, 14.8f), 0.10f, "rust", d);
            Box("CableTray", new Vector3(4.4f, 2.38f, 13.45f), new Vector3(7f, 0.04f, 0.3f), "steel", EchoMaterial.Metal, d);
            // Storeroom: barrels, shelves, sacks
            foreach (var (x, z) in new[] { (-1.5f, -6.0f), (-0.9f, -6.1f), (-1.55f, -5.3f) })
                Cyl("Barrel", new Vector3(x, 0, z), new Vector3(x, 0.9f, z), 0.29f, "rust", d);
            Box("Sacks", new Vector3(1.3f, 0.25f, -2.2f), new Vector3(1.0f, 0.5f, 0.6f), "mud", EchoMaterial.Concrete, d);
            Cyl("Store_Pipe", new Vector3(-1.9f, 2.3f, -6.4f), new Vector3(-1.9f, 2.3f, -1.6f), 0.08f, "rust", d);
        }

        // ---------- systems ----------

        static LivingTape SetupTape(GameObject recorder)
        {
            var src = recorder.AddComponent<AudioSource>();
            src.spatialBlend = 1; src.volume = 0.08f; src.maxDistance = 12; src.rolloffMode = AudioRolloffMode.Linear;
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
            g.transform.position = new Vector3(-1.5f, 1, -7.5f);
            var s = g.AddComponent<AudioSource>();
            s.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GenAudio + "generator_hum_loop.ogg");
            s.loop = true; s.playOnAwake = true; s.spatialBlend = 1f; s.volume = 0.1f; s.minDistance = 2f; s.maxDistance = 25f; s.rolloffMode = AudioRolloffMode.Linear;
            var power = g.AddComponent<GeneratorPower>();
            Set(power, "hum", s); Set(power, "dieClip", Clip("generator_die"));

            var d = new GameObject("Dread_Drone (under everything)"); d.transform.SetParent(world);
            var ds = d.AddComponent<AudioSource>();
            ds.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(GenAudio + "dread_drone_loop.ogg");
            ds.loop = true; ds.playOnAwake = true; ds.spatialBlend = 0; ds.volume = 0.02f;
        }

        static void BuildLights()
        {
            var l = new GameObject("LIGHTS").transform; l.SetParent(world);
            void Point(string n, Vector3 p, Color c, float range, float intensity)
            {
                var go = new GameObject(n); go.transform.SetParent(l); go.transform.position = p;
                var li = go.AddComponent<Light>(); li.type = LightType.Point; li.color = c; li.range = range; li.intensity = intensity * 2.5f;   // restrained physical-light starting point
                li.shadows = LightShadows.Soft;
                if (!go.GetComponent<HDAdditionalLightData>()) go.AddComponent<HDAdditionalLightData>();
            }
            Color neon = new Color(1f, 0.95f, 0.8f);      // yellowish white, never green
            Point("Bulb_Storeroom", new Vector3(0.3f, 2.3f, -4f), new Color(1f, 0.78f, 0.53f), 4.2f, 42);
            Point("Neon_Corridor_1", new Vector3(0, 2.35f, 4), neon, 6, 90);
            Point("Neon_Corridor_2", new Vector3(0, 2.35f, 11), neon, 6, 60);
            Point("Neon_Segment2", new Vector3(4.5f, 2.35f, 14.1f), neon, 5, 40);
            Point("Lantern_Glow_PumpHall", new Vector3(19, 1.3f, 12), new Color(1f, 0.7f, 0.35f), 6, 80);
            Point("Workshop_FailedBulb", new Vector3(-2.85f, 2.28f, 6.35f), new Color(1f, 0.70f, 0.46f), 4.5f, 34);
            Point("ControlRoom_Fluorescent", new Vector3(3.45f, 2.25f, 16.45f), new Color(0.74f, 0.82f, 0.77f), 4.2f, 28);
            Point("PumpHall_LowLamp", new Vector3(17.4f, 2.65f, 14.7f), new Color(0.95f, 0.63f, 0.34f), 5.2f, 34);

            var moonGo = new GameObject("Moon_Fill"); moonGo.transform.SetParent(l);
            moonGo.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
            var moon = moonGo.AddComponent<Light>(); moon.type = LightType.Directional;
            moon.color = new Color(0.38f, 0.48f, 0.62f); moon.intensity = 0.18f; moon.shadows = LightShadows.None;
            if (!moonGo.GetComponent<HDAdditionalLightData>()) moonGo.AddComponent<HDAdditionalLightData>();
        }

        static void BuildVolume()
        {
            var go = new GameObject("HDRP_Volume (fog + fixed exposure)"); go.transform.SetParent(world);
            var v = go.AddComponent<Volume>(); v.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var fog = profile.Add<Fog>(true);
            fog.enabled.Override(true);
            fog.meanFreePath.Override(18f);          // lighter interior haze; keeps near walls readable
            fog.enableVolumetricFog.Override(true);
            // Black night sky: no daylight/ambient leaking into the station.
            var ve = profile.Add<VisualEnvironment>(true);
            ve.skyType.Override(3);                 // 3 = Gradient sky
            var sky = profile.Add<GradientSky>(true);
            sky.top.Override(Color.black); sky.middle.Override(new Color(0.004f, 0.005f, 0.006f)); sky.bottom.Override(Color.black);
            var exp = profile.Add<Exposure>(true);
            exp.mode.Override(ExposureMode.Fixed);
            exp.fixedExposure.Override(4f);         // underexpose one stop; the flashlight retains a readable beam.
            const string profilePath = Root + "/Scenes/Demo_Story_Volume.asset";
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath) != null) AssetDatabase.DeleteAsset(profilePath);
            AssetDatabase.CreateAsset(profile, profilePath);
            v.sharedProfile = profile;
        }

        static AudioClip Clip(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>(GenAudio + n + ".ogg");

        static GameObject SoundObj(string n, Vector3 p, string clip, float vol, bool loop)
        {
            var go = new GameObject(n); go.transform.SetParent(world); go.transform.position = p;
            var a = go.AddComponent<AudioSource>();
            a.clip = Clip(clip); a.playOnAwake = true; a.loop = loop; a.volume = vol;
            a.spatialBlend = 1f; a.minDistance = 1.5f; a.maxDistance = 16f; a.rolloffMode = AudioRolloffMode.Linear;
            go.SetActive(false);
            return go;
        }

        static GameObject Figure(string n, Vector3 feet, float height)
        {
            var f = Box(n, feet + Vector3.up * height / 2, new Vector3(0.45f, height, 0.3f), "dark", EchoMaterial.Body, world).gameObject;
            f.SetActive(false);
            return f;
        }

        static GameObject Vanishing(GameObject figure, float distance)
        {
            Set(figure.AddComponent<VanishOnApproach>(), "distance", distance);
            return figure;
        }

        static GameObject BuildBehindYou()
        {
            var go = new GameObject("A07_BehindYou"); go.transform.SetParent(world);
            go.transform.position = new Vector3(0, 1.2f, 11.5f);
            var bc = go.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(1.8f, 2.4f, 0.4f);
            var fig = Figure("BehindYou_Figure", new Vector3(0, 0, 11.5f), 2.05f);
            fig.transform.SetParent(go.transform, true);
            Object.DestroyImmediate(fig.GetComponent<Collider>());
            var a = fig.AddComponent<AudioSource>();
            a.clip = Clip("whisper_texture_loop"); a.loop = true; a.playOnAwake = true; a.volume = 0.5f;
            a.spatialBlend = 1f; a.minDistance = 0.5f; a.maxDistance = 5f; a.rolloffMode = AudioRolloffMode.Linear;
            Set(go.AddComponent<BehindYou>(), "figure", fig);
            go.SetActive(false);
            return go;
        }

        /// <summary>Each loop changes one thing. Silent first loops, then it gets worse (P.T.-style escalation).</summary>
        static void WireAnomalies(AnomalyDirector director)
        {
            GameObject F(string n) => GameObject.Find(n);
            var so = new SerializedObject(director);
            var seq = so.FindProperty("sequence");
            void A(int i, string note, GameObject[] on, GameObject[] off, bool halluc = false)
            {
                var e = seq.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("note").stringValue = note;
                var pOn = e.FindPropertyRelative("enableOnStep"); pOn.arraySize = on.Length;
                for (int k = 0; k < on.Length; k++) pOn.GetArrayElementAtIndex(k).objectReferenceValue = on[k];
                var pOff = e.FindPropertyRelative("disableOnStep"); pOff.arraySize = off.Length;
                for (int k = 0; k < off.Length; k++) pOff.GetArrayElementAtIndex(k).objectReferenceValue = off[k];
                e.FindPropertyRelative("isHallucination").boolValue = halluc;
            }
            var none = new GameObject[0];
            A(1, "Calendar gone", none, new[] { F("Calendar_Jan1994") });
            A(2, "Radio static behind the left wall", new[] { SoundObj("A02_RadioStatic", new Vector3(-2.5f, 1.2f, 10), "radio_static", 0.2f, false) }, none);
            // Someone beats the workshop door from inside. It stops the moment you get close.
            var wDoor = Box("WorkshopDoor", new Vector3(-0.9f, 1.05f, 6.5f), new Vector3(0.06f, 2.1f, 1.1f), "rust", EchoMaterial.Metal, world);
            var bang = SoundObj("A03_DoorBang", new Vector3(-0.9f, 1.2f, 6.5f), "door_bang_loop", 0.7f, true);
            bang.GetComponent<AudioSource>().playOnAwake = false;
            var bangA = bang.AddComponent<ApproachSound>();
            Set(bangA, "startDistance", 7f); Set(bangA, "stopDistance", 1.7f); Set(bangA, "shake", wDoor);
            A(3, "Far neon dead. The workshop door is beaten from inside", new[] { bang }, new[] { F("Neon_Corridor_2"), F("Neon_Segment2") });
            A(4, "Three knocks on the pipe above you", new[] { SoundObj("A04_Knocks", new Vector3(0, 2.8f, 8), "knock_pipe_x3", 0.6f, false) }, new[] { F("TeaGlass_RED") });
            A(5, "Someone stands at the corner. No echo.",
              new[] { Vanishing(Figure("A05_FigureAtCorner", new Vector3(0, 0, 14.3f), 1.95f), 6.5f), SoundObj("A05_Swell", new Vector3(0, 1.5f, 14), "scare_swell_stinger", 0.4f, false) }, none, true);
            A(6, "Lights out, only the bulb behind you. Metal groans.",
              new[] { SoundObj("A06_Groan", new Vector3(3, 3, 12), "metal_groan", 0.5f, false) }, new[] { F("Neon_Corridor_1"), F("Neon_Corridor_2"), F("Neon_Segment2") });
            var swing = new GameObject("A06b_SwingingLamp"); swing.transform.SetParent(world);
            Set(swing.AddComponent<SwingingLight>(), "target", F("Neon_Corridor_1").transform); swing.SetActive(false);
            A(7, "The lamp swings by itself. Whisper, close", new[] { swing, SoundObj("A06b_Whisper", new Vector3(-1.2f, 1.6f, 6), "whisper_texture_loop", 0.25f, true) }, none);
            A(8, "The figure is halfway down the corridor now",
              new[] { Vanishing(Figure("A07_FigureMid", new Vector3(0.3f, 0, 9.5f), 1.95f), 4f), BuildBehindYou(), SoundObj("A07_Heart", new Vector3(0, 1.5f, 9), "heartbeat", 0.5f, true) }, new[] { F("Neon_Corridor_2") }, true);
            // A phone rings in the empty control room. It stops when you reach it.
            Box("ControlDeskTop", new Vector3(3.5f, 0.72f, 17.6f), new Vector3(1.6f, 0.12f, 0.6f), "wood", EchoMaterial.Concrete, world);
            foreach (float x in new[] { 2.82f, 4.18f })
                foreach (float z in new[] { 17.38f, 17.82f })
                    Box("ControlDeskLeg", new Vector3(x, 0.34f, z), new Vector3(0.10f, 0.68f, 0.10f), "wood", EchoMaterial.Concrete, world);
            Box("Phone_1994", new Vector3(3.5f, 0.86f, 17.6f), new Vector3(0.22f, 0.1f, 0.2f), "dark", EchoMaterial.Tape, world);
            var ring = SoundObj("A08_PhoneRing", new Vector3(3.5f, 0.9f, 17.6f), "phone_ring_loop", 0.6f, true);
            var ringSrc = ring.GetComponent<AudioSource>(); ringSrc.playOnAwake = false; ringSrc.maxDistance = 22f;
            var ringA = ring.AddComponent<ApproachSound>();
            Set(ringA, "startDistance", 16f); Set(ringA, "stopDistance", 1.5f);
            A(9, "Clock gone, dripping. The phone rings in the control room", new[] { ring, SoundObj("A08_Drip", new Vector3(0.5f, 2.4f, 5), "drip", 0.5f, true) }, new[] { F("WallClock_0547") });
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildAmbience()
        {
            var dread = GameObject.Find("Dread_Drone (under everything)");
            var amb = dread.AddComponent<AmbienceDirector>();
            var e = new GameObject("Elsewhere_Source"); e.transform.SetParent(world);
            var es = e.AddComponent<AudioSource>(); es.playOnAwake = false; es.spatialBlend = 1f; es.maxDistance = 20f; es.rolloffMode = AudioRolloffMode.Linear;
            var pts = new List<Transform>();
            foreach (var p in new[] { new Vector3(-3, 1, 12), new Vector3(3, 3.2f, 6), new Vector3(5, 1, 18), new Vector3(-3, 1, -4) })
            { var t = new GameObject("ElsewherePoint").transform; t.SetParent(e.transform); t.position = p; pts.Add(t); }
            var so = new SerializedObject(amb);
            so.FindProperty("dread").objectReferenceValue = dread.GetComponent<AudioSource>();
            so.FindProperty("elsewhere").objectReferenceValue = es;
            var clips = so.FindProperty("elsewhereClips");
            string[] names = { "metal_groan", "distant_splash_thud", "knock_pipe_x3", "drip" };
            clips.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++) clips.GetArrayElementAtIndex(i).objectReferenceValue = Clip(names[i]);
            var ptsP = so.FindProperty("elsewherePoints"); ptsP.arraySize = pts.Count;
            for (int i = 0; i < pts.Count; i++) ptsP.GetArrayElementAtIndex(i).objectReferenceValue = pts[i];
            so.FindProperty("elsewhereInterval").vector2Value = new Vector2(18f, 40f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static AliController BuildAli(Vector3 pos, out Transform cameraPivot, out Flashlight flashlight, out EchoPulse pulse)
        {
            var ali = new GameObject("ALI");
            ali.layer = LayerMask.NameToLayer("Player");
            ali.transform.position = pos;
            var cc = ali.AddComponent<CharacterController>(); cc.height = 1.75f; cc.radius = 0.3f; cc.center = Vector3.up * 0.875f;

            var camGo = new GameObject("Camera"); camGo.tag = "MainCamera";
            camGo.transform.SetParent(ali.transform, false); camGo.transform.localPosition = Vector3.up * 1.6f;
            var cam = camGo.AddComponent<Camera>(); cam.nearClipPlane = 0.045f; cam.fieldOfView = 64;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<Interactor>();

            var lightGo = new GameObject("Flashlight"); lightGo.transform.SetParent(camGo.transform, false);
            lightGo.transform.localPosition = new Vector3(0.18f, -0.15f, 0.2f);
            var spot = lightGo.AddComponent<Light>(); spot.type = LightType.Spot; spot.spotAngle = 36; spot.range = 12; spot.shadows = LightShadows.Soft;
            if (!lightGo.GetComponent<HDAdditionalLightData>()) lightGo.AddComponent<HDAdditionalLightData>();
            var clickSrc = lightGo.AddComponent<AudioSource>(); clickSrc.playOnAwake = false;
            flashlight = lightGo.AddComponent<Flashlight>();
            Set(flashlight, "clickSource", clickSrc);
            Set(flashlight, "clickOn", Clip("flashlight_tap")); Set(flashlight, "clickOff", Clip("cassette_stop"));

            pulse = camGo.AddComponent<EchoPulse>();
            var tapSrc = camGo.AddComponent<AudioSource>(); tapSrc.playOnAwake = false;
            Set(pulse, "tapSource", tapSrc);
            Set(pulse, "tapClip", AssetDatabase.LoadAssetAtPath<AudioClip>(GenAudio + "flashlight_tap.ogg"));
            Set(pulse, "echoLayers", ~(LayerMask.GetMask("Player", "Hallucination")));
            Set(pulse, "rayCount", 640);
            Set(pulse, "cooldown", 0.36f);
            const string pointMatPath = MatDir + "/EchoPoint.mat";
            var pm = AssetDatabase.LoadAssetAtPath<Material>(pointMatPath);
            if (pm == null) { pm = new Material(Shader.Find("HDRP/Unlit")); AssetDatabase.CreateAsset(pm, pointMatPath); }
            pm.SetColor("_UnlitColor", new Color(0.62f, 0.93f, 0.76f));
            Set(pulse, "pointMaterial", pm);

            var stepSrc = ali.AddComponent<AudioSource>(); stepSrc.playOnAwake = false; stepSrc.spatialBlend = 0;
            var ctrl = ali.AddComponent<AliController>();
            Set(ctrl, "cameraPivot", camGo.transform);
            Set(ctrl, "footstepSource", stepSrc);
            var steps = new SerializedObject(ctrl);
            var arr = steps.FindProperty("footstepClips"); arr.arraySize = 1;
            arr.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(GenAudio + "shoe_concrete_step.ogg");
            steps.ApplyModifiedPropertiesWithoutUndo();
            cameraPivot = camGo.transform;
            return ctrl;
        }

        static Canvas BuildPlayerUI(AliController ali, Transform cameraPivot, Flashlight flashlight, EchoPulse pulse,
            out Subtitles subtitles, out Image eyelids, out Image caughtBlack, out GameObject warningScreen,
            out Toggle reduceFlash, out Button continueButton, out Transform titleText, out Transform wishlistText)
        {
            var canvasGo = new GameObject("PLAYER_UI");
            var canvas = canvasGo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
            var scaler = canvasGo.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var events = new GameObject("UI_EventSystem"); events.transform.SetParent(canvasGo.transform, false);
            events.AddComponent<EventSystem>();
            var uiInput = events.AddComponent<InputSystemUIInputModule>();
            // AddComponent at scene-build time does not serialize Input System UI actions.
            // Assign them explicitly so mouse clicks and keyboard submit work in the built demo.
            uiInput.AssignDefaultActions();

            eyelids = FullScreenImage(canvas.transform, "Wake_Eyelids", new Color(0, 0, 0, 0));
            caughtBlack = FullScreenImage(canvas.transform, "Caught_Blackout", new Color(0, 0, 0, 0));
            var breathDarken = FullScreenImage(canvas.transform, "Breath_Darken", new Color(0, 0, 0, 0));

            var subRoot = new GameObject("Subtitles"); subRoot.transform.SetParent(canvas.transform, false);
            var subRect = subRoot.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.08f, 0.045f); subRect.anchorMax = new Vector2(0.92f, 0.19f);
            subRect.offsetMin = Vector2.zero; subRect.offsetMax = Vector2.zero;
            var subGroup = subRoot.AddComponent<CanvasGroup>(); subGroup.alpha = 0f;
            var label = subRoot.AddComponent<TextMeshProUGUI>();
            label.font = demoFont;
            label.fontSize = 30; label.alignment = TextAlignmentOptions.Bottom;
            label.color = new Color(0.91f, 0.90f, 0.82f, 1f); label.textWrappingMode = TextWrappingModes.Normal;
            label.outlineWidth = 0.16f; label.outlineColor = new Color(0.025f, 0.025f, 0.025f, 1f);
            label.raycastTarget = false;
            subtitles = subRoot.AddComponent<Subtitles>();
            Set(subtitles, "label", label); Set(subtitles, "group", subGroup);

            var heart = cameraPivot.gameObject.AddComponent<AudioSource>();
            heart.clip = Clip("heartbeat"); heart.loop = true; heart.playOnAwake = false; heart.spatialBlend = 0f; heart.volume = 0f;
            var gasp = cameraPivot.gameObject.AddComponent<AudioSource>();
            gasp.playOnAwake = false; gasp.spatialBlend = 0f;
            var lowPass = cameraPivot.gameObject.AddComponent<AudioLowPassFilter>(); lowPass.cutoffFrequency = 22000f;
            var breath = ali.gameObject.AddComponent<BreathHold>();
            Set(breath, "ali", ali); Set(breath, "darken", breathDarken); Set(breath, "heartbeat", heart);
            Set(breath, "gaspSource", gasp); Set(breath, "listenerMuffle", lowPass);

            warningScreen = FullScreenImage(canvas.transform, "Photosensitivity_Warning", new Color(0.018f, 0.02f, 0.019f, 1f)).gameObject;
            var warningTitle = AddUIText(warningScreen.transform, "Warning_Title", "PHOTOSENSITIVITY WARNING", 40, new Vector2(0.12f, 0.70f), new Vector2(0.88f, 0.84f), TextAlignmentOptions.Center);
            warningTitle.color = new Color(0.9f, 0.89f, 0.82f, 1f);
            var en = AddUIText(warningScreen.transform, "Warning_English",
                "Photosensitivity warning\nThis demo uses dark scenes and slow light changes. Press Continue, or wait 8 seconds to begin.",
                25, new Vector2(0.15f, 0.56f), new Vector2(0.85f, 0.69f), TextAlignmentOptions.Center);
            en.color = new Color(0.74f, 0.75f, 0.72f, 1f);
            AddUIText(warningScreen.transform, "Controls",
                "WASD Move   |   Mouse Look   |   Q Flashlight   |   F Echo pulse   |   E Interact   |   Ctrl Crouch   |   Space Hold breath\n\n",
                20, new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.54f), TextAlignmentOptions.Center);
            reduceFlash = MakeToggle(warningScreen.transform, new Vector2(0.28f, 0.29f), new Vector2(0.72f, 0.37f), "Reduce flashing");
            continueButton = MakeButton(warningScreen.transform, new Vector2(0.38f, 0.12f), new Vector2(0.62f, 0.24f), "CONTINUE");
            events.GetComponent<EventSystem>().firstSelectedGameObject = continueButton.gameObject;
            warningScreen.SetActive(false);

            var titleCard = FullScreenImage(canvas.transform, "Ending_Title_Card", new Color(0.012f, 0.014f, 0.014f, 0f));
            titleText = AddUIText(titleCard.transform, "Title_Station", "ECHO STATION\nA STORY DEMO", 56,
                new Vector2(0.12f, 0.38f), new Vector2(0.88f, 0.62f), TextAlignmentOptions.Center).transform;
            titleText.gameObject.SetActive(false);
            var wishlistCard = FullScreenImage(canvas.transform, "Ending_Demo_Card", new Color(0.012f, 0.014f, 0.014f, 0f));
            wishlistText = AddUIText(wishlistCard.transform, "Demo_End", "DEMO COMPLETE\nTHANK YOU FOR PLAYING", 42,
                new Vector2(0.12f, 0.4f), new Vector2(0.88f, 0.6f), TextAlignmentOptions.Center).transform;
            wishlistText.gameObject.SetActive(false);

            return canvas;
        }

        static Image FullScreenImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            return image;
        }

        static TextMeshProUGUI AddUIText(Transform parent, string name, string value, float size, Vector2 min, Vector2 max, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>(); rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var text = go.AddComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = size; text.alignment = alignment;
            text.font = demoFont;
            text.isRightToLeftText = false;
            text.color = new Color(0.88f, 0.87f, 0.8f, 1f); text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false;
            return text;
        }

        static Toggle MakeToggle(Transform parent, Vector2 min, Vector2 max, string caption)
        {
            var go = new GameObject("ReduceFlashing_Toggle"); go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>(); rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var bg = go.AddComponent<Image>(); bg.color = new Color(0.12f, 0.13f, 0.12f, 1f);
            var toggle = go.AddComponent<Toggle>(); toggle.targetGraphic = bg;
            var check = FullScreenImage(go.transform, "Check", new Color(0.68f, 0.78f, 0.63f, 1f));
            var checkRt = (RectTransform)check.transform; checkRt.anchorMin = new Vector2(0.02f, 0.22f); checkRt.anchorMax = new Vector2(0.15f, 0.78f);
            checkRt.offsetMin = Vector2.zero; checkRt.offsetMax = Vector2.zero;
            toggle.graphic = check; toggle.isOn = false;
            toggle.onValueChanged.AddListener(on => check.enabled = on);
            var text = AddUIText(go.transform, "Label", caption, 22, new Vector2(0.18f, 0f), new Vector2(1f, 1f), TextAlignmentOptions.MidlineLeft);
            text.color = Color.white;
            return toggle;
        }

        static Button MakeButton(Transform parent, Vector2 min, Vector2 max, string caption)
        {
            var go = new GameObject("Continue_Button"); go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>(); rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>(); image.color = new Color(0.23f, 0.27f, 0.22f, 1f);
            var button = go.AddComponent<Button>(); button.targetGraphic = image;
            var label = AddUIText(go.transform, "Label", caption, 27, Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
            label.color = Color.white;
            return button;
        }

        // ---------- playable story systems ----------

        static void BuildTapeEntries(LivingTape tape)
        {
            string[] effects = {
                "radio_static", "whisper_texture_loop", "door_bang_loop", "knock_pipe_x3", "distant_splash_thud",
                "metal_groan", "cassette_rewind", "radio_static", "phone_ring_loop", "whisper_texture_loop",
                "generator_die", "radio_static", "metal_groan", "knock_pipe_x3", "distant_splash_thud",
                "whisper_texture_loop", "radio_static", "cassette_rewind", "echo_body"
            };
            int[] distortion = { 0, 4, 1, 3, 5, 4, 2, 3, 5, 1, 4, 5, 2, 3, 4, 1, 5, 2, 4 };
            var so = new SerializedObject(tape);
            var entries = so.FindProperty("entries"); entries.arraySize = effects.Length;
            for (int i = 0; i < effects.Length; i++)
            {
                var e = entries.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("step").intValue = i;
                e.FindPropertyRelative("normal").objectReferenceValue = Clip("tape_hiss_loop");
                e.FindPropertyRelative("distortion").enumValueIndex = distortion[i];
                e.FindPropertyRelative("distortedClip").objectReferenceValue = Clip(effects[i]);
                e.FindPropertyRelative("distortAt").floatValue = 0.24f + (i % 5) * 0.12f;
                e.FindPropertyRelative("isClue").boolValue = new[] { 1, 2, 4, 6, 8, 9, 10, 12, 13, 15, 17 }.Contains(i);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            var source = tape.GetComponent<AudioSource>();
            source.volume = 0.12f; source.loop = true; source.playOnAwake = false;
        }

        static void BuildStationPuzzles(LivingTape tape, AliController ali, Transform cameraPivot,
            out ClockPuzzle clockPuzzle, out KnockRhythmPuzzle knockPuzzle,
            out CassetteFlipPuzzle cassettePuzzle, out MuffledSearchPuzzle muffledPuzzle)
        {
            // The stopped wall clock is a physical dial, not a menu. E unlocks it; hold E and move the mouse.
            var clock = GameObject.Find("WallClock_0547").transform;
            Transform minute = Hand("ClockMinuteHand", clock, new Vector3(0f, 0.015f, 0.027f), new Vector3(0.012f, 0.25f, 0.02f));
            Transform hour = Hand("ClockHourHand", clock, new Vector3(0f, 0.01f, 0.03f), new Vector3(0.018f, 0.17f, 0.024f));
            var tick = clock.gameObject.AddComponent<AudioSource>(); tick.playOnAwake = false; tick.spatialBlend = 0f;
            tick.clip = Clip("cassette_stop"); tick.volume = 0.25f;
            clockPuzzle = clock.gameObject.AddComponent<ClockPuzzle>();
            Set(clockPuzzle, "minuteHand", minute); Set(clockPuzzle, "hourHand", hour); Set(clockPuzzle, "tick", tick);
            Set(clockPuzzle, "cam", cameraPivot);

            // Door B answers a measured three-pulse rhythm, so the echo mechanic teaches the route forward.
            var doorB = GameObject.Find("DoorB");
            knockPuzzle = doorB.AddComponent<KnockRhythmPuzzle>();
            Set(knockPuzzle, "cam", cameraPivot);
            Set(knockPuzzle, "answerKnocks", Clip("knock_pipe_x3"));
            var knockSound = doorB.GetComponent<AudioSource>();
            if (!knockSound) knockSound = doorB.AddComponent<AudioSource>();
            knockSound.playOnAwake = false; knockSound.spatialBlend = 1f; knockSound.volume = 0.5f;
            Set(knockPuzzle, "behindDoor", knockSound);

            // The living tape stays the interaction target; it delegates E presses while this mandatory puzzle is active.
            var cassette = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cassette.name = "Cassette_Ejected"; cassette.transform.SetParent(world, true);
            cassette.transform.position = new Vector3(0.96f, 0.6f, 5.08f); cassette.transform.localScale = new Vector3(0.16f, 0.035f, 0.11f);
            Paint(cassette, "dark"); cassette.SetActive(false);
            cassettePuzzle = tape.gameObject.AddComponent<CassetteFlipPuzzle>();
            Set(cassettePuzzle, "cam", cameraPivot); Set(cassettePuzzle, "recorder", tape.GetComponent<AudioSource>());
            Set(cassettePuzzle, "ejectClick", Clip("cassette_stop")); Set(cassettePuzzle, "insertClick", Clip("cassette_play"));
            Set(cassettePuzzle, "sideB", Clip("whisper_texture_loop")); Set(cassettePuzzle, "cassetteOut", cassette);

            // The sign only reveals its speaker after an echo pulse, then E opens it.
            var sign = Box("NoSmoking_SpeakerSign", new Vector3(0.82f, 1.42f, 12.35f), new Vector3(0.035f, 0.48f, 0.38f), "paper", EchoMaterial.Metal, world);
            var hidden = sign.gameObject.AddComponent<AudioSource>(); hidden.clip = Clip("radio_static"); hidden.loop = true;
            hidden.playOnAwake = false; hidden.spatialBlend = 1f; hidden.volume = 0.3f; hidden.maxDistance = 5f;
            muffledPuzzle = sign.gameObject.AddComponent<MuffledSearchPuzzle>();
            Set(muffledPuzzle, "cam", cameraPivot); Set(muffledPuzzle, "sign", sign); Set(muffledPuzzle, "hiddenSpeaker", hidden);

            // A real hiding space beneath the control desk. Crouching is what makes the hiding state valid.
            var hide = new GameObject("Hide_UnderControlDesk"); hide.transform.SetParent(world);
            hide.transform.position = new Vector3(3.5f, 0.34f, 17.6f);
            var volume = hide.AddComponent<BoxCollider>(); volume.isTrigger = true; volume.size = new Vector3(1.25f, 0.62f, 0.42f);
            hide.AddComponent<HidingSpot>();
        }

        static Transform Hand(string name, Transform parent, Vector3 localPosition, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition; go.transform.localScale = scale;
            Paint(go, "dark"); Object.DestroyImmediate(go.GetComponent<Collider>()); return go.transform;
        }

        static void BuildWake(AliController ali, Transform cameraPivot, Flashlight flashlight, EchoPulse pulse, Image eyelids)
        {
            var wakeGo = new GameObject("WakeSequence"); wakeGo.transform.SetParent(world);
            var wake = wakeGo.AddComponent<WakeSequence>();
            Set(wake, "ali", ali); Set(wake, "cameraPivot", cameraPivot); Set(wake, "flashlight", flashlight); Set(wake, "pulse", pulse);
            var bulbGo = GameObject.Find("Bulb_Storeroom"); Set(wake, "bulb", bulbGo ? bulbGo.GetComponent<Light>() : null);
            Set(wake, "generator", GameObject.Find("Generator_Hum (safety)").GetComponent<GeneratorPower>());
            var windowFigure = Figure("Wake_WindowFigure", new Vector3(0.0f, 0f, -0.78f), 1.95f);
            windowFigure.transform.SetParent(world, true);
            Set(wake, "tiltedHeadInDoorWindow", windowFigure);
            SetSurface(windowFigure, EchoMaterial.Body);

            var oneShots = cameraPivot.gameObject.AddComponent<AudioSource>();
            oneShots.playOnAwake = false; oneShots.spatialBlend = 0f; oneShots.volume = 0.85f;
            var recorder = new GameObject("Wake_RecorderAudio"); recorder.transform.SetParent(world);
            recorder.transform.position = new Vector3(0.65f, 0.25f, -3.1f);
            var nearTape = recorder.AddComponent<AudioSource>(); nearTape.playOnAwake = false; nearTape.spatialBlend = 1f;
            nearTape.volume = 0.32f; nearTape.maxDistance = 3f;
            Set(wake, "oneShots", oneShots); Set(wake, "recorder", nearTape);
            Set(wake, "drip", Clip("drip")); Set(wake, "bulbCrackle", Clip("bulb_crackle"));
            Set(wake, "tapeHiss", Clip("tape_hiss_loop")); Set(wake, "breathing", Clip("whisper_texture_loop"));
            // No generated voice is presented as dialogue. The subtitle/story cues work until Arabic voice actors are recorded.
            Set(wake, "tapeStopClick", Clip("cassette_stop")); Set(wake, "knocks", Clip("knock_pipe_x3"));
            Set(wake, "eyelids", eyelids);
        }

        static void BuildStoryAndThreats(AliController ali, Transform cameraPivot, LivingTape tape,
            AnomalyDirector anomalies, LoopCorridor corridor, Subtitles subtitles, Image caughtBlack,
            Transform titleText, Transform wishlistText, ClockPuzzle clockPuzzle, KnockRhythmPuzzle knockPuzzle,
            CassetteFlipPuzzle cassettePuzzle, MuffledSearchPuzzle muffledPuzzle,
            out CarEnding carEnding, out TantalStalker tantal)
        {
            var humGo = GameObject.Find("Generator_Hum (safety)");
            var generator = humGo.GetComponent<GeneratorPower>();
            var neons = new List<Light>();
            foreach (string n in new[] { "Neon_Corridor_1", "Neon_Corridor_2", "Neon_Segment2", "Lantern_Glow_PumpHall" })
            { var g = GameObject.Find(n); if (g && g.GetComponent<Light>()) neons.Add(g.GetComponent<Light>()); }
            var quietBeds = new List<AudioSource>();
            var dread = GameObject.Find("Dread_Drone (under everything)"); if (dread) quietBeds.Add(dread.GetComponent<AudioSource>());
            var elsewhere = GameObject.Find("Elsewhere_Source"); if (elsewhere) quietBeds.Add(elsewhere.GetComponent<AudioSource>());
            SetReferences(generator, "neonLights", neons.ToArray());
            SetReferences(generator, "silencedDuringBlackout", quietBeds.ToArray());

            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/lines.json");
            var lineGo = new GameObject("Story_Lines"); lineGo.transform.SetParent(world);
            var lineTable = lineGo.AddComponent<LineTable>();
            Set(lineTable, "json", json); Set(lineTable, "subtitles", subtitles);

            var demoClock = new GameObject("DemoClock").AddComponent<DemoClock>(); demoClock.transform.SetParent(world);
            Set(demoClock, "corridor", corridor); Set(demoClock, "director", anomalies);

            // Khalaf is an ordinary physical body. The shape is intentionally subdued; his footsteps and pauses carry the fear.
            var killerGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            killerGo.name = "Khalaf_HumanThreat"; killerGo.layer = LayerMask.NameToLayer("Stalker");
            killerGo.transform.SetParent(world); killerGo.transform.position = new Vector3(19f, 0.9f, 16.2f);
            killerGo.transform.localScale = new Vector3(0.38f, 0.9f, 0.38f); Paint(killerGo, "dark");
            var killerSurface = killerGo.AddComponent<EchoSurface>(); Set(killerSurface, "material", (int)EchoMaterial.Body);
            Set(killerSurface, "clip", Clip("echo_body"));
            var agent = killerGo.AddComponent<NavMeshAgent>(); agent.speed = 1.05f; agent.radius = 0.28f; agent.height = 1.8f;
            var killerBreath = killerGo.AddComponent<AudioSource>(); killerBreath.clip = Clip("whisper_texture_loop"); killerBreath.loop = true;
            killerBreath.playOnAwake = false; killerBreath.spatialBlend = 1f; killerBreath.volume = 0.12f; killerBreath.maxDistance = 7f;
            var killerStepsGo = new GameObject("Khalaf_Footsteps"); killerStepsGo.transform.SetParent(killerGo.transform, false);
            var killerSteps = killerStepsGo.AddComponent<AudioSource>(); killerSteps.playOnAwake = false; killerSteps.spatialBlend = 1f; killerSteps.maxDistance = 12f;
            var killer = killerGo.AddComponent<KillerController>();
            Set(killer, "breath", killerBreath); Set(killer, "steps", killerSteps);
            Set(killer, "headphoneClick", Clip("cassette_stop"));
            SetReferences(killer, "bootSteps", new[] { Clip("boot_water_step") });

            var path = new List<Transform>();
            foreach (var point in new[] { new Vector3(15.2f, 0.05f, 13.2f), new Vector3(18.8f, 0.05f, 13.2f),
                         new Vector3(19.2f, 0.05f, 16.8f), new Vector3(16.2f, 0.05f, 17.8f) })
            {
                var t = new GameObject("KhalafPatrolPoint").transform; t.SetParent(world); t.position = point; path.Add(t);
            }
            SetReferences(killer, "path", path.ToArray());

            var surfaceGo = new GameObject("RuntimeNavMesh_PumpHall"); surfaceGo.transform.SetParent(world);
            surfaceGo.layer = LayerMask.NameToLayer("Stalker");
            var nav = surfaceGo.AddComponent<NavMeshSurface>();
            var navSo = new SerializedObject(nav);
            navSo.FindProperty("m_CollectObjects").enumValueIndex = 0;
            // Build the chase surface from gameplay colliders only. Visual-only TMP signs and set dressing
            // must not be fed to the runtime navmesh builder as render meshes.
            navSo.FindProperty("m_UseGeometry").enumValueIndex = 1;
            navSo.FindProperty("m_LayerMask").intValue = ~(LayerMask.GetMask("Player", "Hallucination", "Stalker"));
            navSo.ApplyModifiedPropertiesWithoutUndo();
            nav.BuildNavMesh();

            var checkpoint = new GameObject("PumpHall_Checkpoint").transform; checkpoint.SetParent(world);
            checkpoint.position = new Vector3(12.6f, 0.05f, 14.1f);
            var caughtGo = new GameObject("CaughtSequence"); caughtGo.transform.SetParent(world);
            var caught = caughtGo.AddComponent<CaughtSequence>();
            var sfx = caughtGo.AddComponent<AudioSource>(); sfx.playOnAwake = false; sfx.spatialBlend = 0f;
            var lowPass = cameraPivot.gameObject.GetComponent<AudioLowPassFilter>();
            Set(caught, "killer", killer); Set(caught, "ali", ali); Set(caught, "cameraPivot", cameraPivot);
            Set(caught, "black", caughtBlack); Set(caught, "sfx", sfx); Set(caught, "tapeWind", Clip("cassette_rewind"));
            Set(caught, "muffledThud", Clip("echo_body")); Set(caught, "listenerMuffle", lowPass); Set(caught, "checkpoint", checkpoint);

            // A still figure at the far threshold has no echo; it is the station's first impossible image.
            var threshold = Figure("PumpHall_ThresholdFigure", new Vector3(11.1f, 0f, 14.1f), 1.95f);
            threshold.layer = LayerMask.NameToLayer("Hallucination");
            var thresholdSurface = threshold.GetComponent<EchoSurface>();
            if (thresholdSurface) thresholdSurface.IsHallucination = true;

            // Tantal emerges only outside. It steals ambience one layer at a time and returns it if disabled.
            var wind = SoundObj("Levee_Wind", new Vector3(60f, -0.4f, 15f), "wind_reeds_loop", 0.18f, true);
            wind.SetActive(true);
            var tant = GameObject.CreatePrimitive(PrimitiveType.Capsule); tant.name = "Tantal_Silhouette";
            tant.layer = LayerMask.NameToLayer("Hallucination"); tant.transform.SetParent(world);
            tant.transform.localScale = new Vector3(0.42f, 1.08f, 0.32f); Paint(tant, "dark");
            var tantSurface = tant.AddComponent<EchoSurface>(); tantSurface.IsHallucination = true;
            tantal = tant.AddComponent<TantalStalker>();
            Set(tantal, "cam", cameraPivot); Set(tantal, "target", ali.transform);
            Set(tantal, "body", tant.GetComponent<Renderer>());
            Set(tantal, "startDistance", 30f); Set(tantal, "stepDistance", 4f);
            SetReferences(tantal, "soundsToSteal", new[] { wind.GetComponent<AudioSource>(), ali.GetComponent<AudioSource>() });
            tant.SetActive(false);

            var storyGo = new GameObject("StationStoryDirector"); storyGo.transform.SetParent(world);
            var story = storyGo.AddComponent<StationStoryDirector>();
            var pumpDoor = GameObject.Find("PumpHall_Door").GetComponent<AutoDoor>();
            Set(story, "corridor", corridor); Set(story, "anomalies", anomalies); Set(story, "tape", tape);
            Set(story, "subtitles", subtitles); Set(story, "radioStatic", Clip("radio_static"));
            Set(story, "pipeKnocks", Clip("knock_pipe_x3")); Set(story, "generatorDie", Clip("generator_die"));
            Set(story, "generator", generator); Set(story, "pumpHallDoor", pumpDoor);
            Set(story, "thresholdFigure", threshold); Set(story, "clockPuzzle", clockPuzzle);
            Set(story, "knockPuzzle", knockPuzzle); Set(story, "cassettePuzzle", cassettePuzzle);
            Set(story, "muffledPuzzle", muffledPuzzle); Set(story, "killer", killer);
            Set(story, "caughtSequence", caught); Set(story, "tantal", tantal);
            Set(story, "pumpCheckpoint", checkpoint); Set(story, "demoClock", demoClock);

            StoryTrigger("PumpHall_EntryTrigger", new Vector3(11.35f, 1f, 14.1f), new Vector3(0.8f, 2f, 2.2f), story, 0);
            StoryTrigger("Station_ExitTrigger", new Vector3(42.5f, -0.5f, 14.1f), new Vector3(1.5f, 1.5f, 1.4f), story, 1);

            const float endingCarX = 82f; // the road scenery parent is offset 2 m to align with the culvert
            var carRoot = new GameObject("PoliceCar_Interior"); carRoot.transform.SetParent(world);
            var trigger = carRoot.AddComponent<BoxCollider>(); trigger.isTrigger = true;
            carRoot.transform.position = new Vector3(endingCarX, -0.15f, 16f); trigger.size = new Vector3(3.2f, 2.2f, 5.5f);
            var seat = new GameObject("CarSeatCamera").transform; seat.SetParent(world); seat.position = new Vector3(endingCarX, -0.05f, 16.2f); seat.rotation = Quaternion.Euler(0f, 95f, 0f);
            var window = new GameObject("Khalaf_WindowAudio"); window.transform.SetParent(world); window.transform.position = new Vector3(endingCarX - 1.5f, -0.1f, 15.5f);
            var windowAudio = window.AddComponent<AudioSource>(); windowAudio.playOnAwake = false; windowAudio.spatialBlend = 1f; windowAudio.maxDistance = 9f;
            var carAudio = carRoot.AddComponent<AudioSource>(); carAudio.playOnAwake = false; carAudio.spatialBlend = 0f;
            var khalafAtWindow = GameObject.CreatePrimitive(PrimitiveType.Capsule); khalafAtWindow.name = "Khalaf_AtWindow";
            khalafAtWindow.transform.SetParent(world); khalafAtWindow.transform.position = new Vector3(endingCarX - 1.5f, 0.65f, 15.5f);
            khalafAtWindow.transform.localScale = new Vector3(0.32f, 0.75f, 0.32f); Paint(khalafAtWindow, "dark");
            khalafAtWindow.SetActive(false);
            var tea = GameObject.CreatePrimitive(PrimitiveType.Sphere); tea.name = "Tea_Offered"; tea.transform.SetParent(world);
            tea.transform.position = new Vector3(endingCarX - 1f, 0.25f, 15.7f); tea.transform.localScale = Vector3.one * 0.13f; Paint(tea, "tea");
            tea.SetActive(false);

            var reach = new GameObject("ReachPoliceCarTrigger"); reach.transform.SetParent(world); reach.transform.position = new Vector3(endingCarX - 1f, -0.2f, 16f);
            var reachCollider = reach.AddComponent<BoxCollider>(); reachCollider.isTrigger = true; reachCollider.size = new Vector3(3.5f, 2f, 5f);
            var reachEvent = reach.AddComponent<StoryAreaTrigger>(); Set(reachEvent, "story", story); Set(reachEvent, "eventType", 2);
            carEnding = carRoot.AddComponent<CarEnding>();
            Set(carEnding, "ali", ali); Set(carEnding, "seatCamera", seat); Set(carEnding, "cameraPivot", cameraPivot);
            Set(carEnding, "khalaf", khalafAtWindow); Set(carEnding, "teaGlassOffer", tea);
            Set(carEnding, "car", carAudio); Set(carEnding, "window", windowAudio);
            Set(carEnding, "doorLock", Clip("door_slam")); Set(carEnding, "knocks", Clip("knock_glass_x3"));
            Set(carEnding, "tapeSpin", Clip("cassette_play")); Set(carEnding, "black", caughtBlack);
            Set(carEnding, "titleCard", titleText.parent.GetComponent<Image>());
            Set(carEnding, "wishlistCard", wishlistText.parent.GetComponent<Image>());
            Set(carEnding, "titleText", titleText.GetComponent<TMP_Text>());
            Set(carEnding, "wishlistText", wishlistText.GetComponent<TMP_Text>());
            Set(carEnding, "subtitles", subtitles); Set(carEnding, "tantal", tantal);
        }

        static void StoryTrigger(string name, Vector3 pos, Vector3 size, StationStoryDirector story, int eventType)
        {
            var go = new GameObject(name); go.transform.SetParent(world); go.transform.position = pos;
            var c = go.AddComponent<BoxCollider>(); c.isTrigger = true; c.size = size;
            var trigger = go.AddComponent<StoryAreaTrigger>(); Set(trigger, "story", story); Set(trigger, "eventType", eventType);
        }

        static void SetReferences<T>(Object target, string field, T[] values) where T : Object
        {
            var so = new SerializedObject(target); var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[EchoStation] Missing array '{field}' on {target.GetType().Name}"); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildGameMenus(AliController ali, GameObject warningScreen, Toggle warningReduce, Button continueButton)
        {
            var canvas = warningScreen.transform.parent;
            var pause = MenuPanel(canvas, "Pause_Menu", "PAUSED");
            var options = MenuPanel(canvas, "Options_Menu", "OPTIONS");
            var menu = canvas.gameObject.AddComponent<EchoStation.UI.GameMenus>();
            Set(menu, "warningScreen", warningScreen); Set(menu, "warningReduceFlashing", warningReduce);
            Set(menu, "warningContinue", continueButton); Set(menu, "pauseScreen", pause); Set(menu, "optionsScreen", options); Set(menu, "ali", ali);

            var resume = MakeButton(pause.transform, new Vector2(0.35f, 0.55f), new Vector2(0.65f, 0.66f), "RESUME");
            var open = MakeButton(pause.transform, new Vector2(0.35f, 0.40f), new Vector2(0.65f, 0.51f), "SETTINGS");
            var quit = MakeButton(pause.transform, new Vector2(0.35f, 0.25f), new Vector2(0.65f, 0.36f), "QUIT");
            resume.onClick.AddListener(menu.Resume); open.onClick.AddListener(menu.OpenOptions); quit.onClick.AddListener(menu.Quit);

            var sensitivityLabel = AddUIText(options.transform, "Sensitivity_Label", "MOUSE SENSITIVITY", 24,
                new Vector2(0.25f, 0.65f), new Vector2(0.75f, 0.74f), TextAlignmentOptions.Center);
            var sliderGo = new GameObject("Sensitivity_Slider"); sliderGo.transform.SetParent(options.transform, false);
            var sliderRect = sliderGo.AddComponent<RectTransform>(); sliderRect.anchorMin = new Vector2(0.28f, 0.57f); sliderRect.anchorMax = new Vector2(0.72f, 0.63f); sliderRect.offsetMin = sliderRect.offsetMax = Vector2.zero;
            var sliderBg = sliderGo.AddComponent<Image>(); sliderBg.color = new Color(0.25f, 0.27f, 0.25f, 1f);
            var handleGo = new GameObject("Handle"); handleGo.transform.SetParent(sliderGo.transform, false);
            var handleRect = handleGo.AddComponent<RectTransform>(); handleRect.sizeDelta = new Vector2(28f, 32f);
            var handleImage = handleGo.AddComponent<Image>(); handleImage.color = new Color(0.75f, 0.78f, 0.68f, 1f);
            var slider = sliderGo.AddComponent<Slider>(); slider.targetGraphic = handleImage; slider.handleRect = handleRect; slider.minValue = 0.3f; slider.maxValue = 2f; slider.value = 1f;
            Set(menu, "sensitivity", slider);

            var reduce = MakeToggle(options.transform, new Vector2(0.28f, 0.45f), new Vector2(0.72f, 0.53f), "REDUCE FLASHING");
            var breath = MakeToggle(options.transform, new Vector2(0.28f, 0.35f), new Vector2(0.72f, 0.43f), "TOGGLE BREATH HOLD");
            Set(menu, "reduceFlashing", reduce); Set(menu, "breathToggleMode", breath);
            var en = MakeButton(options.transform, new Vector2(0.33f, 0.22f), new Vector2(0.48f, 0.31f), "ENGLISH");
            var off = MakeButton(options.transform, new Vector2(0.52f, 0.22f), new Vector2(0.67f, 0.31f), "SUBTITLES OFF");
            en.onClick.AddListener(() => SaveSubtitleLanguage("en"));
            off.onClick.AddListener(() => SaveSubtitleLanguage("off"));
            var back = MakeButton(options.transform, new Vector2(0.38f, 0.08f), new Vector2(0.62f, 0.18f), "BACK");
            back.onClick.AddListener(menu.CloseOptions);
            pause.SetActive(false); options.SetActive(false);
        }

        static GameObject MenuPanel(Transform canvas, string name, string title)
        {
            var panel = FullScreenImage(canvas, name, new Color(0.012f, 0.015f, 0.014f, 0.96f)).gameObject;
            AddUIText(panel.transform, "Title", title, 38, new Vector2(0.2f, 0.78f), new Vector2(0.8f, 0.9f), TextAlignmentOptions.Center);
            return panel;
        }

        static void SaveSubtitleLanguage(string value)
        {
            Subtitles.Language = value; PlayerPrefs.SetString("es_subs", value); PlayerPrefs.Save();
        }

        static void ConfigureBuildEntryPoint()
        {
            var scenes = new List<EditorBuildSettingsScene> { BuildSceneEntry(ScenePath, true) };
            if (System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "_EchoStation/Scenes/Demo_Greybox.unity")))
                scenes.Add(BuildSceneEntry(GreyboxScenePath, false));
            EditorBuildSettings.scenes = scenes.ToArray();
            PlayerSettings.productName = "Echo Station — Story Demo";
            PlayerSettings.companyName = "Echo Station";
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
        }

        static EditorBuildSettingsScene BuildSceneEntry(string path, bool enabled)
        {
            string assetGuid = AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrEmpty(assetGuid) && assetGuid != "00000000000000000000000000000000")
            {
                var entry = new EditorBuildSettingsScene(new GUID(assetGuid), enabled);
                entry.path = path;
                return entry;
            }
            return new EditorBuildSettingsScene(path, enabled);
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
                case Vector3 v: p.vector3Value = v; break;
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
