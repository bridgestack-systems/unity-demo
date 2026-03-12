using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NexusArena.Editor
{
    public static class CharacterGenerator
    {
        private const string PrefabsPath = "Assets/Prefabs";
        private const string MaterialsPath = "Assets/Materials";
        private const string PrefabAssetPath = "Assets/Prefabs/PlayerRobot.prefab";
        private const string BodyMaterialPath = "Assets/Materials/PlayerBody.mat";
        private const string AccentMaterialPath = "Assets/Materials/PlayerAccent.mat";
        private const string GameArenaScenePath = "Assets/Scenes/GameArena.unity";

        [MenuItem("NexusArena/Generate Player Character")]
        public static void GenerateCharacter()
        {
            EnsureDirectoryExists(PrefabsPath);
            EnsureDirectoryExists(MaterialsPath);

            Material bodyMat = GetOrCreateBodyMaterial();
            Material accentMat = GetOrCreateAccentMaterial();

            // Build the robot hierarchy in the active scene, then save as prefab
            GameObject root = new GameObject("PlayerRobot");

            // --- CharacterController ---
            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            // --- PlayerController (depends on Input System, so wrap in try-catch) ---
            try
            {
                root.AddComponent<Player.PlayerController>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NexusArena] Could not add PlayerController: {e.Message}");
            }

            // --- Layer ---
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                SetLayerRecursive(root, playerLayer);
            }

            // --- Tag ---
            try
            {
                root.tag = "Player";
            }
            catch
            {
                Debug.LogWarning("[NexusArena] 'Player' tag not found. Using default tag.");
            }

            // =====================
            // Body hierarchy
            // =====================
            GameObject body = CreateEmpty("Body", root.transform, Vector3.zero);

            // --- Torso ---
            CreatePrimitivePart("Torso", PrimitiveType.Cube,
                body.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.6f, 0.8f, 0.35f),
                bodyMat, receiveShadows: true, castShadows: true);

            // --- Waist ---
            CreatePrimitivePart("Waist", PrimitiveType.Cube,
                body.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.45f, 0.2f, 0.3f),
                bodyMat, receiveShadows: true, castShadows: true);

            // --- Head ---
            GameObject head = CreateEmpty("Head", body.transform, Vector3.zero);

            CreatePrimitivePart("HeadBase", PrimitiveType.Sphere,
                head.transform, new Vector3(0f, 1.7f, 0f), new Vector3(0.35f, 0.35f, 0.35f),
                bodyMat, receiveShadows: true, castShadows: true);

            CreatePrimitivePart("Visor", PrimitiveType.Cube,
                head.transform, new Vector3(0f, 1.7f, 0.12f), new Vector3(0.3f, 0.1f, 0.2f),
                accentMat, receiveShadows: true, castShadows: true);

            CreatePrimitivePart("Antenna", PrimitiveType.Cylinder,
                head.transform, new Vector3(0.1f, 1.95f, 0f), new Vector3(0.03f, 0.15f, 0.03f),
                accentMat, receiveShadows: true, castShadows: false);

            // --- Left Arm ---
            GameObject leftArm = CreateEmpty("LeftArm", body.transform, Vector3.zero);

            CreatePrimitivePart("UpperArm", PrimitiveType.Cube,
                leftArm.transform, new Vector3(-0.45f, 1.15f, 0f), new Vector3(0.15f, 0.35f, 0.15f),
                bodyMat, receiveShadows: true, castShadows: true);

            CreatePrimitivePart("LowerArm", PrimitiveType.Cube,
                leftArm.transform, new Vector3(-0.45f, 0.75f, 0f), new Vector3(0.12f, 0.35f, 0.12f),
                bodyMat, receiveShadows: true, castShadows: true);

            CreatePrimitivePart("Hand", PrimitiveType.Sphere,
                leftArm.transform, new Vector3(-0.45f, 0.55f, 0f), new Vector3(0.12f, 0.12f, 0.12f),
                accentMat, receiveShadows: true, castShadows: false);

            // --- Right Arm (mirror of Left on X) ---
            GameObject rightArm = CreateEmpty("RightArm", body.transform, Vector3.zero);

            CreatePrimitivePart("UpperArm", PrimitiveType.Cube,
                rightArm.transform, new Vector3(0.45f, 1.15f, 0f), new Vector3(0.15f, 0.35f, 0.15f),
                bodyMat, receiveShadows: true, castShadows: true);

            CreatePrimitivePart("LowerArm", PrimitiveType.Cube,
                rightArm.transform, new Vector3(0.45f, 0.75f, 0f), new Vector3(0.12f, 0.35f, 0.12f),
                bodyMat, receiveShadows: true, castShadows: true);

            CreatePrimitivePart("Hand", PrimitiveType.Sphere,
                rightArm.transform, new Vector3(0.45f, 0.55f, 0f), new Vector3(0.12f, 0.12f, 0.12f),
                accentMat, receiveShadows: true, castShadows: false);

            // --- Left Leg ---
            GameObject leftLeg = CreateEmpty("LeftLeg", body.transform, Vector3.zero);

            CreatePrimitivePart("UpperLeg", PrimitiveType.Cube,
                leftLeg.transform, new Vector3(-0.15f, 0.35f, 0f), new Vector3(0.18f, 0.4f, 0.18f),
                bodyMat, receiveShadows: true, castShadows: true);

            CreatePrimitivePart("LowerLeg", PrimitiveType.Cube,
                leftLeg.transform, new Vector3(-0.15f, -0.1f, 0f), new Vector3(0.14f, 0.4f, 0.14f),
                bodyMat, receiveShadows: true, castShadows: true);

            CreatePrimitivePart("Foot", PrimitiveType.Cube,
                leftLeg.transform, new Vector3(-0.15f, -0.32f, 0.04f), new Vector3(0.18f, 0.08f, 0.28f),
                accentMat, receiveShadows: true, castShadows: true);

            // --- Right Leg (mirror of Left on X) ---
            GameObject rightLeg = CreateEmpty("RightLeg", body.transform, Vector3.zero);

            CreatePrimitivePart("UpperLeg", PrimitiveType.Cube,
                rightLeg.transform, new Vector3(0.15f, 0.35f, 0f), new Vector3(0.18f, 0.4f, 0.18f),
                bodyMat, receiveShadows: true, castShadows: true);

            CreatePrimitivePart("LowerLeg", PrimitiveType.Cube,
                rightLeg.transform, new Vector3(0.15f, -0.1f, 0f), new Vector3(0.14f, 0.4f, 0.14f),
                bodyMat, receiveShadows: true, castShadows: true);

            CreatePrimitivePart("Foot", PrimitiveType.Cube,
                rightLeg.transform, new Vector3(0.15f, -0.32f, 0.04f), new Vector3(0.18f, 0.08f, 0.28f),
                accentMat, receiveShadows: true, castShadows: true);

            // =====================
            // Utility empty GameObjects
            // =====================
            CreateEmpty("GrabPoint", root.transform, new Vector3(0f, 1.0f, 0.8f));
            CreateEmpty("GroundCheck", root.transform, new Vector3(0f, 0.02f, 0f));
            CreateEmpty("CameraTarget", root.transform, new Vector3(0f, 1.5f, 0f));

            // --- Ensure layer is set on all children ---
            if (playerLayer >= 0)
            {
                SetLayerRecursive(root, playerLayer);
            }

            // =====================
            // Save as prefab
            // =====================
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabAssetPath);
            Debug.Log($"[NexusArena] Player robot prefab saved to {PrefabAssetPath}");

            // Destroy the temporary scene object
            UnityEngine.Object.DestroyImmediate(root);

            // =====================
            // Place instance in GameArena scene
            // =====================
            PlaceInGameArenaScene(prefab);

            AssetDatabase.Refresh();
            Debug.Log("[NexusArena] Character generation complete.");
        }

        // =====================================================================
        // Helper: create a primitive part, strip its default collider, assign
        // material, configure shadow settings.
        // =====================================================================
        private static GameObject CreatePrimitivePart(
            string name,
            PrimitiveType primitiveType,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool receiveShadows,
            bool castShadows)
        {
            GameObject obj = GameObject.CreatePrimitive(primitiveType);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localScale = localScale;

            // Remove default collider (only CharacterController on root)
            Collider col = obj.GetComponent<Collider>();
            if (col != null)
            {
                UnityEngine.Object.DestroyImmediate(col);
            }

            // Material
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.receiveShadows = receiveShadows;
                renderer.shadowCastingMode = castShadows
                    ? UnityEngine.Rendering.ShadowCastingMode.On
                    : UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            return obj;
        }

        // =====================================================================
        // Helper: create an empty GameObject under a parent
        // =====================================================================
        private static GameObject CreateEmpty(string name, Transform parent, Vector3 localPosition)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            return obj;
        }

        // =====================================================================
        // Materials
        // =====================================================================
        private static Material GetOrCreateBodyMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath);
            if (mat != null) return mat;

            // Try URP Lit shader first, fall back to Standard
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            mat = new Material(shader);
            mat.name = "PlayerBody";

            // Medium gray, metallic look
            mat.color = new Color(0.45f, 0.48f, 0.52f, 1f);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0.75f);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.6f);
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", 0.6f);

            AssetDatabase.CreateAsset(mat, BodyMaterialPath);
            Debug.Log($"[NexusArena] Created body material at {BodyMaterialPath}");
            return mat;
        }

        private static Material GetOrCreateAccentMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(AccentMaterialPath);
            if (mat != null) return mat;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            mat = new Material(shader);
            mat.name = "PlayerAccent";

            // Cyan with emission for the visor / accents
            Color cyan = new Color(0f, 0.9f, 1f, 1f);
            mat.color = cyan;

            // Enable emission
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", cyan * 2f);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0.5f);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.85f);
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", 0.85f);

            // URP surface type: set to opaque by default (no changes needed)
            // Flag the material so global illumination picks up emission
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            AssetDatabase.CreateAsset(mat, AccentMaterialPath);
            Debug.Log($"[NexusArena] Created accent material at {AccentMaterialPath}");
            return mat;
        }

        // =====================================================================
        // Place the prefab into the GameArena scene
        // =====================================================================
        private static void PlaceInGameArenaScene(GameObject prefab)
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), GameArenaScenePath)))
            {
                Debug.LogWarning($"[NexusArena] GameArena scene not found at {GameArenaScenePath}. Skipping scene placement.");
                return;
            }

            Scene arenaScene = EditorSceneManager.OpenScene(GameArenaScenePath, OpenSceneMode.Single);

            // Remove old PlayerRobot instance if one exists
            foreach (GameObject rootObj in arenaScene.GetRootGameObjects())
            {
                if (rootObj.name == "PlayerRobot")
                {
                    UnityEngine.Object.DestroyImmediate(rootObj);
                }
            }

            // Instantiate the prefab at the spawn point
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = new Vector3(0f, 1f, 0f);
            instance.transform.rotation = Quaternion.identity;
            SceneManager.MoveGameObjectToScene(instance, arenaScene);

            // --- Cinemachine FreeLook targeting the CameraTarget ---
            SetupCinemachineFreeLook(instance, arenaScene);

            EditorSceneManager.SaveScene(arenaScene);
            Debug.Log($"[NexusArena] Placed PlayerRobot instance in {GameArenaScenePath}");
        }

        // =====================================================================
        // Cinemachine FreeLook setup (wrapped in try-catch)
        // =====================================================================
        private static void SetupCinemachineFreeLook(GameObject playerInstance, Scene scene)
        {
            try
            {
                // Find the CameraTarget on the player instance
                Transform cameraTarget = playerInstance.transform.Find("CameraTarget");
                if (cameraTarget == null)
                {
                    Debug.LogWarning("[NexusArena] CameraTarget not found on player instance.");
                    return;
                }

                // Look for an existing FreeLook camera in the scene
                GameObject freeLookObj = null;

                // Try to find Cinemachine FreeLook type
                Type freeLookType = Type.GetType("Cinemachine.CinemachineFreeLook, Cinemachine");
                // Also try the newer Unity.Cinemachine namespace
                if (freeLookType == null)
                    freeLookType = Type.GetType("Unity.Cinemachine.CinemachineFreeLook, Unity.Cinemachine");

                if (freeLookType == null)
                {
                    Debug.LogWarning("[NexusArena] Cinemachine FreeLook type not found. Skipping FreeLook camera setup.");
                    return;
                }

                // Check existing scene objects for a FreeLook camera
                foreach (GameObject rootObj in scene.GetRootGameObjects())
                {
                    if (rootObj.GetComponent(freeLookType) != null)
                    {
                        freeLookObj = rootObj;
                        break;
                    }
                    // Also search children
                    if (rootObj.GetComponentInChildren(freeLookType) != null)
                    {
                        freeLookObj = rootObj.GetComponentInChildren(freeLookType).gameObject;
                        break;
                    }
                }

                // If no FreeLook camera exists, create one
                if (freeLookObj == null)
                {
                    freeLookObj = new GameObject("CM FreeLook");
                    SceneManager.MoveGameObjectToScene(freeLookObj, scene);
                    freeLookObj.AddComponent(freeLookType);
                }

                // Set Follow and LookAt targets via reflection (types may vary)
                Component freeLookComponent = freeLookObj.GetComponent(freeLookType);
                if (freeLookComponent != null)
                {
                    var followProp = freeLookType.GetProperty("Follow");
                    if (followProp != null)
                        followProp.SetValue(freeLookComponent, playerInstance.transform);

                    var lookAtProp = freeLookType.GetProperty("LookAt");
                    if (lookAtProp != null)
                        lookAtProp.SetValue(freeLookComponent, cameraTarget);
                }

                Debug.Log("[NexusArena] Cinemachine FreeLook camera configured to follow PlayerRobot.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NexusArena] Could not set up Cinemachine FreeLook: {e.Message}");
            }
        }

        // =====================================================================
        // Utility
        // =====================================================================
        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
                string folder = Path.GetFileName(path);
                if (parent != null)
                {
                    AssetDatabase.CreateFolder(parent, folder);
                    Debug.Log($"[NexusArena] Created folder: {path}");
                }
            }
        }

        private static void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }
}
