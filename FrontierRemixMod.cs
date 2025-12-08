global using BTD_Mod_Helper.Extensions;
using System;
using MelonLoader;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using FrontierRemix;
using BTD_Mod_Helper.Api.ModOptions;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Data.Legends;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Menu;
using Il2CppAssets.Scripts.Unity.UI_New.Legends;
using Il2CppAssets.Scripts.Unity.UI_New.Popups;
using Il2CppNinjaKiwi.Common.ResourceUtils;
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

    public static readonly ModSettingHotkey MapHotkey = new(KeyCode.M)
    {
        description = "Open up the Frontier Fast travel map from anywhere"
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
    /// Main hotkeys
    /// </summary>
    [HarmonyPatch(typeof(FrontierMap), nameof(FrontierMap.Update))]
    internal static class FrontierMap_Update
    {
        [HarmonyPostfix]
        internal static void Postfix(FrontierMap __instance)
        {
            if (MapHotkey.JustPressed() && !PopupScreen.instance.IsPopupActive())
            {
                if (MenuManager.instance.IsMenuOpenOrOpening("FrontierFastTravelUI"))
                {
                    MenuManager.instance.ForceCloseMenu("FrontierFastTravelUI", true);
                }
                else
                {
                    MenuManager.instance.OpenMenu("FrontierFastTravelUI");
                }
                Game.instance.audioFactory.PlaySound(
                    new AudioClipReference(VanillaAudioClips.BuildingEnterNoticeboard01), "EnterBuildingSound",
                    volume: 0.5f);
            }

            __instance.lootDuration = 2 / Math.Max(Speed, 1);

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
            if (!FreeCamKey.JustPressed() || PopupScreen.instance.IsPopupActive()) return;

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

    private static bool autoFish;

    /// <summary>
    /// Add Auto Fish toggle
    /// </summary>
    [HarmonyPatch(typeof(FrontierFishingMinigame), nameof(FrontierFishingMinigame.Awake))]
    internal static class FrontierFishingMinigame_Awake
    {
        [HarmonyPostfix]
        internal static void Postfix(FrontierFishingMinigame __instance)
        {
            var toggle = __instance.gameObject.AddModHelperComponent(ModHelperCheckbox.Create(new Info("AutoFish", 150)
            {
                X = -175, Y = 175, Anchor = new Vector2(1, 0)
            }, autoFish, VanillaSprites.FrontierMainPanelBlack, new Action<bool>(b =>
            {
                autoFish = b;
            })));
            toggle.AddText(new Info("Label", 300, 100)
            {
                Y = -36, Anchor = new Vector2(0.5f, 0),
            }, "Auto Fish", 60);
        }
    }

    /// <summary>
    /// Auto Fish collecting fish
    /// </summary>
    [HarmonyPatch(typeof(FrontierFishingMinigame), nameof(FrontierFishingMinigame.Update))]
    internal static class FrontierFishingMinigame_Update
    {

        [HarmonyPrefix]
        internal static void Prefix(FrontierFishingMinigame __instance)
        {
            if (autoFish)
            {
                __instance.hitBtnHeld = __instance.targetPos > __instance.t;

                InputSystemController_GetMouseButtonDown.overrideMouse = __instance.map.frontierMap.lootSequenceRunning;
            }
        }
    }

    /// <summary>
    /// Auto Fish restarting fishing
    /// </summary>
    [HarmonyPatch(typeof(FrontierFishingMinigame), nameof(FrontierFishingMinigame.IdleState))]
    internal static class FrontierFishingMinigame_IdleState
    {
        [HarmonyPostfix]
        internal static void Postfix(FrontierFishingMinigame __instance)
        {
            if (autoFish)
            {
                TaskScheduler.ScheduleTask(__instance.StartWaitingAsync, ScheduleType.WaitForSecondsScaled, 1);
            }
        }
    }

    /// <summary>
    /// Auto Fish console message
    /// </summary>
    [HarmonyPatch(typeof(FrontierFishLootDisplay), nameof(FrontierFishLootDisplay.Bind))]
    internal static class FrontierFishLootDisplay_Bind
    {
        [HarmonyPostfix]
        internal static void Postfix(FrontierLoot loot)
        {
            if (autoFish && loot.Is(out FrontierFishingLoot fishingLoot))
            {
                ModHelper.Msg<FrontierRemixMod>(
                    $"Caught {fishingLoot.GetLootNameLocKey().Localize()}! Weight: {fishingLoot.weight:F2} Value: {fishingLoot.value}");
            }
        }
    }

    /// <summary>
    /// Auto Fish skip loot display
    /// </summary>
    [HarmonyPatch(typeof(InputSystemController), nameof(InputSystemController.GetMouseButtonDown))]
    internal static class InputSystemController_GetMouseButtonDown
    {
        internal static bool overrideMouse;

        [HarmonyPostfix]
        internal static void Postfix(ref bool __result)
        {
            if (autoFish && overrideMouse)
            {
                __result = true;
            }
            overrideMouse = false;
        }
    }
}