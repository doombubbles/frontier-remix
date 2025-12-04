global using BTD_Mod_Helper.Extensions;
using System;
using MelonLoader;
using BTD_Mod_Helper;
using FrontierRemix;
using BTD_Mod_Helper.Api.ModOptions;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.Legends;
using UnityEngine;
using UnityEngine.EventSystems;

[assembly: MelonInfo(typeof(FrontierRemixMod), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace FrontierRemix;

public class FrontierRemixMod : BloonsTD6Mod
{
    public static readonly ModSettingBool LongRangePathFinding = new(true)
    {
        description = "Enables you to click and move in the Frontier Legends map at any range"
    };

    private static readonly ModSettingHotkey FreeCamKey = new(KeyCode.Space)
    {
        description = "Keybinding to toggle Free Cam mode in the Frontier Legends map."
    };

    private static readonly ModSettingHotkey FrontierSpeedReset = new(KeyCode.F1)
    {
    };


    private static readonly ModSettingFloat FrontierSpeed1 = new(1.5f)
    {
        min = 1,
        max = 10
    };

    private static readonly ModSettingHotkey FrontierSpeed1Key = new(KeyCode.F2)
    {
    };

    private static readonly ModSettingFloat FrontierSpeed2 = new(2f)
    {
        min = 1,
        max = 10
    };

    private static readonly ModSettingHotkey FrontierSpeed2Key = new(KeyCode.F3)
    {
    };

    private static readonly ModSettingFloat FrontierSpeed3 = new(3f)
    {
        min = 1,
        max = 10
    };

    private static readonly ModSettingHotkey FrontierSpeed3Key = new(KeyCode.F4)
    {
    };

    public static float Speed { get; private set; } = 1;

    /// <summary>
    /// Switch Speed on hotkey
    /// </summary>
    [HarmonyPatch(typeof(FrontierMap), nameof(FrontierMap.Update))]
    internal static class FrontierMap_Update
    {
        [HarmonyPostfix]
        internal static void Postfix()
        {
            try
            {
                if (FrontierSpeedReset.JustPressed() && !Mathf.Approximately(Speed, 1))
                {
                    Speed = 1;
                }
                else if (FrontierSpeed1Key.JustPressed() && !Mathf.Approximately(Speed, FrontierSpeed1))
                {
                    Speed = FrontierSpeed1;
                }
                else if (FrontierSpeed2Key.JustPressed() && !Mathf.Approximately(Speed, FrontierSpeed2))
                {
                    Speed = FrontierSpeed2;
                }
                else if (FrontierSpeed3Key.JustPressed() && !Mathf.Approximately(Speed, FrontierSpeed3))
                {
                    Speed = FrontierSpeed3;
                }
                else return;

                ModHelper.Msg<FrontierRemixMod>($"Frontier Speed is now {Speed}x");
            }
            finally
            {
                Time.timeScale = Speed;
            }
        }
    }

    /// <summary>
    /// Reset speed when leaving Frontier
    /// </summary>
    [HarmonyPatch(typeof(FrontierMapScreen), nameof(FrontierMapScreen.Close))]
    internal static class FrontierMapScreen_Close
    {
        [HarmonyPostfix]
        internal static void Postfix(FrontierMapScreen __instance)
        {
            Time.timeScale = 1;
        }
    }

    /// <summary>
    /// Switch Camera Type on hotkey
    /// </summary>
    [HarmonyPatch(typeof(LegendsMapCameraRig), nameof(LegendsMapCameraRig.Update))]
    internal static class LegendsMapCameraRig_Update
    {
        private const string FreeCam = "freeCam";
        private const string FollowCam = "followCam";

        [HarmonyPostfix]
        internal static void Postfix(LegendsMapCameraRig __instance)
        {
            if (!FreeCamKey.JustPressed()) return;

            switch (__instance.selectedCameraSettings?.settingsName)
            {
                case FreeCam:
                    __instance.ChangeSettings(FollowCam);
                    break;
                case FollowCam:
                    __instance.ChangeSettings(FreeCam);
                    break;
            }
        }
    }

    /// <summary>
    /// Enable long distance pathfinding
    /// </summary>
    [HarmonyPatch(typeof(LegendsTileNavigator), nameof(LegendsTileNavigator.UpdateMovement))]
    internal static class FrontierMonkeyMovement_Init
    {
        [HarmonyPrefix]
        internal static void Prefix(LegendsTileNavigator __instance)
        {
            if (!__instance.Is<FrontierMonkeyMovement>()) return;

            if (LongRangePathFinding)
            {
                __instance.longDistancePathFinding = true;
                __instance.pathfindingRange = 1000;
                __instance.maxPathfindingTries = 100;
                __instance.maxPathfindingTriesLong = 1000;
            }
        }
    }

    /// <summary>
    /// Make the click interactions ignore fast forward for their timing calculations
    /// </summary>
    [HarmonyPatch(typeof(LegendsMapInput), nameof(LegendsMapInput.GetMouseInput))]
    internal static class LegendsMapInput_GetMouseInput
    {
        [HarmonyPrefix]
        internal static void Prefix(LegendsMapInput __instance)
        {
            if (__instance.inputEnabled && !EventSystem.current.IsPointerOverGameObject())
            {
                __instance.clickTimer += Time.unscaledDeltaTime - Time.deltaTime;
                __instance.clickCooldownTimer += Time.unscaledDeltaTime - Time.deltaTime;
            }
        }
    }

    /// <summary>
    /// Walk to campfire if trail is already started
    /// </summary>
    [HarmonyPatch(typeof(FrontierMapScreen), nameof(FrontierMapScreen.CampfireHintButtonClicked))]
    internal static class FrontierMapScreen_CampfireHintButtonClicked
    {
        [HarmonyPrefix]
        internal static void Prefix(FrontierMapScreen __instance)
        {
            if (__instance.frontierMap.runningHighlightPathTask != null)
            {
                __instance.frontierMap.TryWalkToCampfire();
            }
        }
    }
}