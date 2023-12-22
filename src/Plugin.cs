using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.IO;
using System;

namespace VogueHeads
{   
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]

    public class Plugin : BaseUnityPlugin {

        private static AssetBundle animationBundle;

        private const string bundleName = "voguebundle";

        private void Awake() {
            LoadAssets();
            if (animationBundle != null) {
                SpringManAIPatch.Init(Logger, animationBundle);
                var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
                harmony.PatchAll(typeof(SpringManAIPatch));
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} ({PluginInfo.PLUGIN_VERSION}) is loaded!");
            } else {
                Logger.LogWarning("Failed to load assets, skipping.");
            }
        }

        private void LoadAssets() {
			var pluginDirectory = ((BaseUnityPlugin)this).Info.Location;
			try
			{
				animationBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(pluginDirectory), bundleName));
			}
			catch (Exception ex)
			{
				Logger.LogError("Couldn't load asset bundle: " + ex.Message);
			}
        }
    }
}