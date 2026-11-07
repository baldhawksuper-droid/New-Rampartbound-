#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Rampartbound.Editor
{
    /// <summary>
    /// Automated Build Pipeline Script for GameCI & WebGL deployment to GitHub Pages.
    /// Exposes BuildScript.BuildWebGL for CI/CD batch mode execution.
    /// </summary>
    public static class BuildScript
    {
        public const string DEFAULT_SCENE_PATH = "Assets/Scenes/RampartboundMainScene.unity";
        public const string WEBGL_BUILD_DIR = "Builds/WebGL/Rampartbound";

        [MenuItem("Rampartbound/Build/1-Click Build WebGL (GitHub Pages)", false, 14)]
        public static void BuildWebGL()
        {
            Debug.Log("[BuildScript] Memulai otomatisasi kompilasi WebGL untuk GitHub Pages...");

            // 1. Eksekusi Setup Scene otomatis jika diperlukan
            try
            {
                RampartboundSceneSetup.SetupArenaScene();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildScript] Catatan setup scene arena: {ex.Message}");
            }

            // 2. Generate Build Info & Version Stamping (Tanggal UTC & Commit Hash)
            string displayVersion = GenerateBuildInfo();

            // 3. Konfigurasi PlayerSettings WebGL
            ConfigureWebGLPlayerSettings();

            // 4. Resolusi Scenes
            string[] scenes = GetScenesForBuild();
            Debug.Log($"[BuildScript] Total Scene terdaftar: {scenes.Length} (Primary: {scenes[0]})");

            // 5. Folder Output
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string outputPath = Path.Combine(projectRoot, WEBGL_BUILD_DIR);
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            Debug.Log($"[BuildScript] Mengeksekusi BuildPlayer ke lokasi: {outputPath}...");
            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                double sizeMb = summary.totalSize / (1024.0 * 1024.0);
                Debug.Log($"[BuildScript] [PASS] Kompilasi WebGL Berhasil! Ukuran: {sizeMb:F2} MB, Durasi: {summary.totalTime.TotalSeconds:F1}s, Versi: {displayVersion}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            else
            {
                Debug.LogError($"[BuildScript] [FAIL] Kompilasi WebGL Gagal! Total Error: {summary.totalErrors}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        public static string GenerateBuildInfo()
        {
            string baseVersion = PlayerSettings.bundleVersion;
            if (string.IsNullOrEmpty(baseVersion)) baseVersion = "1.2.18";

            string dateStamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmm");
            string commitHash = Environment.GetEnvironmentVariable("GITHUB_SHA");
            if (!string.IsNullOrEmpty(commitHash) && commitHash.Length >= 7)
            {
                commitHash = commitHash.Substring(0, 7);
            }
            else
            {
                commitHash = "local";
            }

            string displayVersion = $"v{baseVersion} ({dateStamp} · {commitHash})";

            try
            {
                string resourcesDir = Path.Combine(Application.dataPath, "Resources");
                if (!Directory.Exists(resourcesDir))
                {
                    Directory.CreateDirectory(resourcesDir);
                }

                string jsonContent = $"{{\"version\":\"{baseVersion}\",\"dateStamp\":\"{dateStamp}\",\"commitHash\":\"{commitHash}\",\"displayVersion\":\"{displayVersion}\",\"platform\":\"WebGL\"}}";
                string filePath = Path.Combine(resourcesDir, "build_info.json");
                File.WriteAllText(filePath, jsonContent);
                Debug.Log($"[BuildScript] Berhasil menulis build info: {displayVersion} -> {filePath}");
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildScript] Gagal menulis Resources/build_info.json: {ex.Message}");
            }

            return displayVersion;
        }

        public static void ConfigureWebGLPlayerSettings()
        {
            PlayerSettings.companyName = "BaldHawk Studios";
            PlayerSettings.productName = "Rampartbound: Merge Defense";

            // WebGL Resolution: Portrait 1080x1920 (Letterbox pada desktop browser)
            PlayerSettings.defaultWebScreenWidth = 1080;
            PlayerSettings.defaultWebScreenHeight = 1920;

            // Run in Background diaktifkan agar browser tab tidak membekukan audio/gameplay saat pindah jendela
            PlayerSettings.runInBackground = true;

            // Format Kompresi WebGL:
            // Menggunakan Disabled atau Gzip dengan Decompression Fallback.
            // Disabled direkomendasikan untuk GitHub Pages guna menghindari masalah MIME/Content-Encoding header.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;

            Debug.Log("[BuildScript] Konfigurasi WebGL PlayerSettings: Portrait 1080x1920, RunInBackground=true, Compression=Disabled, DecompressionFallback=true.");
        }

        private static string[] GetScenesForBuild()
        {
            var editorScenes = EditorBuildSettings.scenes;
            if (editorScenes != null && editorScenes.Length > 0)
            {
                var sceneList = new System.Collections.Generic.List<string>();
                foreach (var s in editorScenes)
                {
                    if (s.enabled && !string.IsNullOrEmpty(s.path))
                    {
                        sceneList.Add(s.path);
                    }
                }
                if (sceneList.Count > 0) return sceneList.ToArray();
            }

            return new string[] { DEFAULT_SCENE_PATH };
        }
    }
}

/// <summary>
/// Global namespace wrapper for Unity CLI -executeMethod BuildScript.BuildWebGL
/// </summary>
public static class BuildScript
{
    public static void BuildWebGL()
    {
        Rampartbound.Editor.BuildScript.BuildWebGL();
    }
}
#endif
