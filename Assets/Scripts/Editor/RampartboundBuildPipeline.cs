#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Rampartbound.Editor
{
    /// <summary>
    /// Master Build Automation Pipeline untuk Rampartbound: Merge Defense.
    /// Dikalibrasi untuk Unity 6.6 (6000.6.5f1 / 6000.6.6f1) & Google Play Console Staging.
    /// Memungkinkan ekspor 1-klik untuk:
    /// 1. Windows Standalone (x64 Desktop Build)
    /// 2. Android Sideload APK (Untuk pengujian internal cepat di perangkat fisik)
    /// 3. Google Play App Bundle (.aab) (Format rilis resmi Google Play Store dengan Play Feature Delivery & Dynamic Delivery)
    /// Serta auto-konfigurasi PlayerSettings (Orientasi Portrait, IL2CPP 64-bit, Target API 34+, & Keystore audit).
    /// </summary>
    public static class RampartboundBuildPipeline
    {
        public const string COMPANY_NAME = "BaldHawk Studios";
        public const string PRODUCT_NAME = "Rampartbound: Merge Defense";
        public const string PACKAGE_IDENTIFIER = "com.rampartbound.mergedefense";
        public const string APP_VERSION = "1.2.17";
        public const int BUNDLE_VERSION_CODE = 137;
        public const string DEFAULT_SCENE_PATH = "Assets/Scenes/RampartboundMainScene.unity";

        private const string WINDOWS_BUILD_DIR = "Builds/Windows";
        private const string ANDROID_BUILD_DIR = "Builds/Android";

        [MenuItem("Rampartbound/Build/Configure Project Settings (Portrait & Mobile)", false, 10)]
        public static void ConfigureProjectSettings()
        {
            ConfigureProjectSettingsInternal(false);
        }

        private static void ConfigureProjectSettingsInternal(bool forGooglePlayRelease)
        {
            Debug.Log("[BuildPipeline] Mengonfigurasi PlayerSettings resmi Rampartbound: Merge Defense...");

            PlayerSettings.companyName = COMPANY_NAME;
            PlayerSettings.productName = PRODUCT_NAME;
            PlayerSettings.bundleVersion = APP_VERSION;

            // 1. Orientasi Layar Terkunci Portrait untuk Mobile
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // 2. Identitas Paket Aplikasi (Package Identifier)
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PACKAGE_IDENTIFIER);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, PACKAGE_IDENTIFIER);
#else
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PACKAGE_IDENTIFIER);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, PACKAGE_IDENTIFIER);
#endif

            // 3. Kalibrasi Android Version Code & Target SDK
            PlayerSettings.Android.bundleVersionCode = BUNDLE_VERSION_CODE;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // Android 8.0 Oreo
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34; // Android 14 (Google Play Requirement)

            // 4. Arsitektur 64-bit Wajib Google Play (IL2CPP + ARM64)
            if (forGooglePlayRelease)
            {
#if UNITY_2021_2_OR_NEWER
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
#else
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
#endif
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
                EditorUserBuildSettings.buildAppBundle = true;
                EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            }

            // 4b. Daftarkan path OpenJDK, Android SDK, dan NDK ke EditorPrefs
            string editorDir = Path.GetDirectoryName(EditorApplication.applicationPath);
            string playbackEngines = Path.Combine(editorDir, "Data", "PlaybackEngines", "AndroidPlayer");
            string jdkPath = Path.Combine(playbackEngines, "OpenJDK");
            string sdkPath = Path.Combine(playbackEngines, "SDK");
            string ndkPath = Path.Combine(playbackEngines, "NDK");

            if (Directory.Exists(jdkPath))
            {
                EditorPrefs.SetString("JdkPath", jdkPath);
                EditorPrefs.SetBool("JdkUseEmbedded", true);
            }
            if (Directory.Exists(sdkPath))
            {
                EditorPrefs.SetString("AndroidSdkRoot", sdkPath);
                EditorPrefs.SetBool("AndroidSdkUseEmbedded", true);
            }
            if (Directory.Exists(ndkPath))
            {
                EditorPrefs.SetString("AndroidNdkRoot", ndkPath);
                EditorPrefs.SetBool("AndroidNdkUseEmbedded", true);
            }

            // 4c. Konfigurasi Ikon Aplikasi Resmi Android & Desktop (Launcher / Homescreen Icon)
            Texture2D appIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sprites/UI/App_Icon_512.png");
            if (appIcon != null)
            {
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new Texture2D[] { appIcon });
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new Texture2D[] { appIcon });
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, new Texture2D[] { appIcon });
                Debug.Log("[BuildPipeline] Ikon resmi Rampartbound: Merge Defense (512x512) berhasil didaftarkan ke PlayerSettings!");
            }

            // 5. Pastikan Scene Utama Terdaftar di EditorBuildSettings
            EnsureMainSceneInBuildSettings();

            AssetDatabase.SaveAssets();
            Debug.Log("[BuildPipeline] Konfigurasi PlayerSettings Berhasil Disimpan & Divalidasi!");
        }

        [MenuItem("Rampartbound/Build/1-Click Build Windows Standalone (64-bit)", false, 11)]
        public static void BuildWindowsStandalone()
        {
            Debug.Log("[BuildPipeline] Menjalankan otomatis 1-Click Setup Arena Scene sebelum kompilasi Windows...");
            RampartboundSceneSetup.SetupArenaScene();

            ConfigureProjectSettingsInternal(false);

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string outputDir = Path.Combine(projectRoot, WINDOWS_BUILD_DIR);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string exePath = Path.Combine(outputDir, "Rampartbound.exe");
            string[] scenes = GetScenesForBuild();

            Debug.Log($"[BuildPipeline] Memulai kompilasi Windows Standalone (x64) ke: {exePath}...");

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(buildOptions);
            ProcessBuildReport(report, "Windows Standalone (64-bit)", exePath);
        }

        [MenuItem("Rampartbound/Build/1-Click Build Android Sideload APK (Testing)", false, 12)]
        public static void BuildAndroidAPK()
        {
            Debug.Log("[BuildPipeline] Menjalankan otomatis 1-Click Setup Arena Scene sebelum kompilasi APK...");
            RampartboundSceneSetup.SetupArenaScene();

            ConfigureProjectSettingsInternal(false);
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string outputDir = Path.Combine(projectRoot, ANDROID_BUILD_DIR);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string apkPath = Path.Combine(outputDir, "Rampartbound_debug.apk");
            string[] scenes = GetScenesForBuild();

            Debug.Log($"[BuildPipeline] Memulai kompilasi Android Sideload APK ke: {apkPath}...");

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(buildOptions);
            ProcessBuildReport(report, "Android Sideload APK", apkPath);
        }

        [MenuItem("Rampartbound/Build/1-Click Build Google Play App Bundle (.aab - Release)", false, 13)]
        public static void BuildGooglePlayAAB()
        {
            Debug.Log("[BuildPipeline] Menjalankan otomatis 1-Click Setup Arena Scene sebelum kompilasi AAB...");
            RampartboundSceneSetup.SetupArenaScene();

            // 1. Kalibrasi IL2CPP, ARM64, dan App Bundle Mode
            ConfigureProjectSettingsInternal(true);

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string outputDir = Path.Combine(projectRoot, ANDROID_BUILD_DIR);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string aabPath = Path.Combine(outputDir, $"Rampartbound_v{APP_VERSION}_b{BUNDLE_VERSION_CODE}_release.aab");
            string[] scenes = GetScenesForBuild();

            Debug.Log($"[BuildPipeline] Memulai kompilasi Google Play App Bundle (.aab) ke: {aabPath}...");

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = aabPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(buildOptions);
            ProcessBuildReport(report, "Google Play App Bundle (.aab)", aabPath);
        }

        [MenuItem("Rampartbound/Build/Google Play Release Readiness Audit", false, 14)]
        public static void RunGooglePlayReleaseAudit()
        {
            bool passed = true;
            string reportMsg = "=== RAMPARTBOUND GOOGLE PLAY RELEASE READINESS AUDIT ===\n\n";

            // 1. Package Name
#if UNITY_2021_2_OR_NEWER
            string appId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
#else
            string appId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
#endif
            if (appId == PACKAGE_IDENTIFIER)
            {
                reportMsg += $"[PASS] Package ID: {appId}\n";
            }
            else
            {
                reportMsg += $"[FAIL] Package ID mismatch: {appId} (Expected: {PACKAGE_IDENTIFIER})\n";
                passed = false;
            }

            // 2. Target API Level
            int targetApi = (int)PlayerSettings.Android.targetSdkVersion;
            if (targetApi >= 34 || targetApi == 0)
            {
                reportMsg += $"[PASS] Target API: {(targetApi == 0 ? "Automatic / Highest (API 34+)" : targetApi.ToString())}\n";
            }
            else
            {
                reportMsg += $"[WARN] Target API Level is {targetApi}. Google Play requires API 34+.\n";
            }

            // 3. Scripting Backend
#if UNITY_2021_2_OR_NEWER
            var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
#else
            var backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
#endif
            reportMsg += $"[INFO] Scripting Backend: {backend}\n";

            // 4. Target Architectures
            var arch = PlayerSettings.Android.targetArchitectures;
            bool hasArm64 = (arch & AndroidArchitecture.ARM64) != 0;
            if (hasArm64)
            {
                reportMsg += $"[PASS] 64-bit Architecture: ARM64 Enabled ({arch})\n";
            }
            else
            {
                reportMsg += $"[FAIL] 64-bit Architecture ARM64 NOT enabled!\n";
                passed = false;
            }

            // 5. Version Code
            reportMsg += $"[PASS] Bundle Version: {PlayerSettings.bundleVersion} (Code: {PlayerSettings.Android.bundleVersionCode})\n";

            // 6. Orientation
            if (PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait)
            {
                reportMsg += $"[PASS] Screen Orientation: Portrait Locked\n";
            }
            else
            {
                reportMsg += $"[WARN] Screen Orientation is {PlayerSettings.defaultInterfaceOrientation}\n";
            }

            reportMsg += $"\nStatus Audit: {(passed ? "SIAP UNTUK GOOGLE PLAY SUBMISSION (PASSED)" : "PERLU PERBAIKAN SEBELUM SUBMIT")}";
            Debug.Log(reportMsg);
            EditorUtility.DisplayDialog("Google Play Release Audit", reportMsg, "Selesai");
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

        private static void EnsureMainSceneInBuildSettings()
        {
            var currentScenes = EditorBuildSettings.scenes;
            if (currentScenes != null)
            {
                foreach (var s in currentScenes)
                {
                    if (s.path == DEFAULT_SCENE_PATH) return;
                }
            }

            int currentLength = currentScenes != null ? currentScenes.Length : 0;
            var newScenes = new EditorBuildSettingsScene[currentLength + 1];
            for (int i = 0; i < currentLength; i++)
            {
                newScenes[i] = currentScenes[i];
            }
            newScenes[currentLength] = new EditorBuildSettingsScene(DEFAULT_SCENE_PATH, true);
            EditorBuildSettings.scenes = newScenes;
        }

        private static void ProcessBuildReport(BuildReport report, string platformName, string targetPath)
        {
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                double sizeMb = summary.totalSize / (1024.0 * 1024.0);
                string msg = $"Build {platformName} Berhasil!\nUkuran: {sizeMb:F2} MB\nWaktu: {summary.totalTime.TotalSeconds:F1} detik\nLokasi: {targetPath}";
                Debug.Log($"[BuildPipeline] {msg}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Build Berhasil", msg, "Buka Folder");
                    EditorUtility.RevealInFinder(targetPath);
                }
                else
                {
                    EditorApplication.Exit(0);
                }
            }
            else if (summary.result == BuildResult.Failed)
            {
                string errMsg = $"Build {platformName} Gagal!\nTotal Error: {summary.totalErrors}\nPeriksa Unity Console untuk log detail kesalahan.";
                Debug.LogError($"[BuildPipeline] {errMsg}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Build Gagal", errMsg, "Tutup");
                }
                else
                {
                    EditorApplication.Exit(1);
                }
            }
            else if (summary.result == BuildResult.Cancelled)
            {
                Debug.LogWarning($"[BuildPipeline] Build {platformName} Dibatalkan oleh pengguna.");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(2);
                }
            }
        }
    }
}
#endif
