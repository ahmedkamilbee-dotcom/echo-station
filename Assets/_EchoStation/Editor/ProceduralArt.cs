#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EchoStation.EditorTools
{
    /// <summary>
    /// Menu: EchoStation > Generate Textures & Props.
    /// Placeholder art made entirely in code (ours, no licence needed), until real CC0 assets replace it:
    ///  Textures + HDRP/Lit materials: wet concrete, faded government beige with peeling plaster, rust,
    ///  cracked mud, reed, dark water.
    ///  Prefabs from primitives: reed bundle, cassette recorder, tea glass (the red), bench, wall clock,
    ///  calendar (January 1994), control panel with knob, barrel, lantern, iron grate, and a grey low-poly human
    ///  (for the killer/Khalaf until a MakeHuman/Mixamo model replaces it).
    /// </summary>
    public static class ProceduralArt
    {
        const string Dir = "Assets/_EchoStation/Art/Generated";
        const int Size = 1024;

        [MenuItem("EchoStation/Generate Textures and Props", priority = 1)]
        public static void Generate()
        {
            Ensure("Assets/_EchoStation/Art"); Ensure(Dir); Ensure(Dir + "/Prefabs");
            var concrete = Mat("WetConcrete", Tex("WetConcrete", (u, v) => Concrete(u, v)), 0.55f);
            var paint = Mat("GovBeigePeeling", Tex("GovBeigePeeling", (u, v) => Paint(u, v)), 0.2f);
            var rust = Mat("Rust", Tex("Rust", (u, v) => Rust(u, v)), 0.25f);
            Mat("CrackedMud", Tex("CrackedMud", (u, v) => Mud(u, v)), 0.1f);
            var reed = Mat("Reed", Tex("Reed", (u, v) => Reed(u, v)), 0.15f);
            Mat("DarkWater", Tex("DarkWater", (u, v) => new Color(0.03f, 0.06f, 0.05f) * (0.8f + 0.4f * Noise(u, v, 8))), 0.97f);
            var dark = Mat("DarkPlastic", null, 0.35f, new Color(0.07f, 0.07f, 0.07f));
            var tea = Mat("TeaRed", null, 0.9f, new Color(0.60f, 0.16f, 0.06f));      // the only saturated red
            var wood = Mat("OldWood", Tex("OldWood", (u, v) => Wood(u, v)), 0.2f);
            var paper = Mat("Paper", null, 0.1f, new Color(0.78f, 0.74f, 0.62f));
            var grey = Mat("GreyHuman", null, 0.3f, new Color(0.22f, 0.22f, 0.22f));
            var glow = Mat("LanternGlass", null, 0.9f, new Color(1f, 0.55f, 0.2f));

            Prefab("ReedBundle", root =>
            {
                for (int i = 0; i < 14; i++)
                {
                    var c = Part(PrimitiveType.Cylinder, root, reed, new Vector3(Random.Range(-0.15f, 0.15f), 0.9f, Random.Range(-0.15f, 0.15f)), new Vector3(0.03f, 0.9f + Random.value * 0.2f, 0.03f));
                    c.transform.localRotation = Quaternion.Euler(Random.Range(-4f, 4f), 0, Random.Range(-4f, 4f));
                }
                Part(PrimitiveType.Cylinder, root, rust, new Vector3(0, 1.0f, 0), new Vector3(0.36f, 0.02f, 0.36f));   // binding cord
            });
            Prefab("CassetteRecorder", root =>
            {
                Part(PrimitiveType.Cube, root, dark, new Vector3(0, 0.06f, 0), new Vector3(0.32f, 0.12f, 0.2f));
                Part(PrimitiveType.Cube, root, grey, new Vector3(0, 0.12f, -0.01f), new Vector3(0.16f, 0.005f, 0.1f));
                foreach (float x in new[] { -0.04f, 0.04f }) Part(PrimitiveType.Cylinder, root, paper, new Vector3(x, 0.125f, -0.01f), new Vector3(0.03f, 0.003f, 0.03f));
                Part(PrimitiveType.Sphere, root, tea, new Vector3(0.13f, 0.1f, -0.1f), Vector3.one * 0.012f);       // red LED
            });
            Prefab("TeaGlass", root =>
            {
                Part(PrimitiveType.Cylinder, root, tea, new Vector3(0, 0.045f, 0), new Vector3(0.05f, 0.045f, 0.05f));
                Part(PrimitiveType.Cylinder, root, paper, new Vector3(0, 0.003f, 0), new Vector3(0.1f, 0.003f, 0.1f));  // saucer
            });
            Prefab("Bench", root =>
            {
                Part(PrimitiveType.Cube, root, wood, new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.05f, 0.4f));
                foreach (float x in new[] { -0.7f, 0.7f }) Part(PrimitiveType.Cube, root, rust, new Vector3(x, 0.22f, 0), new Vector3(0.05f, 0.45f, 0.35f));
            });
            Prefab("WallClock_0547", root =>
            {
                var face = Part(PrimitiveType.Cylinder, root, paper, Vector3.zero, new Vector3(0.36f, 0.02f, 0.36f));
                face.transform.localRotation = Quaternion.Euler(90, 0, 0);
                var hour = new GameObject("HourHand"); hour.transform.SetParent(root.transform, false); hour.transform.localPosition = new Vector3(0, 0, -0.025f);
                Part(PrimitiveType.Cube, hour, dark, new Vector3(0, 0.05f, 0), new Vector3(0.012f, 0.1f, 0.005f));
                hour.transform.localRotation = Quaternion.Euler(0, 0, -(5 * 30 + 47 * 0.5f));
                var min = new GameObject("MinuteHand"); min.transform.SetParent(root.transform, false); min.transform.localPosition = new Vector3(0, 0, -0.03f);
                Part(PrimitiveType.Cube, min, dark, new Vector3(0, 0.07f, 0), new Vector3(0.008f, 0.14f, 0.005f));
                min.transform.localRotation = Quaternion.Euler(0, 0, -47 * 6f);
            });
            Prefab("Calendar_Jan1994", root => Part(PrimitiveType.Cube, root, paper, Vector3.zero, new Vector3(0.35f, 0.5f, 0.005f)));
            Prefab("ControlPanel", root =>
            {
                Part(PrimitiveType.Cube, root, rust, new Vector3(0, 0.6f, 0), new Vector3(1.2f, 1.2f, 0.25f));
                var knob = Part(PrimitiveType.Cylinder, root, dark, new Vector3(0.3f, 0.8f, -0.14f), new Vector3(0.08f, 0.03f, 0.08f));
                knob.name = "ChannelKnob"; knob.transform.localRotation = Quaternion.Euler(90, 0, 0);
            });
            Prefab("Barrel", root => Part(PrimitiveType.Cylinder, root, rust, new Vector3(0, 0.45f, 0), new Vector3(0.58f, 0.45f, 0.58f)));
            Prefab("Lantern", root =>
            {
                Part(PrimitiveType.Cube, root, rust, new Vector3(0, 0.15f, 0), new Vector3(0.14f, 0.3f, 0.14f));
                Part(PrimitiveType.Cube, root, glow, new Vector3(0, 0.15f, 0), new Vector3(0.1f, 0.18f, 0.1f));
                var l = new GameObject("Light"); l.transform.SetParent(root.transform, false); l.transform.localPosition = new Vector3(0, 0.15f, 0);
                var li = l.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, 0.65f, 0.3f); li.range = 6; li.intensity = 60;
                l.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
            });
            Prefab("IronGrate_2m", root =>
            {
                for (int i = 0; i < 16; i++) Part(PrimitiveType.Cube, root, rust, new Vector3(-0.94f + i * 0.125f, 0, 0), new Vector3(0.02f, 0.03f, 1.6f));
                foreach (float z in new[] { -0.8f, 0.8f }) Part(PrimitiveType.Cube, root, rust, new Vector3(0, 0, z), new Vector3(2f, 0.04f, 0.05f));
            });
            Prefab("GreyHuman_175", root =>
            {
                Part(PrimitiveType.Capsule, root, grey, new Vector3(0, 1.15f, 0), new Vector3(0.42f, 0.38f, 0.26f));      // torso
                Part(PrimitiveType.Sphere, root, grey, new Vector3(0, 1.62f, 0), new Vector3(0.22f, 0.26f, 0.24f));       // head (shemagh later)
                foreach (float x in new[] { -0.11f, 0.11f }) Part(PrimitiveType.Capsule, root, grey, new Vector3(x, 0.45f, 0), new Vector3(0.14f, 0.45f, 0.14f));  // legs (rubber boots)
                foreach (float x in new[] { -0.28f, 0.28f }) Part(PrimitiveType.Capsule, root, grey, new Vector3(x, 1.12f, 0), new Vector3(0.1f, 0.34f, 0.1f));   // arms
            }, keepRootCollider: true);

            AssetDatabase.SaveAssets();
            Debug.Log("[EchoStation] Generated textures, materials and prefabs in " + Dir);
        }

        // ---------- patterns ----------
        static float Noise(float u, float v, float f) => Mathf.PerlinNoise(u * f + 13.1f, v * f + 7.7f);
        static float Fbm(float u, float v, float f) { float s = 0, a = 0.5f; for (int i = 0; i < 5; i++) { s += a * Noise(u, v, f); f *= 2; a *= 0.5f; } return s; }

        static Color Concrete(float u, float v)
        {
            float n = Fbm(u, v, 6); float stain = Mathf.SmoothStep(0.45f, 0.75f, Fbm(u + 3, v, 2));
            return Color.Lerp(new Color(0.34f, 0.34f, 0.32f), new Color(0.16f, 0.17f, 0.16f), stain) * (0.8f + 0.35f * n);
        }
        static Color Paint(float u, float v)
        {
            float peel = Fbm(u, v, 5);
            Color beige = new Color(0.58f, 0.52f, 0.41f), plaster = new Color(0.68f, 0.66f, 0.60f);
            Color c = peel > 0.62f ? plaster : beige;
            c *= 0.85f + 0.25f * Noise(u, v, 40);
            float drip = Mathf.Clamp01(Noise(u * 30, 0, 1) - 0.6f) * (1 - v); // vertical grime streaks
            return Color.Lerp(c, new Color(0.25f, 0.22f, 0.18f), drip * 1.6f);
        }
        static Color Rust(float u, float v)
        {
            float n = Fbm(u, v, 7);
            return Color.Lerp(new Color(0.28f, 0.17f, 0.10f), new Color(0.45f, 0.24f, 0.11f), n) * (0.75f + 0.3f * Noise(u, v, 60));
        }
        static Color Mud(float u, float v)
        {
            // Cheap Voronoi-ish cracks.
            float cell = Mathf.Abs(Mathf.Sin(u * 22 + Fbm(u, v, 3) * 6) * Mathf.Sin(v * 22 + Fbm(v, u, 3) * 6));
            float crack = cell < 0.06f ? 0.35f : 1f;
            return new Color(0.30f, 0.26f, 0.20f) * crack * (0.85f + 0.25f * Fbm(u, v, 9));
        }
        static Color Reed(float u, float v) => Color.Lerp(new Color(0.42f, 0.40f, 0.26f), new Color(0.28f, 0.27f, 0.18f), Noise(u * 40, v * 2, 1)) * (0.85f + 0.2f * Noise(u, v, 30));
        static Color Wood(float u, float v) => new Color(0.30f, 0.21f, 0.13f) * (0.7f + 0.3f * Mathf.Sin((u + Fbm(u, v, 3) * 0.3f) * 60f) * 0.5f + 0.3f * Noise(u, v, 20));

        // ---------- assets ----------
        delegate Color Pattern(float u, float v);

        static Texture2D Tex(string name, Pattern p)
        {
            string path = $"{Dir}/T_{name}.png";
            var t = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
                { var c = p(x / (float)Size, y / (float)Size); c.a = 1; px[y * Size + x] = c; }
            t.SetPixels(px); t.Apply();
            System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            ImportTexture(path, false);
            WriteNormal(name, px);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void WriteNormal(string name, Color[] source)
        {
            var normal = new Texture2D(Size, Size, TextureFormat.RGBA32, true, true);
            var pixels = new Color[Size * Size];
            int Wrap(int value) => (value + Size) % Size;
            float HeightAt(int x, int y)
            {
                var c = source[Wrap(y) * Size + Wrap(x)];
                return c.r * 0.30f + c.g * 0.59f + c.b * 0.11f;
            }
            const float bumpStrength = 20f;
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                float dx = (HeightAt(x + 1, y) - HeightAt(x - 1, y)) * bumpStrength;
                float dy = (HeightAt(x, y + 1) - HeightAt(x, y - 1)) * bumpStrength;
                Vector3 n = new Vector3(-dx, -dy, 1f).normalized;
                pixels[y * Size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
            normal.SetPixels(pixels); normal.Apply();
            string path = $"{Dir}/N_{name}.png";
            System.IO.File.WriteAllBytes(path, normal.EncodeToPNG());
            Object.DestroyImmediate(normal);
            ImportTexture(path, true);
        }

        static void ImportTexture(string path, bool normalMap)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.maxTextureSize = Size;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (!normalMap) importer.sRGBTexture = true;
            importer.SaveAndReimport();
        }

        static Material Mat(string name, Texture2D tex, float smooth, Color? tint = null)
        {
            string path = $"{Dir}/M_{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("HDRP/Lit")); AssetDatabase.CreateAsset(m, path); }
            if (tex) m.SetTexture("_BaseColorMap", tex);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/N_{name}.png");
            if (normal)
            {
                m.SetTexture("_NormalMap", normal);
                m.SetFloat("_NormalScale", 0.65f);
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetColor("_BaseColor", tint ?? Color.white);
            m.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Prefab(string name, System.Action<GameObject> build, bool keepRootCollider = false)
        {
            var root = new GameObject(name);
            build(root);
            if (keepRootCollider) { var c = root.AddComponent<CapsuleCollider>(); c.height = 1.75f; c.radius = 0.3f; c.center = Vector3.up * 0.875f; }
            PrefabUtility.SaveAsPrefabAsset(root, $"{Dir}/Prefabs/{name}.prefab");
            Object.DestroyImmediate(root);
        }

        static GameObject Part(PrimitiveType t, GameObject parent, Material m, Vector3 pos, Vector3 scale)
        {
            var g = GameObject.CreatePrimitive(t);
            g.transform.SetParent(parent.transform, false);
            g.transform.localPosition = pos; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }

        static void Ensure(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            Ensure(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
#endif
