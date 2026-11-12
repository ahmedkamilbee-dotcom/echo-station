using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace ProjectBootstrap
{
    // One-time setup: adds the layers CLAUDE.md requires (Player, Hallucination),
    // forces a full import/compile, saves, and exits with 1 if scripts don't compile.
    // Run: Unity.exe -batchmode -projectPath <path> -executeMethod ProjectBootstrap.ProjectSetup.Run
    public static class ProjectSetup
    {
        static readonly string[] RequiredLayers = { "Player", "Hallucination" };

        public static void Run()
        {
            AddLayers();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            bool failed = EditorUtility.scriptCompilationFailed;
            Debug.Log($"[ProjectSetup] Done. Script compilation failed: {failed}");
            EditorApplication.Exit(failed ? 1 : 0);
        }

        static void AddLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            foreach (var name in RequiredLayers)
            {
                if (LayerMask.NameToLayer(name) >= 0) { Debug.Log($"[ProjectSetup] Layer '{name}' already exists."); continue; }
                bool added = false;
                for (int i = 8; i < layers.arraySize; i++)   // 0-7 are Unity's built-in layers
                {
                    var slot = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(slot.stringValue))
                    {
                        slot.stringValue = name;
                        Debug.Log($"[ProjectSetup] Added layer '{name}' at index {i}.");
                        added = true;
                        break;
                    }
                }
                if (!added) Debug.LogError($"[ProjectSetup] No free layer slot for '{name}'.");
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
