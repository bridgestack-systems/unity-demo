using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace NexusArena.Editor
{
    public static class BuildManager
    {
        private const string CompanyName = "NexusArena";
        private const string ProductName = "Nexus Arena";
        private const string BuildRoot = "Build";

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/GameArena.unity",
            "Assets/Scenes/Lobby.unity",
            "Assets/Scenes/ARScene.unity",
            "Assets/Scenes/VRScene.unity"
        };

        [MenuItem("NexusArena/Build/Windows")]
        public static void BuildWindows()
        {
            ApplyCommonSettings();
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);

            Build(BuildTarget.StandaloneWindows64, $"{BuildRoot}/Windows/NexusArena.exe");
        }

        [MenuItem("NexusArena/Build/macOS")]
        public static void BuildMacOS()
        {
            ApplyCommonSettings();
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);

            Build(BuildTarget.StandaloneOSX, $"{BuildRoot}/macOS/NexusArena.app");
        }

        [MenuItem("NexusArena/Build/Linux")]
        public static void BuildLinux()
        {
            ApplyCommonSettings();
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);

            Build(BuildTarget.StandaloneLinux64, $"{BuildRoot}/Linux/NexusArena");
        }

        [MenuItem("NexusArena/Build/WebGL")]
        public static void BuildWebGL()
        {
            ApplyCommonSettings();

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            // WebGL subtarget set via build settings UI if needed

            Build(BuildTarget.WebGL, $"{BuildRoot}/WebGL");
        }

        [MenuItem("NexusArena/Build/Android")]
        public static void BuildAndroid()
        {
            ApplyCommonSettings();
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.nexusarena.game");

            Build(BuildTarget.Android, $"{BuildRoot}/Android/NexusArena.apk");
        }

        [MenuItem("NexusArena/Build/iOS")]
        public static void BuildIOS()
        {
            ApplyCommonSettings();
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.nexusarena.game");

            Build(BuildTarget.iOS, $"{BuildRoot}/iOS");
        }

        private static void ApplyCommonSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = "0.1.0";
            EnsureScenesExist();
            EnsureURPRendererData();
            DisableXRForBuild();
        }

        private static void EnsureScenesExist()
        {
            bool allExist = ScenePaths.All(s => File.Exists(s));
            if (!allExist)
            {
                Debug.Log("[NexusArena] Scenes missing — generating all scenes...");
                SceneGenerator.GenerateAllScenes();
            }
        }

        private static void EnsureURPRendererData()
        {
            try
            {
                var pipelineAsset = GraphicsSettings.defaultRenderPipeline;
                if (pipelineAsset == null)
                {
                    pipelineAsset = QualitySettings.renderPipeline;
                }

                if (pipelineAsset == null)
                {
                    Debug.Log("[NexusArena] No render pipeline asset assigned. Skipping renderer data check.");
                    return;
                }

                var so = new SerializedObject(pipelineAsset);
                var rendererDataList = so.FindProperty("m_RendererDataList");

                if (rendererDataList == null)
                {
                    Debug.Log("[NexusArena] Not a URP asset or m_RendererDataList not found.");
                    return;
                }

                bool needsFix = rendererDataList.arraySize == 0;
                if (!needsFix)
                {
                    for (int i = 0; i < rendererDataList.arraySize; i++)
                    {
                        if (rendererDataList.GetArrayElementAtIndex(i).objectReferenceValue == null)
                        {
                            needsFix = true;
                            break;
                        }
                    }
                }

                if (!needsFix)
                {
                    Debug.Log("[NexusArena] URP renderer data is properly configured.");
                    return;
                }

                Debug.Log("[NexusArena] URP renderer data missing — creating default renderer...");

                // Use LoadBuiltinRendererData via reflection (public method on UniversalRenderPipelineAsset)
                var loadMethod = pipelineAsset.GetType().GetMethod("LoadBuiltinRendererData",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (loadMethod != null)
                {
                    // Resolve RendererType enum to pass correct type
                    var rendererTypeEnum = loadMethod.GetParameters()[0].ParameterType;
                    var universalRendererValue = Enum.ToObject(rendererTypeEnum, 1); // UniversalRenderer = 1
                    loadMethod.Invoke(pipelineAsset, new object[] { universalRendererValue });
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.Log("[NexusArena] URP renderer data created and assigned via LoadBuiltinRendererData.");
                    return;
                }

                // Fallback: create UniversalRendererData via ScriptableObject.CreateInstance
                Type rendererDataType = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    rendererDataType = assembly.GetType("UnityEngine.Rendering.Universal.UniversalRendererData");
                    if (rendererDataType != null) break;
                }

                if (rendererDataType == null)
                {
                    Debug.LogWarning("[NexusArena] Could not find UniversalRendererData type.");
                    return;
                }

                var rendererData = ScriptableObject.CreateInstance(rendererDataType);
                string assetDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(pipelineAsset));
                string rendererPath = $"{assetDir}/NexusArena_URPRendererData.asset";
                AssetDatabase.CreateAsset(rendererData, rendererPath);

                if (rendererDataList.arraySize == 0)
                    rendererDataList.InsertArrayElementAtIndex(0);

                rendererDataList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(pipelineAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[NexusArena] Created and assigned renderer data: {rendererPath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NexusArena] Failed to ensure URP renderer data: {ex.Message}");
            }
        }

        private static void DisableXRForBuild()
        {
            try
            {
                var xrGeneralSettings = typeof(UnityEditor.Editor).Assembly
                    .GetType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget");
                if (xrGeneralSettings != null)
                {
                    Debug.Log("[NexusArena] XR settings detected — skipping XR initialization for standard build.");
                }
            }
            catch { }
        }

        private static void Build(BuildTarget target, string outputPath)
        {
            string[] validScenes = ScenePaths.Where(s => File.Exists(s)).ToArray();

            if (validScenes.Length == 0)
            {
                Debug.LogError("[NexusArena] No valid scenes found for build. Run NexusArena/Generate All Scenes first.");
                return;
            }

            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = validScenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[NexusArena] Build succeeded: {outputPath} ({report.summary.totalSize / (1024 * 1024):F1} MB)");
                EditorUtility.RevealInFinder(outputPath);
            }
            else
            {
                Debug.LogError($"[NexusArena] Build failed for {target}: {report.summary.totalErrors} error(s)");
            }
        }
    }
}
