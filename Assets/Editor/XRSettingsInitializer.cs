using UnityEditor;
using UnityEngine;

namespace NexusArena.Editor
{
    public static class XRSettingsInitializer
    {
        [MenuItem("NexusArena/Initialize XR Settings")]
        public static void Initialize()
        {
            // Force OpenXR to create its settings asset
            try
            {
                var openXRSettingsType = System.Type.GetType(
                    "UnityEditor.XR.OpenXR.OpenXRPackageSettings, Unity.XR.OpenXR.Editor");
                if (openXRSettingsType != null)
                {
                    var getOrCreate = openXRSettingsType.GetMethod("GetOrCreateInstance",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (getOrCreate != null)
                    {
                        getOrCreate.Invoke(null, null);
                        Debug.Log("[NexusArena] OpenXR settings initialized.");
                    }
                }

                // Also initialize XR General Settings
                var xrGeneralType = System.Type.GetType(
                    "UnityEngine.XR.Management.XRGeneralSettings, Unity.XR.Management");
                if (xrGeneralType != null)
                {
                    var settingsPath = "Assets/XR/XRGeneralSettings.asset";
                    if (!AssetDatabase.LoadAssetAtPath<ScriptableObject>(settingsPath))
                    {
                        if (!AssetDatabase.IsValidFolder("Assets/XR"))
                            AssetDatabase.CreateFolder("Assets", "XR");

                        var instance = ScriptableObject.CreateInstance(xrGeneralType);
                        AssetDatabase.CreateAsset(instance, settingsPath);
                        Debug.Log("[NexusArena] XR General Settings created.");
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[NexusArena] XR settings init: {e.Message}");
            }
        }
    }
}
