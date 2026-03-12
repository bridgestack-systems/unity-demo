using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NexusArena.Editor
{
    public static class SceneGenerator
    {
        private const string ScenesPath = "Assets/Scenes";

        [MenuItem("NexusArena/Generate All Scenes")]
        public static void GenerateAllScenes()
        {
            EnsureDirectoryExists(ScenesPath);

            CreateMainMenuScene();
            CreateGameArenaScene();
            CreateLobbyScene();
            CreateARScene();
            CreateVRScene();

            UpdateBuildSettings();

            AssetDatabase.Refresh();
            Debug.Log("[NexusArena] All scenes generated successfully.");
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
                string folder = Path.GetFileName(path);
                if (parent != null)
                {
                    AssetDatabase.CreateFolder(parent, folder);
                }
            }
        }

        private static Scene NewCleanScene()
        {
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void SaveScene(Scene scene, string name)
        {
            string path = $"{ScenesPath}/{name}.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[NexusArena] Created scene: {path}");
        }

        private static readonly Color CyanAccent = new Color(0f, 0.9f, 1f, 1f);
        private static readonly Color DarkBg = new Color(0.05f, 0.05f, 0.12f, 0.9f);
        private static readonly Color ButtonBg = new Color(0.15f, 0.17f, 0.3f, 1f);
        private static readonly Color ButtonHover = new Color(0.05f, 0.4f, 0.55f, 1f);
        private static readonly Color ButtonPressed = new Color(0f, 0.7f, 0.85f, 1f);

        private static void CreateMainMenuScene()
        {
            Scene scene = NewCleanScene();

            // Camera looking at the platform
            GameObject camera = new GameObject("Main Camera");
            var cam = camera.AddComponent<Camera>();
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.05f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camera.AddComponent<AudioListener>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 5f, -10f);
            camera.transform.LookAt(Vector3.zero);

            CreateDirectionalLight();

            // --- Canvas ---
            GameObject canvas = CreateCanvas("MainMenuCanvas");
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Background overlay — not a raycast target so buttons remain clickable
            GameObject bgObj = CreateUIChild(canvas, "Background");
            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.02f, 0.02f, 0.06f, 0.85f);
            bgImage.raycastTarget = false;
            StretchFill(bgObj);

            // --- Title Panel ---
            GameObject titlePanel = CreateUIChild(canvas, "TitlePanel");
            var titleRect = titlePanel.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.75f);
            titleRect.anchorMax = new Vector2(0.5f, 0.95f);
            titleRect.sizeDelta = new Vector2(800, 150);
            titleRect.anchoredPosition = Vector2.zero;

            // Title text
            GameObject titleObj = CreateUIChild(titlePanel, "TitleText");
            StretchFill(titleObj);
            AddUIText(titleObj, "NEXUS ARENA", 72, CyanAccent, FontStyle.Bold, TextAnchor.MiddleCenter);

            // Subtitle
            GameObject subtitleObj = CreateUIChild(canvas, "SubtitleText");
            var subRect = subtitleObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 0.68f);
            subRect.anchorMax = new Vector2(0.5f, 0.75f);
            subRect.sizeDelta = new Vector2(600, 50);
            subRect.anchoredPosition = Vector2.zero;
            AddUIText(subtitleObj, "MULTIPLAYER ARENA COMBAT", 22, new Color(0.5f, 0.6f, 0.7f, 1f),
                FontStyle.Normal, TextAnchor.MiddleCenter);

            // --- Button Panel ---
            GameObject buttonPanel = CreateUIChild(canvas, "ButtonPanel");
            var bpRect = buttonPanel.GetComponent<RectTransform>();
            bpRect.anchorMin = new Vector2(0.5f, 0.15f);
            bpRect.anchorMax = new Vector2(0.5f, 0.65f);
            bpRect.sizeDelta = new Vector2(360, 500);
            bpRect.anchoredPosition = Vector2.zero;

            var layout = buttonPanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(20, 20, 10, 10);

            // Buttons
            GameObject playBtn = CreateMenuButton(buttonPanel, "PlayButton", "PLAY");
            GameObject multiBtn = CreateMenuButton(buttonPanel, "MultiplayerButton", "MULTIPLAYER");
            GameObject arBtn = CreateMenuButton(buttonPanel, "ARModeButton", "AR MODE");
            GameObject vrBtn = CreateMenuButton(buttonPanel, "VRModeButton", "VR MODE");
            GameObject settingsBtn = CreateMenuButton(buttonPanel, "SettingsButton", "SETTINGS");
            GameObject quitBtn = CreateMenuButton(buttonPanel, "QuitButton", "QUIT");

            // --- Settings Panel (hidden by default) ---
            GameObject settingsPanel = CreateUIChild(canvas, "SettingsPanel");
            var spRect = settingsPanel.GetComponent<RectTransform>();
            spRect.anchorMin = new Vector2(0.5f, 0.5f);
            spRect.anchorMax = new Vector2(0.5f, 0.5f);
            spRect.sizeDelta = new Vector2(500, 400);
            spRect.anchoredPosition = Vector2.zero;
            var spBg = settingsPanel.AddComponent<Image>();
            spBg.color = new Color(0.06f, 0.06f, 0.14f, 0.95f);

            // Settings title
            GameObject settingsTitleObj = CreateUIChild(settingsPanel, "SettingsTitle");
            var stRect = settingsTitleObj.GetComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0f, 0.85f);
            stRect.anchorMax = new Vector2(1f, 1f);
            stRect.offsetMin = new Vector2(20, 0);
            stRect.offsetMax = new Vector2(-20, -10);
            AddUIText(settingsTitleObj, "SETTINGS", 32, CyanAccent, FontStyle.Bold, TextAnchor.MiddleCenter);

            // Placeholder text
            GameObject settingsBody = CreateUIChild(settingsPanel, "SettingsBody");
            var sbRect = settingsBody.GetComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(0.1f, 0.25f);
            sbRect.anchorMax = new Vector2(0.9f, 0.8f);
            sbRect.offsetMin = Vector2.zero;
            sbRect.offsetMax = Vector2.zero;
            AddUIText(settingsBody, "Audio Volume\nGraphics Quality\nMouse Sensitivity\n\n(Coming Soon)",
                20, new Color(0.6f, 0.6f, 0.7f, 1f), FontStyle.Normal, TextAnchor.UpperCenter);

            // Close button
            GameObject closeBtn = CreateMenuButton(settingsPanel, "CloseButton", "CLOSE");
            var cbRect = closeBtn.GetComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.5f, 0f);
            cbRect.anchorMax = new Vector2(0.5f, 0f);
            cbRect.pivot = new Vector2(0.5f, 0f);
            cbRect.anchoredPosition = new Vector2(0, 20);
            cbRect.sizeDelta = new Vector2(200, 50);
            // Wire close button to hide settings panel (persistent listener survives serialization)
            UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(
                closeBtn.GetComponent<Button>().onClick,
                new UnityEngine.Events.UnityAction<bool>(settingsPanel.SetActive),
                false);

            settingsPanel.SetActive(false);

            // --- Credits Panel (hidden) ---
            GameObject creditsPanel = CreateUIChild(canvas, "CreditsPanel");
            creditsPanel.SetActive(false);

            // --- MainMenuController ---
            var menuController = canvas.AddComponent<UI.MainMenuController>();
            var so = new SerializedObject(menuController);
            SetSerializedRef(so, "playButton", playBtn.GetComponent<Button>());
            SetSerializedRef(so, "multiplayerButton", multiBtn.GetComponent<Button>());
            SetSerializedRef(so, "arModeButton", arBtn.GetComponent<Button>());
            SetSerializedRef(so, "vrModeButton", vrBtn.GetComponent<Button>());
            SetSerializedRef(so, "settingsButton", settingsBtn.GetComponent<Button>());
            SetSerializedRef(so, "quitButton", quitBtn.GetComponent<Button>());
            // titleText is TMP_Text on the controller but we use legacy Text — skip wiring it
            // The controller null-checks it, so it's safe to leave unassigned
            SetSerializedRef(so, "settingsPanel", settingsPanel);
            // Set correct scene names to match actual scene files
            SetSerializedString(so, "gameScene", "GameArena");
            SetSerializedString(so, "multiplayerScene", "Lobby");
            SetSerializedString(so, "arScene", "ARScene");
            SetSerializedString(so, "vrScene", "VRScene");
            so.ApplyModifiedPropertiesWithoutUndo();

            // --- Version text ---
            GameObject versionObj = CreateUIChild(canvas, "VersionText");
            var verRect = versionObj.GetComponent<RectTransform>();
            verRect.anchorMin = new Vector2(1f, 0f);
            verRect.anchorMax = new Vector2(1f, 0f);
            verRect.pivot = new Vector2(1f, 0f);
            verRect.sizeDelta = new Vector2(200, 30);
            verRect.anchoredPosition = new Vector2(-15, 10);
            AddUIText(versionObj, "v0.1.0", 16, new Color(0.4f, 0.4f, 0.5f, 1f),
                FontStyle.Normal, TextAnchor.LowerRight);

            CreateEventSystem();

            // Preview platform
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platform.name = "RotatingPlatformPreview";
            platform.transform.position = Vector3.zero;
            platform.transform.localScale = new Vector3(3f, 0.2f, 3f);

            SaveScene(scene, "MainMenu");
        }

        private static GameObject CreateMenuButton(GameObject parent, string name, string label)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent.transform, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320, 55);

            var image = btnObj.AddComponent<Image>();
            image.color = ButtonBg;

            // Add a visible border via Outline
            var outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.6f, 0.8f, 0.5f);
            outline.effectDistance = new Vector2(2, -2);

            var button = btnObj.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = ButtonBg;
            colors.highlightedColor = ButtonHover;
            colors.pressedColor = ButtonPressed;
            colors.selectedColor = ButtonHover;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.targetGraphic = image;

            // Button text using built-in UI Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            StretchFill(textObj);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            AddUIText(textObj, label, 26, new Color(0.85f, 0.9f, 1f, 1f),
                FontStyle.Bold, TextAnchor.MiddleCenter);

            return btnObj;
        }

        private static void AddUIText(GameObject obj, string text, int fontSize, Color color,
            FontStyle style, TextAnchor alignment)
        {
            var uiText = obj.AddComponent<Text>();
            uiText.text = text;
            uiText.fontSize = fontSize;
            uiText.color = color;
            uiText.fontStyle = style;
            uiText.alignment = alignment;
            uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static GameObject CreateUIChild(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            child.AddComponent<RectTransform>();
            return child;
        }

        private static void StretchFill(GameObject obj)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect == null) rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetSerializedRef(SerializedObject so, string propName, Object value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
                prop.objectReferenceValue = value;
        }

        private static void SetSerializedString(SerializedObject so, string propName, string value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
                prop.stringValue = value;
        }

        private static void CreateGameArenaScene()
        {
            Scene scene = NewCleanScene();

            // Main Camera
            GameObject camera = new GameObject("Main Camera");
            var cam = camera.AddComponent<Camera>();
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.05f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camera.AddComponent<AudioListener>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 10f, -8f);
            camera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);

            CreateDirectionalLight();

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10f, 1f, 10f);

            CreateSpawnPoint("SpawnPoint_NW", new Vector3(-40f, 1f, 40f));
            CreateSpawnPoint("SpawnPoint_NE", new Vector3(40f, 1f, 40f));
            CreateSpawnPoint("SpawnPoint_SW", new Vector3(-40f, 1f, -40f));
            CreateSpawnPoint("SpawnPoint_SE", new Vector3(40f, 1f, -40f));

            // Playable box — large, uses existing material if available
            GameObject playerBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playerBox.name = "PlayerBox";
            playerBox.transform.position = new Vector3(0f, 1.5f, 0f);
            playerBox.transform.localScale = new Vector3(3f, 3f, 3f);
            // Try to load the PlayerAccent material created by VisualPolishSetup
            var accentMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PlayerAccent.mat");
            if (accentMat == null)
                accentMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PhysicsBall.mat");
            if (accentMat != null)
                playerBox.GetComponent<Renderer>().sharedMaterial = accentMat;
            playerBox.AddComponent<Player.SimpleBoxController>();

            GameObject playerSpawnRef = new GameObject("PlayerPrefabReference");
            playerSpawnRef.transform.position = Vector3.zero;

            GameObject hudCanvas = CreateCanvas("HUDCanvas");
            CreateEmptyChild(hudCanvas, "ScoreDisplay");
            CreateEmptyChild(hudCanvas, "TimerDisplay");
            CreateEmptyChild(hudCanvas, "HealthBar");
            CreateEmptyChild(hudCanvas, "Crosshair");

            CreateEventSystem();

            GameObject gameManagerObj = new GameObject("GameManager");
            gameManagerObj.AddComponent<Core.GameManager>();

            GameObject audioManagerObj = new GameObject("AudioManager");
            audioManagerObj.AddComponent<Core.AudioManager>();

            SaveScene(scene, "GameArena");
        }

        private static void CreateLobbyScene()
        {
            Scene scene = NewCleanScene();

            GameObject camera = new GameObject("Main Camera");
            camera.AddComponent<Camera>();
            camera.AddComponent<AudioListener>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 2f, -5f);

            CreateDirectionalLight();

            GameObject canvas = CreateCanvas("LobbyCanvas");
            CreateEmptyChild(canvas, "PlayerListPanel");
            CreateEmptyChild(canvas, "ChatPanel");
            CreateEmptyChild(canvas, "ReadyPanel");
            CreateEmptyChild(canvas, "RoomCodeDisplay");

            CreateEventSystem();

            GameObject networkManager = new GameObject("NetworkManager");
            try
            {
                var netManagerType = System.Type.GetType("Unity.Netcode.NetworkManager, Unity.Netcode.Runtime");
                if (netManagerType != null)
                {
                    networkManager.AddComponent(netManagerType);
                }
            }
            catch
            {
                // Netcode package may not be available
            }

            SaveScene(scene, "Lobby");
        }

        private static void CreateARScene()
        {
            Scene scene = NewCleanScene();

#if UNITY_HAS_ARFOUNDATION || true
            try
            {
                GameObject arSession = new GameObject("AR Session");
                var arSessionType = System.Type.GetType("UnityEngine.XR.ARFoundation.ARSession, Unity.XR.ARFoundation");
                if (arSessionType != null)
                {
                    arSession.AddComponent(arSessionType);
                }

                GameObject arSessionOrigin = new GameObject("AR Session Origin");
                var arOriginType = System.Type.GetType("UnityEngine.XR.ARFoundation.ARSessionOrigin, Unity.XR.ARFoundation");
                if (arOriginType != null)
                {
                    arSessionOrigin.AddComponent(arOriginType);
                }

                var arPlaneManagerType = System.Type.GetType("UnityEngine.XR.ARFoundation.ARPlaneManager, Unity.XR.ARFoundation");
                if (arPlaneManagerType != null && arSessionOrigin != null)
                {
                    arSessionOrigin.AddComponent(arPlaneManagerType);
                }

                GameObject arCamera = new GameObject("AR Camera");
                arCamera.transform.SetParent(arSessionOrigin.transform);
                arCamera.AddComponent<Camera>();
                arCamera.AddComponent<AudioListener>();
                arCamera.tag = "MainCamera";

                var arCameraBgType = System.Type.GetType("UnityEngine.XR.ARFoundation.ARCameraBackground, Unity.XR.ARFoundation");
                if (arCameraBgType != null)
                {
                    arCamera.AddComponent(arCameraBgType);
                }

                var arCameraManagerType = System.Type.GetType("UnityEngine.XR.ARFoundation.ARCameraManager, Unity.XR.ARFoundation");
                if (arCameraManagerType != null)
                {
                    arCamera.AddComponent(arCameraManagerType);
                }
            }
            catch
            {
                GameObject fallbackCamera = new GameObject("Main Camera");
                fallbackCamera.AddComponent<Camera>();
                fallbackCamera.AddComponent<AudioListener>();
                fallbackCamera.tag = "MainCamera";
            }
#endif

            SaveScene(scene, "ARScene");
        }

        private static void CreateVRScene()
        {
            Scene scene = NewCleanScene();

            CreateDirectionalLight();

            try
            {
                GameObject xrOrigin = new GameObject("XR Origin");
                var xrOriginType = System.Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");
                if (xrOriginType != null)
                {
                    xrOrigin.AddComponent(xrOriginType);
                }

                GameObject cameraOffset = new GameObject("Camera Offset");
                cameraOffset.transform.SetParent(xrOrigin.transform);
                cameraOffset.transform.localPosition = Vector3.zero;

                GameObject xrCamera = new GameObject("XR Camera");
                xrCamera.transform.SetParent(cameraOffset.transform);
                xrCamera.AddComponent<Camera>();
                xrCamera.AddComponent<AudioListener>();
                xrCamera.tag = "MainCamera";

                GameObject leftHand = new GameObject("LeftHand Controller");
                leftHand.transform.SetParent(xrOrigin.transform);
                leftHand.transform.localPosition = new Vector3(-0.2f, 1.2f, 0.3f);

                GameObject rightHand = new GameObject("RightHand Controller");
                rightHand.transform.SetParent(xrOrigin.transform);
                rightHand.transform.localPosition = new Vector3(0.2f, 1.2f, 0.3f);

                var controllerType = System.Type.GetType(
                    "UnityEngine.XR.Interaction.Toolkit.XRController, Unity.XR.Interaction.Toolkit");
                if (controllerType != null)
                {
                    leftHand.AddComponent(controllerType);
                    rightHand.AddComponent(controllerType);
                }
            }
            catch
            {
                GameObject fallbackCamera = new GameObject("Main Camera");
                fallbackCamera.AddComponent<Camera>();
                fallbackCamera.AddComponent<AudioListener>();
                fallbackCamera.tag = "MainCamera";
            }

            GameObject vrGround = GameObject.CreatePrimitive(PrimitiveType.Plane);
            vrGround.name = "Ground";
            vrGround.transform.position = Vector3.zero;
            vrGround.transform.localScale = new Vector3(5f, 1f, 5f);

            SaveScene(scene, "VRScene");
        }

        private static void UpdateBuildSettings()
        {
            string[] sceneNames = { "MainMenu", "GameArena", "Lobby", "ARScene", "VRScene" };
            EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[sceneNames.Length];

            for (int i = 0; i < sceneNames.Length; i++)
            {
                string path = $"{ScenesPath}/{sceneNames[i]}.unity";
                buildScenes[i] = new EditorBuildSettingsScene(path, true);
            }

            EditorBuildSettings.scenes = buildScenes;
        }

        private static GameObject CreateDirectionalLight()
        {
            GameObject light = new GameObject("Directional Light");
            Light lightComp = light.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.color = new Color(1f, 0.956f, 0.839f);
            lightComp.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            return light;
        }

        private static void CreateSpawnPoint(string name, Vector3 position)
        {
            GameObject spawnPoint = new GameObject(name);
            spawnPoint.transform.position = position;
            spawnPoint.tag = "SpawnPoint";
        }

        private static GameObject CreateCanvas(string name)
        {
            GameObject canvasObj = new GameObject(name);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            return canvasObj;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();

            try
            {
                var inputModuleType = System.Type.GetType(
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null)
                {
                    var module = eventSystem.AddComponent(inputModuleType);
                    // AssignDefaultActions sets up Point, Click, ScrollWheel, etc.
                    var assignMethod = inputModuleType.GetMethod("AssignDefaultActions",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (assignMethod != null)
                    {
                        assignMethod.Invoke(module, null);
                    }
                    return;
                }
            }
            catch { }

            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private static GameObject CreateEmptyChild(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }
    }
}
