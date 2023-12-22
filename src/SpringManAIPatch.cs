using System;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using Random = System.Random;

namespace VogueHeads
{
    [HarmonyPatch(typeof(SpringManAI))]
    internal class SpringManAIPatch : MonoBehaviour {

        private const string assetName = "assets/animatorcontroller/animcontainer_14.controller";

        private const string animatorProp = "stopPose";

        private static ManualLogSource logger { get; set; }
        
        public static AssetBundle bundle;

        public static RuntimeAnimatorController voguePoses;

	    public static AudioClip[] stopNoises;

        static AccessTools.FieldRef<SpringManAI, Animator> creatureAnimatorRef =
            AccessTools.FieldRefAccess<SpringManAI, Animator>("creatureAnimator");

        static AccessTools.FieldRef<SpringManAI, bool> hasStoppedRef =
            AccessTools.FieldRefAccess<SpringManAI, bool>("hasStopped");

        static AccessTools.FieldRef<SpringManAI, AnimationStopPoints> animStopPointsRef =
            AccessTools.FieldRefAccess<SpringManAI, AnimationStopPoints>("animStopPoints");

        public static void Init(ManualLogSource logger, AssetBundle bundle) {
            SpringManAIPatch.logger = logger;
            SpringManAIPatch.bundle = bundle;
            try {
                voguePoses = bundle.LoadAsset<RuntimeAnimatorController>(assetName);
                if (voguePoses != null) {
				    logger.LogInfo($"Loaded poses: {assetName}");
                } else {
				    logger.LogWarning($"Failed to load poses: {assetName}");
                }
            }
			catch (Exception ex)
			{
				logger.LogError("Couldn't load animator: " + ex.Message);
			}
        }

        [HarmonyPostfix]
        [HarmonyPatch("__initializeVariables")]
        public static void patchAnimator(SpringManAI __instance) {
            if (voguePoses != null) {
				logger.LogInfo("Replacing animator on coilhead");

                Animator animator = ((Component)((object)__instance)).transform.Find("SpringManModel").Find("AnimContainer").gameObject.GetComponent<Animator>();
                if (animator != null) {
                    animator.runtimeAnimatorController = voguePoses;
				    logger.LogInfo("Replaced animator on coilhead");
                } else {
                    logger.LogWarning("Couldn't find animator");
                }
			} else {
				logger.LogWarning("Vogue poses were not found. Cannot replace animator.");
            }
        }

        internal struct State {
            internal bool HasStopped;
        }

        // using a pre and post with __state to watch change on `hasStopped` property
        [HarmonyPatch(typeof(SpringManAI), nameof(SpringManAI.Update))]
        [HarmonyPrefix]
        public static void PreUpdate(SpringManAI __instance, ref State __state) {
            __state.HasStopped = hasStoppedRef(__instance);
        }

        [HarmonyPatch(typeof(SpringManAI), nameof(SpringManAI.Update))]
        [HarmonyPostfix]
        public static void PostUpdate(SpringManAI __instance, ref State __state) {
            bool shouldStop = hasStoppedRef(__instance);
            if (__state.HasStopped != shouldStop) {
                if (shouldStop) {
				    logger.LogInfo("animator must stop");
                    AnimationStopPoints whichSet = animStopPointsRef(__instance);
                    // 5 animation clips
                    var pose = 1 + (2 * (new Random()).Next(5)) + whichSet.animationPosition;
                    creatureAnimatorRef(__instance).SetInteger(animatorProp, pose);
                } else {
				    logger.LogInfo("animator must start");
                    creatureAnimatorRef(__instance).SetInteger(animatorProp, 0);
                }
            }
        }
    }
}