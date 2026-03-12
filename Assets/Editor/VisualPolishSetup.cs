using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NexusArena.Editor
{
    /// <summary>
    /// Comprehensive visual polish setup for the Nexus Arena project.
    /// Creates all materials, configures lighting, post-processing, and URP pipeline.
    /// Run from command line: -executeMethod NexusArena.Editor.VisualPolishSetup.SetupAll
    /// </summary>
    public static class VisualPolishSetup
    {
        private const string MaterialsPath = "Assets/Materials";
        private const string ScenesPath = "Assets/Scenes";
        private const string RenderingPath = "Assets/Rendering";
        private const string LogPrefix = "[NexusArena]";

        // -----------------------------------------------------------------
        // Entry Point
        // -----------------------------------------------------------------

        [MenuItem("NexusArena/Visual Polish/Setup All")]
        public static void SetupAll()
        {
            try
            {
                Log("=== Starting Visual Polish Setup ===");

                EnsureDirectoryExists(MaterialsPath);
                EnsureDirectoryExists(RenderingPath);

                // Phase 1: Create all materials
                Log("Phase 1: Creating materials...");
                CreateAllMaterials();

                // Phase 2: Configure URP pipeline
                Log("Phase 2: Configuring URP render pipeline...");
                ConfigureURPPipeline();

                // Phase 3: Configure GameArena scene lighting & material assignments
                Log("Phase 3: Configuring GameArena scene...");
                ConfigureGameArenaScene();

                // Phase 4: Configure MainMenu scene lighting & material assignments
                Log("Phase 4: Configuring MainMenu scene...");
                ConfigureMainMenuScene();

                // Final save
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Log("=== Visual Polish Setup Complete ===");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} Visual Polish Setup failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // -----------------------------------------------------------------
        // Directory Helpers
        // -----------------------------------------------------------------

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
                string folder = Path.GetFileName(path);
                if (parent != null)
                {
                    // Recursively ensure parent exists
                    if (!AssetDatabase.IsValidFolder(parent))
                    {
                        EnsureDirectoryExists(parent);
                    }
                    AssetDatabase.CreateFolder(parent, folder);
                    Log($"Created directory: {path}");
                }
            }
        }

        // -----------------------------------------------------------------
        // Logging
        // -----------------------------------------------------------------

        private static void Log(string message)
        {
            Debug.Log($"{LogPrefix} {message}");
        }

        private static void LogWarning(string message)
        {
            Debug.LogWarning($"{LogPrefix} {message}");
        }

        // -----------------------------------------------------------------
        // Phase 1: Create All Materials
        // -----------------------------------------------------------------

        [MenuItem("NexusArena/Visual Polish/Create Materials Only")]
        public static void CreateAllMaterials()
        {
            EnsureDirectoryExists(MaterialsPath);

            CreateArenaFloorMaterial();
            CreateArenaWallMaterial();
            CreateArenaEdgeGlowMaterial();
            CreatePlayerBodyMaterial();
            CreatePlayerAccentMaterial();
            CreateDestructibleCrateMaterial();
            CreatePhysicsBallMaterial();
            CreateHologramMaterial();
            CreateSkyboxMaterial();
            CreateSpawnPointGlowMaterial();

            AssetDatabase.SaveAssets();
            Log("All materials created successfully.");
        }

        /// <summary>
        /// ArenaFloor.mat - Custom ArenaGrid shader, dark blue-black base with cyan grid
        /// </summary>
        private static void CreateArenaFloorMaterial()
        {
            Shader shader = Shader.Find("NexusArena/ArenaGrid");
            if (shader == null)
            {
                LogWarning("ArenaGrid shader not found at 'NexusArena/ArenaGrid'. " +
                           "Ensure Assets/Shaders/ArenaGrid.shader exists. Falling back to URP/Lit.");
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (shader == null)
            {
                LogWarning("Could not find any suitable shader for ArenaFloor. Skipping.");
                return;
            }

            Material mat = new Material(shader);
            mat.name = "ArenaFloor";

            if (shader.name == "NexusArena/ArenaGrid")
            {
                mat.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.05f, 1f));
                mat.SetColor("_GridColor", new Color(0f, 0.9f, 1f, 1f));
                mat.SetFloat("_GridScale", 2f);
                mat.SetFloat("_PulseSpeed", 0.5f);
                mat.SetFloat("_EmissionStrength", 2f);
            }
            else
            {
                // Fallback: configure as dark URP/Lit
                mat.SetColor("_BaseColor", new Color(0.02f, 0.02f, 0.05f, 1f));
            }

            SaveMaterial(mat, "ArenaFloor");
        }

        /// <summary>
        /// ArenaWall.mat - URP/Lit, dark metallic gray, high metallic, dim blue emission
        /// </summary>
        private static void CreateArenaWallMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                LogWarning("URP/Lit shader not found. Skipping ArenaWall material.");
                return;
            }

            Material mat = new Material(shader);
            mat.name = "ArenaWall";

            mat.SetColor("_BaseColor", new Color(0.15f, 0.15f, 0.18f, 1f));
            mat.SetFloat("_Metallic", 0.8f);
            mat.SetFloat("_Smoothness", 0.3f);

            // Enable emission
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.05f, 0.1f, 0.2f, 1f));
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            SaveMaterial(mat, "ArenaWall");
        }

        /// <summary>
        /// ArenaEdgeGlow.mat - URP/Unlit, cyan with emission, transparent
        /// </summary>
        private static void CreateArenaEdgeGlowMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                LogWarning("URP/Unlit shader not found. Skipping ArenaEdgeGlow material.");
                return;
            }

            Material mat = new Material(shader);
            mat.name = "ArenaEdgeGlow";

            // Configure transparent surface
            SetupTransparentSurface(mat);

            mat.SetColor("_BaseColor", new Color(0f, 0.9f, 1f, 0.8f));

            // Enable emission
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0f, 0.9f, 1f, 1f) * 2f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            SaveMaterial(mat, "ArenaEdgeGlow");
        }

        /// <summary>
        /// PlayerBody.mat - URP/Lit, white-silver, metallic 0.6, smoothness 0.7
        /// </summary>
        private static void CreatePlayerBodyMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                LogWarning("URP/Lit shader not found. Skipping PlayerBody material.");
                return;
            }

            Material mat = new Material(shader);
            mat.name = "PlayerBody";

            mat.SetColor("_BaseColor", new Color(0.85f, 0.87f, 0.9f, 1f));
            mat.SetFloat("_Metallic", 0.6f);
            mat.SetFloat("_Smoothness", 0.7f);

            SaveMaterial(mat, "PlayerBody");
        }

        /// <summary>
        /// PlayerAccent.mat - URP/Lit, bright cyan accent with emission
        /// </summary>
        private static void CreatePlayerAccentMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                LogWarning("URP/Lit shader not found. Skipping PlayerAccent material.");
                return;
            }

            Material mat = new Material(shader);
            mat.name = "PlayerAccent";

            mat.SetColor("_BaseColor", new Color(0f, 0.8f, 1f, 1f));
            mat.SetFloat("_Metallic", 0.4f);
            mat.SetFloat("_Smoothness", 0.6f);

            // Bright cyan emission
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0f, 0.8f, 1f, 1f) * 1.5f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            SaveMaterial(mat, "PlayerAccent");
        }

        /// <summary>
        /// DestructibleCrate.mat - URP/Lit, orange-brown wood-like, no metallic, smoothness 0.3
        /// </summary>
        private static void CreateDestructibleCrateMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                LogWarning("URP/Lit shader not found. Skipping DestructibleCrate material.");
                return;
            }

            Material mat = new Material(shader);
            mat.name = "DestructibleCrate";

            mat.SetColor("_BaseColor", new Color(0.6f, 0.35f, 0.15f, 1f));
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.3f);

            SaveMaterial(mat, "DestructibleCrate");
        }

        /// <summary>
        /// PhysicsBall.mat - URP/Lit, red, metallic 0.2, smoothness 0.8
        /// </summary>
        private static void CreatePhysicsBallMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                LogWarning("URP/Lit shader not found. Skipping PhysicsBall material.");
                return;
            }

            Material mat = new Material(shader);
            mat.name = "PhysicsBall";

            mat.SetColor("_BaseColor", new Color(0.9f, 0.1f, 0.1f, 1f));
            mat.SetFloat("_Metallic", 0.2f);
            mat.SetFloat("_Smoothness", 0.8f);

            SaveMaterial(mat, "PhysicsBall");
        }

        /// <summary>
        /// HologramMat.mat - Custom Hologram shader, cyan holo color
        /// </summary>
        private static void CreateHologramMaterial()
        {
            Shader shader = Shader.Find("NexusArena/Hologram");
            if (shader == null)
            {
                LogWarning("Hologram shader not found at 'NexusArena/Hologram'. " +
                           "Ensure Assets/Shaders/Hologram.shader exists. Falling back to URP/Unlit.");
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                LogWarning("Could not find any suitable shader for HologramMat. Skipping.");
                return;
            }

            Material mat = new Material(shader);
            mat.name = "HologramMat";

            if (shader.name == "NexusArena/Hologram")
            {
                mat.SetColor("_HoloColor", new Color(0f, 0.9f, 1f, 1f));
                mat.SetFloat("_ScanLineSpeed", 2f);
                mat.SetFloat("_Alpha", 0.6f);
            }
            else
            {
                // Fallback: transparent cyan unlit
                SetupTransparentSurface(mat);
                mat.SetColor("_BaseColor", new Color(0f, 0.9f, 1f, 0.6f));
            }

            SaveMaterial(mat, "HologramMat");
        }

        /// <summary>
        /// SkyboxMat.mat - Procedural skybox with dark space-like tint
        /// </summary>
        private static void CreateSkyboxMaterial()
        {
            // Try the built-in procedural skybox shader first
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                // Fall back to gradient skybox or standard skybox
                shader = Shader.Find("Skybox/6 Sided");
                if (shader == null)
                {
                    LogWarning("No skybox shader found. Skipping SkyboxMat material.");
                    return;
                }
            }

            Material mat = new Material(shader);
            mat.name = "SkyboxMat";

            if (shader.name == "Skybox/Procedural")
            {
                // Configure for a dark space-like look
                // _SunDisk: 0 = None, 1 = Simple, 2 = High Quality
                mat.SetFloat("_SunDisk", 0f); // No sun disk for space feel
                mat.SetFloat("_SunSize", 0.02f);
                mat.SetFloat("_SunSizeConvergence", 5f);
                mat.SetFloat("_AtmosphereThickness", 0.4f);
                mat.SetColor("_SkyTint", new Color(0.05f, 0.05f, 0.15f, 1f));
                mat.SetColor("_GroundColor", new Color(0.02f, 0.02f, 0.04f, 1f));
                mat.SetFloat("_Exposure", 0.5f);
            }

            SaveMaterial(mat, "SkyboxMat");
        }

        /// <summary>
        /// SpawnPointGlow.mat - URP/Unlit, green with emission, transparent
        /// </summary>
        private static void CreateSpawnPointGlowMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                LogWarning("URP/Unlit shader not found. Skipping SpawnPointGlow material.");
                return;
            }

            Material mat = new Material(shader);
            mat.name = "SpawnPointGlow";

            // Configure transparent surface
            SetupTransparentSurface(mat);

            mat.SetColor("_BaseColor", new Color(0f, 1f, 0.3f, 0.6f));

            // Enable emission for glow effect
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0f, 1f, 0.3f, 1f) * 2f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            SaveMaterial(mat, "SpawnPointGlow");
        }

        // -----------------------------------------------------------------
        // Material Helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Configures a URP material for transparent rendering.
        /// Sets _Surface to 1 (Transparent), _Blend to 0 (Alpha), and render queue.
        /// </summary>
        private static void SetupTransparentSurface(Material mat)
        {
            mat.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
            mat.SetFloat("_Blend", 0f);   // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = (int)RenderQueue.Transparent;

            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        /// <summary>
        /// Saves a material as an asset, overwriting if it already exists.
        /// </summary>
        private static void SaveMaterial(Material mat, string name)
        {
            string assetPath = $"{MaterialsPath}/{name}.mat";

            Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null)
            {
                // Overwrite existing material by copying properties
                EditorUtility.CopySerialized(mat, existing);
                EditorUtility.SetDirty(existing);
                Log($"Updated existing material: {assetPath}");
            }
            else
            {
                AssetDatabase.CreateAsset(mat, assetPath);
                Log($"Created material: {assetPath}");
            }
        }

        // -----------------------------------------------------------------
        // Phase 2: Configure URP Render Pipeline
        // -----------------------------------------------------------------

        [MenuItem("NexusArena/Visual Polish/Configure URP Pipeline")]
        public static void ConfigureURPPipeline()
        {
            EnsureDirectoryExists(RenderingPath);

            try
            {
                // Look for existing URP pipeline assets in the project
                string[] urpAssetGuids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
                UnityEngine.Object existingPipelineAsset = null;

                if (urpAssetGuids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(urpAssetGuids[0]);
                    existingPipelineAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    Log($"Found existing URP pipeline asset: {path}");
                }

                // Try to use the URP-specific type for configuration
                Type urpAssetType = FindType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset");

                if (urpAssetType != null)
                {
                    ConfigureURPPipelineWithReflection(urpAssetType, existingPipelineAsset);
                }
                else
                {
                    LogWarning("UniversalRenderPipelineAsset type not found. " +
                               "URP package may not be installed. Skipping pipeline configuration.");

                    // Still try to configure what we can via GraphicsSettings
                    if (existingPipelineAsset != null)
                    {
                        ConfigureExistingPipelineViaSerializedObject(existingPipelineAsset);
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning($"URP pipeline configuration encountered an error: {ex.Message}");
            }
        }

        /// <summary>
        /// Configures the URP pipeline asset using reflection to avoid hard compile-time dependency.
        /// </summary>
        private static void ConfigureURPPipelineWithReflection(Type urpAssetType, UnityEngine.Object existingAsset)
        {
            UnityEngine.Object pipelineAsset = existingAsset;

            if (pipelineAsset == null)
            {
                // Try using the factory method Create() which also creates renderer data
                var createMethod = urpAssetType.GetMethod("Create",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (createMethod != null)
                {
                    pipelineAsset = createMethod.Invoke(null, new object[] { null }) as UnityEngine.Object;
                }

                if (pipelineAsset == null)
                {
                    pipelineAsset = ScriptableObject.CreateInstance(urpAssetType) as UnityEngine.Object;
                }

                if (pipelineAsset == null)
                {
                    LogWarning("Failed to create UniversalRenderPipelineAsset instance.");
                    return;
                }

                string assetPath = $"{RenderingPath}/NexusArena_URPAsset.asset";
                AssetDatabase.CreateAsset(pipelineAsset, assetPath);
                Log($"Created new URP pipeline asset: {assetPath}");
            }

            // Ensure renderer data exists
            EnsureRendererDataExists(pipelineAsset);

            // Configure via SerializedObject for reliable property access
            ConfigureExistingPipelineViaSerializedObject(pipelineAsset);

            // Assign to GraphicsSettings if not already set
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                GraphicsSettings.defaultRenderPipeline = pipelineAsset as RenderPipelineAsset;
                Log("Assigned URP pipeline asset to GraphicsSettings.defaultRenderPipeline.");
            }
            else
            {
                Log("GraphicsSettings.defaultRenderPipeline already set. Updating configuration only.");
                // Update the existing one too
                if (GraphicsSettings.defaultRenderPipeline != pipelineAsset)
                {
                    ConfigureExistingPipelineViaSerializedObject(GraphicsSettings.defaultRenderPipeline);
                }
            }

            // Also set quality-level render pipeline
            QualitySettings.renderPipeline = pipelineAsset as RenderPipelineAsset;
            Log("Assigned URP pipeline asset to QualitySettings.renderPipeline.");
        }

        /// <summary>
        /// Uses SerializedObject to configure URP pipeline properties regardless of compile-time type.
        /// </summary>
        private static void ConfigureExistingPipelineViaSerializedObject(UnityEngine.Object pipelineAsset)
        {
            if (pipelineAsset == null) return;

            SerializedObject serialized = new SerializedObject(pipelineAsset);

            // Shadow distance
            SetSerializedFloat(serialized, "m_MainLightShadowmapResolution", 2048);
            SetSerializedFloat(serialized, "m_ShadowDistance", 50f);

            // Shadow cascades (4 cascades)
            SetSerializedInt(serialized, "m_ShadowCascadeCount", 4);

            // Cascade splits for 4 cascades
            SerializedProperty cascade2Split = serialized.FindProperty("m_Cascade2Split");
            if (cascade2Split != null)
                cascade2Split.floatValue = 0.25f;

            SerializedProperty cascade3Split = serialized.FindProperty("m_Cascade3Split");
            if (cascade3Split != null)
            {
                cascade3Split.vector2Value = new Vector2(0.1f, 0.3f);
            }

            SerializedProperty cascade4Split = serialized.FindProperty("m_Cascade4Split");
            if (cascade4Split != null)
            {
                cascade4Split.vector3Value = new Vector3(0.067f, 0.2f, 0.467f);
            }

            // HDR
            SetSerializedBool(serialized, "m_SupportsHDR", true);

            // MSAA: 0=Disabled, 1=2x, 2=4x, 3=8x — URP uses the actual sample count
            // In URP serialized data, MSAA is stored as the actual count: 1, 2, 4, 8
            SetSerializedInt(serialized, "m_MSAA", 4);

            // Main light shadows enabled
            SetSerializedInt(serialized, "m_MainLightRenderingMode", 1); // 0=Disabled, 1=PerPixel
            SetSerializedBool(serialized, "m_MainLightShadowsSupported", true);

            // Additional lights
            SetSerializedInt(serialized, "m_AdditionalLightsRenderingMode", 1); // PerPixel
            SetSerializedBool(serialized, "m_AdditionalLightShadowsSupported", true);

            // Soft shadows
            SetSerializedBool(serialized, "m_SoftShadowsSupported", true);

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(pipelineAsset);

            Log("URP pipeline asset configured: Shadow distance=50, Cascades=4, HDR=On, MSAA=4x, Soft Shadows=On.");
        }

        /// <summary>
        /// Ensures the URP pipeline asset has valid renderer data assigned.
        /// </summary>
        private static void EnsureRendererDataExists(UnityEngine.Object pipelineAsset)
        {
            if (pipelineAsset == null) return;

            SerializedObject so = new SerializedObject(pipelineAsset);
            SerializedProperty rendererDataList = so.FindProperty("m_RendererDataList");

            if (rendererDataList == null) return;

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

            if (!needsFix) return;

            Log("Renderer data missing on URP asset — attempting to create...");

            // Try LoadBuiltinRendererData via reflection
            var loadMethod = pipelineAsset.GetType().GetMethod("LoadBuiltinRendererData",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (loadMethod != null)
            {
                var rendererTypeEnum = loadMethod.GetParameters()[0].ParameterType;
                var universalRendererValue = Enum.ToObject(rendererTypeEnum, 1); // UniversalRenderer = 1
                loadMethod.Invoke(pipelineAsset, new object[] { universalRendererValue });
                AssetDatabase.SaveAssets();
                Log("Created renderer data via LoadBuiltinRendererData.");
                return;
            }

            // Fallback: create renderer data manually
            Type rendererDataType = FindType("UnityEngine.Rendering.Universal.UniversalRendererData");
            if (rendererDataType != null)
            {
                var rendererData = ScriptableObject.CreateInstance(rendererDataType);
                string rendererPath = $"{RenderingPath}/NexusArena_URPRendererData.asset";
                AssetDatabase.CreateAsset(rendererData, rendererPath);

                if (rendererDataList.arraySize == 0)
                    rendererDataList.InsertArrayElementAtIndex(0);

                rendererDataList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(pipelineAsset);
                AssetDatabase.SaveAssets();
                Log($"Created and assigned renderer data: {rendererPath}");
            }
            else
            {
                LogWarning("Could not find UniversalRendererData type to create renderer data.");
            }
        }

        // -----------------------------------------------------------------
        // Phase 3: Configure GameArena Scene
        // -----------------------------------------------------------------

        [MenuItem("NexusArena/Visual Polish/Configure GameArena Scene")]
        public static void ConfigureGameArenaScene()
        {
            string scenePath = $"{ScenesPath}/GameArena.unity";
            if (!File.Exists(scenePath))
            {
                LogWarning($"GameArena scene not found at {scenePath}. Skipping scene configuration.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Log("Opened GameArena scene.");

            // --- Lighting ---
            ConfigureGameArenaLighting(scene);

            // --- Material Assignments ---
            AssignGameArenaMaterials(scene);

            // --- Skybox ---
            AssignSkybox();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Log("GameArena scene configured and saved.");
        }

        private static void ConfigureGameArenaLighting(Scene scene)
        {
            // Find directional light
            Light directionalLight = FindComponentInScene<Light>(scene, light => light.type == LightType.Directional);

            if (directionalLight != null)
            {
                directionalLight.color = new Color(1f, 0.96f, 0.9f, 1f);
                directionalLight.intensity = 1.2f;
                directionalLight.shadows = LightShadows.Soft;
                directionalLight.shadowStrength = 0.8f;
                directionalLight.shadowBias = 0.05f;
                directionalLight.shadowNormalBias = 0.4f;
                EditorUtility.SetDirty(directionalLight.gameObject);
                Log("Directional light configured: warm white, intensity 1.2, soft shadows.");
            }
            else
            {
                LogWarning("No Directional Light found in GameArena scene.");
            }

            // Ambient lighting
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.15f, 1f);
            Log("Ambient light set to dark blue flat mode.");

            // Fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.03f, 0.03f, 0.08f, 1f);
            RenderSettings.fogDensity = 0.01f;
            Log("Fog enabled: exponential, dark blue-gray, density 0.01.");
        }

        private static void AssignGameArenaMaterials(Scene scene)
        {
            // Load materials
            Material arenaFloor = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/ArenaFloor.mat");
            Material arenaWall = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/ArenaWall.mat");
            Material spawnPointGlow = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/SpawnPointGlow.mat");

            // Assign ArenaFloor to ground plane
            if (arenaFloor != null)
            {
                GameObject ground = FindGameObjectInScene(scene, "Ground");
                if (ground == null)
                    ground = FindGameObjectInScene(scene, "Floor");
                if (ground == null)
                    ground = FindGameObjectInScene(scene, "ArenaFloor");

                if (ground != null)
                {
                    Renderer renderer = ground.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.sharedMaterial = arenaFloor;
                        EditorUtility.SetDirty(renderer);
                        Log($"Assigned ArenaFloor material to '{ground.name}'.");
                    }
                }
                else
                {
                    LogWarning("No ground object found in GameArena scene (searched: Ground, Floor, ArenaFloor).");
                }
            }

            // Assign ArenaWall to wall objects
            if (arenaWall != null)
            {
                int wallCount = 0;
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject root in rootObjects)
                {
                    AssignMaterialToMatchingObjects(root, arenaWall, "Wall", ref wallCount);
                }

                if (wallCount > 0)
                    Log($"Assigned ArenaWall material to {wallCount} wall object(s).");
                else
                    LogWarning("No wall objects found in GameArena scene.");
            }

            // Assign SpawnPointGlow to spawn points that have renderers
            if (spawnPointGlow != null)
            {
                int spawnCount = 0;
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject root in rootObjects)
                {
                    AssignMaterialToMatchingObjects(root, spawnPointGlow, "SpawnPoint", ref spawnCount);
                    AssignMaterialToMatchingObjects(root, spawnPointGlow, "Spawn_Point", ref spawnCount);
                }

                if (spawnCount > 0)
                    Log($"Assigned SpawnPointGlow material to {spawnCount} spawn point object(s).");

                // Also try to add visual indicators to spawn points that don't have renderers
                AssignSpawnPointVisuals(scene, spawnPointGlow);
            }
        }

        /// <summary>
        /// For spawn point GameObjects that have no Renderer, add a small glowing disc.
        /// </summary>
        private static void AssignSpawnPointVisuals(Scene scene, Material glowMat)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                // Check root and children
                Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in allTransforms)
                {
                    if (t.name.Contains("SpawnPoint") || t.name.Contains("Spawn_Point"))
                    {
                        // Only add visual if no renderer exists
                        if (t.GetComponent<Renderer>() == null && t.GetComponentInChildren<Renderer>() == null)
                        {
                            // Create a small flat cylinder as spawn indicator
                            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                            indicator.name = "SpawnIndicator";
                            indicator.transform.SetParent(t, false);
                            indicator.transform.localPosition = Vector3.zero;
                            indicator.transform.localScale = new Vector3(2f, 0.05f, 2f);

                            // Remove collider to avoid physics interference
                            Collider col = indicator.GetComponent<Collider>();
                            if (col != null)
                                UnityEngine.Object.DestroyImmediate(col);

                            Renderer rend = indicator.GetComponent<Renderer>();
                            if (rend != null)
                                rend.sharedMaterial = glowMat;

                            EditorUtility.SetDirty(indicator);
                            Log($"Added spawn indicator visual to '{t.name}'.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Recursively finds objects whose name contains the search string and assigns material.
        /// </summary>
        private static void AssignMaterialToMatchingObjects(GameObject root, Material mat, string nameContains, ref int count)
        {
            Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTransforms)
            {
                if (t.name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Renderer renderer = t.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.sharedMaterial = mat;
                        EditorUtility.SetDirty(renderer);
                        count++;
                    }
                }
            }
        }

        private static void AssignSkybox()
        {
            Material skyboxMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/SkyboxMat.mat");
            if (skyboxMat != null)
            {
                RenderSettings.skybox = skyboxMat;
                Log("Assigned SkyboxMat to RenderSettings.skybox.");
            }
        }

        // -----------------------------------------------------------------
        // Phase 4: Configure MainMenu Scene
        // -----------------------------------------------------------------

        [MenuItem("NexusArena/Visual Polish/Configure MainMenu Scene")]
        public static void ConfigureMainMenuScene()
        {
            string scenePath = $"{ScenesPath}/MainMenu.unity";
            if (!File.Exists(scenePath))
            {
                LogWarning($"MainMenu scene not found at {scenePath}. Skipping scene configuration.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Log("Opened MainMenu scene.");

            // --- Lighting ---
            ConfigureMainMenuLighting(scene);

            // --- Material Assignments ---
            AssignMainMenuMaterials(scene);

            // --- Skybox ---
            AssignSkybox();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Log("MainMenu scene configured and saved.");
        }

        private static void ConfigureMainMenuLighting(Scene scene)
        {
            // Find directional light
            Light directionalLight = FindComponentInScene<Light>(scene, light => light.type == LightType.Directional);

            if (directionalLight != null)
            {
                directionalLight.color = new Color(0.6f, 0.7f, 1f, 1f); // Blueish tone
                directionalLight.intensity = 0.5f; // Lower intensity for moody feel
                directionalLight.shadows = LightShadows.Soft;
                directionalLight.shadowStrength = 0.6f;
                EditorUtility.SetDirty(directionalLight.gameObject);
                Log("MainMenu directional light: blueish tone, intensity 0.5, soft shadows.");
            }
            else
            {
                LogWarning("No Directional Light found in MainMenu scene.");
            }

            // Very dark ambient for moody atmosphere
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.02f, 0.02f, 0.05f, 1f);
            Log("MainMenu ambient light set to very dark blue.");

            // Subtle fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.01f, 0.01f, 0.03f, 1f);
            RenderSettings.fogDensity = 0.015f;
        }

        private static void AssignMainMenuMaterials(Scene scene)
        {
            Material hologramMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/HologramMat.mat");

            if (hologramMat != null)
            {
                // Look for preview/display objects in the main menu
                string[] previewNames = {
                    "RotatingPlatformPreview", "Preview", "Display", "Model",
                    "CharacterPreview", "ArenaPreview", "MenuModel"
                };

                int assignCount = 0;
                GameObject[] rootObjects = scene.GetRootGameObjects();

                foreach (GameObject root in rootObjects)
                {
                    Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
                    foreach (Transform t in allTransforms)
                    {
                        foreach (string previewName in previewNames)
                        {
                            if (t.name.IndexOf(previewName, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                Renderer renderer = t.GetComponent<Renderer>();
                                if (renderer != null)
                                {
                                    renderer.sharedMaterial = hologramMat;
                                    EditorUtility.SetDirty(renderer);
                                    assignCount++;
                                    Log($"Assigned HologramMat to '{t.name}'.");
                                }
                                break;
                            }
                        }
                    }
                }

                if (assignCount == 0)
                {
                    LogWarning("No preview objects found in MainMenu scene for hologram material assignment.");
                }
            }
            else
            {
                LogWarning("HologramMat material not found. Skipping MainMenu material assignment.");
            }
        }

        // -----------------------------------------------------------------
        // Utility: Scene Object Finders
        // -----------------------------------------------------------------

        /// <summary>
        /// Finds a component in a scene that matches the given predicate.
        /// </summary>
        private static T FindComponentInScene<T>(Scene scene, Func<T, bool> predicate) where T : Component
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                T[] components = root.GetComponentsInChildren<T>(true);
                foreach (T component in components)
                {
                    if (predicate(component))
                        return component;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds a root-level or nested GameObject by exact name in a scene.
        /// </summary>
        private static GameObject FindGameObjectInScene(Scene scene, string name)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                if (root.name == name)
                    return root;

                Transform found = root.transform.Find(name);
                if (found != null)
                    return found.gameObject;

                // Deep search
                Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in allTransforms)
                {
                    if (t.name == name)
                        return t.gameObject;
                }
            }
            return null;
        }

        // -----------------------------------------------------------------
        // Utility: Serialized Object Helpers
        // -----------------------------------------------------------------

        private static void SetSerializedFloat(SerializedObject obj, string propertyName, float value)
        {
            SerializedProperty prop = obj.FindProperty(propertyName);
            if (prop != null)
                prop.floatValue = value;
        }

        private static void SetSerializedInt(SerializedObject obj, string propertyName, int value)
        {
            SerializedProperty prop = obj.FindProperty(propertyName);
            if (prop != null)
                prop.intValue = value;
        }

        private static void SetSerializedBool(SerializedObject obj, string propertyName, bool value)
        {
            SerializedProperty prop = obj.FindProperty(propertyName);
            if (prop != null)
                prop.boolValue = value;
        }

        // -----------------------------------------------------------------
        // Utility: Type Resolution
        // -----------------------------------------------------------------

        /// <summary>
        /// Attempts to find a type by name across all loaded assemblies.
        /// </summary>
        private static Type FindType(string fullTypeName)
        {
            // Try direct
            Type type = Type.GetType(fullTypeName);
            if (type != null) return type;

            // Search all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullTypeName);
                if (type != null) return type;
            }

            return null;
        }
    }
}
