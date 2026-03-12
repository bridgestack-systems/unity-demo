using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NexusArena.Editor
{
    [InitializeOnLoad]
    public static class NexusArenaSetup
    {
        private const string SetupCompleteKey = "NexusArena_SetupComplete";
        private const string ScenesPath = "Assets/Scenes";

        private static readonly string[] RequiredTags = { "Player", "Destructible", "Interactable", "SpawnPoint" };

        private static readonly Dictionary<string, int> RequiredLayers = new()
        {
            { "Player", 8 },
            { "Interactable", 9 },
            { "Destructible", 10 },
            { "Projectile", 11 },
            { "UI", 12 },
            { "Environment", 13 }
        };

        static NexusArenaSetup()
        {
            EditorApplication.delayCall += RunSetup;
        }

        private static void RunSetup()
        {
            SetupTags();
            SetupLayers();
            ConfigureBuildSettings();

            if (!SessionState.GetBool(SetupCompleteKey, false))
            {
                SessionState.SetBool(SetupCompleteKey, true);

                if (!Directory.Exists(Path.Combine(Application.dataPath, "Scenes")))
                {
                    if (EditorUtility.DisplayDialog(
                            "Nexus Arena Setup",
                            "Welcome to Nexus Arena!\n\nScenes have not been generated yet. Would you like to generate them now?",
                            "Generate Scenes",
                            "Later"))
                    {
                        SceneGenerator.GenerateAllScenes();
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Nexus Arena",
                        "Welcome back to Nexus Arena!",
                        "OK");
                }
            }
        }

        private static void SetupTags()
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            foreach (string tag in RequiredTags)
            {
                if (TagExists(tagsProp, tag)) continue;

                int index = tagsProp.arraySize;
                tagsProp.InsertArrayElementAtIndex(index);
                tagsProp.GetArrayElementAtIndex(index).stringValue = tag;
            }

            tagManager.ApplyModifiedProperties();
        }

        private static bool TagExists(SerializedProperty tagsProp, string tag)
        {
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    return true;
            }
            return false;
        }

        private static void SetupLayers()
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            SerializedProperty layersProp = tagManager.FindProperty("layers");

            foreach (var kvp in RequiredLayers)
            {
                SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(kvp.Value);
                if (string.IsNullOrEmpty(layerProp.stringValue))
                {
                    layerProp.stringValue = kvp.Key;
                }
            }

            tagManager.ApplyModifiedProperties();
        }

        private static void ConfigureBuildSettings()
        {
            string[] sceneNames = { "MainMenu", "GameArena", "Lobby", "ARScene", "VRScene" };
            List<EditorBuildSettingsScene> scenes = new();

            foreach (string sceneName in sceneNames)
            {
                string scenePath = $"{ScenesPath}/{sceneName}.unity";
                if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scenePath)))
                {
                    scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                }
            }

            if (scenes.Count > 0)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
