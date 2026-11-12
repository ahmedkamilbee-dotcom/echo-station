using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ProjectBootstrap
{
    // Installs the Echo Station packages via the PackageManager Client API, headless-safe.
    // Run: Unity.exe -batchmode -projectPath <path> -executeMethod ProjectBootstrap.PackageInstaller.Install (NO -quit)
    public static class PackageInstaller
    {
        static readonly string[] PackagesToAdd =
        {
            "com.unity.probuilder",
            "com.unity.cinemachine",
            "com.unity.inputsystem",
            "com.unity.ai.navigation",
        };

        const double TimeoutSeconds = 900;
        static AddAndRemoveRequest _request;
        static double _deadline;

        public static void Install()
        {
            Debug.Log($"[PackageInstaller] Adding: {string.Join(", ", PackagesToAdd)}");
            _request = Client.AddAndRemove(packagesToAdd: PackagesToAdd, packagesToRemove: new string[0]);
            _deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (_request == null) return;
            if (!_request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup > _deadline)
                {
                    EditorApplication.update -= Poll;
                    Debug.LogError("[PackageInstaller] Timed out waiting for UPM.");
                    EditorApplication.Exit(2);
                }
                return;
            }
            EditorApplication.update -= Poll;
            if (_request.Status == StatusCode.Success)
            {
                var names = _request.Result.Select(p => $"{p.name}@{p.version}");
                Debug.Log($"[PackageInstaller] Resolved: {string.Join(", ", names)}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[PackageInstaller] Failed: {_request.Error?.message}");
                EditorApplication.Exit(1);
            }
        }
    }
}
