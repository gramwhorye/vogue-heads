extern alias AudioModule;

using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VogueHeads
{
    [HarmonyPatch(typeof(SpringManAI))]
    internal class SpringManAIPatch : MonoBehaviour
    {

        private const string assetName = "assets/animatorcontroller/animcontainer_14.controller";

        private static string[] calloutNames = [
            "assets/audioclip/fierce.mp3",
            "assets/audioclip/fierce1.mp3",
            "assets/audioclip/slay.mp3",
            "assets/audioclip/work.mp3",
            "assets/audioclip/work2.mp3"
        ];

        private static string[] animatorTriggerPrefix = [
            "vogue-tilted",
            "vogue-trade",
            "vogue-duck",
            "vogue-serve",
            "dab"
        ];

        private static string[] stopPositionSuffix = [
            "-left",
            "-right"
        ];

        private static ManualLogSource logger { get; set; }

        public static AssetBundle bundle;

        public static RuntimeAnimatorController voguePoses;

        public static AudioClip[] callouts;

        static AccessTools.FieldRef<SpringManAI, Animator> creatureAnimatorRef =
            AccessTools.FieldRefAccess<SpringManAI, Animator>("creatureAnimator");

        static AccessTools.FieldRef<SpringManAI, bool> hasStoppedRef =
            AccessTools.FieldRefAccess<SpringManAI, bool>("hasStopped");

        static AccessTools.FieldRef<SpringManAI, AnimationStopPoints> animStopPointsRef =
            AccessTools.FieldRefAccess<SpringManAI, AnimationStopPoints>("animStopPoints");

        // coilhead will use the creatureVoice to play the spring sound
        static AccessTools.FieldRef<SpringManAI, AudioSource> creatureSFXRef = 
            AccessTools.FieldRefAccess<SpringManAI, AudioSource>("creatureSFX");

        public static void Init(ManualLogSource logger, AssetBundle bundle)
        {
            SpringManAIPatch.logger = logger;
            SpringManAIPatch.bundle = bundle;
            try
            {
                voguePoses = bundle.LoadAsset<RuntimeAnimatorController>(assetName);
                if (voguePoses != null)
                {
                    logger.LogInfo($"Loaded poses: {assetName}");
                }
                else
                {
                    logger.LogWarning($"Failed to load poses: {assetName}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Couldn't load animator: " + ex.Message);
            }
            if (callouts == null) {
                callouts = new AudioClip[calloutNames.Length];
            }
            var calloutIndex = 0;
            foreach (string calloutFileName in calloutNames)
            {
                try
                {
                    AudioClip callout = bundle.LoadAsset<AudioClip>(calloutFileName);
                    if (callout != null)
                    {
                        callouts[calloutIndex] = callout;
                        calloutIndex++;
                    }
                    else
                    {
                        logger.LogWarning($"Couldn't load ${calloutFileName}. Is it an Audio Clip?");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("Couldn't load audio clip: " + ex.Message);
                }
            }
            logger.LogInfo($"Loaded {callouts.Length} audio clips");
        }

        [HarmonyPostfix]
        [HarmonyPatch("__initializeVariables")]
        public static void patchAnimator(SpringManAI __instance)
        {
            if (voguePoses != null)
            {
                logger.LogInfo("Replacing animator on coilhead");

                Animator animator = ((Component)((object)__instance)).transform.Find("SpringManModel").Find("AnimContainer").gameObject.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = voguePoses;
                    logger.LogInfo("Replaced animator on coilhead");
                }
                else
                {
                    logger.LogWarning("Couldn't find animator");
                }
                logger.LogInfo($"There are currently {callouts.Length} callouts");
            }
            else
            {
                logger.LogWarning("Vogue poses were not found. Cannot replace animator.");
            }
        }

        internal struct State
        {
            internal bool HasStopped;
        }

        // using a pre and post with __state to watch change on `hasStopped` property
        [HarmonyPatch(typeof(SpringManAI), nameof(SpringManAI.Update))]
        [HarmonyPrefix]
        public static void PreUpdate(SpringManAI __instance, ref State __state)
        {
            __state.HasStopped = hasStoppedRef(__instance);
        }

        [HarmonyPatch(typeof(SpringManAI), nameof(SpringManAI.Update))]
        [HarmonyPostfix]
        public static void PostUpdate(SpringManAI __instance, ref State __state)
        {
            bool shouldStop = hasStoppedRef(__instance);
            if (__state.HasStopped != shouldStop && shouldStop)
            {
                try {
                    var poseSuffix = stopPositionSuffix[animStopPointsRef(__instance).animationPosition % 2];
                    var posePrefixIndex = Random.Range(0, animatorTriggerPrefix.Length);
                    var posePrefix = animatorTriggerPrefix[posePrefixIndex];
                    creatureAnimatorRef(__instance).SetTrigger($"{posePrefix}{poseSuffix}");
                }
                catch (Exception ex) {
                        logger.LogWarning($"Failed to set pose: {ex.Message}");
                }
                if (callouts.Length > 0) {
                    try {
                        AudioSource SFX = creatureSFXRef(__instance);
                        if (SFX != null) {
                            RoundManager.PlayRandomClip((AudioModule.UnityEngine.AudioSource)(object) SFX, (AudioModule.UnityEngine.AudioClip[])(object[]) callouts, false);
                        } else {
                            logger.LogWarning("Couldn't find sound effect source");
                        }
                    } catch (Exception ex) {
                        logger.LogWarning($"Failed to play audio clip: {ex.Message}");
                    }
                } else {
                    logger.LogWarning("There are no callout audio clips available");
                }
            }
        }
    }
}