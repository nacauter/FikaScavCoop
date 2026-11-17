using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using EFT;
using Fika.Core.UI.Custom;
using Fika.Core.UI.Models;
using HarmonyLib;

namespace FikaScavCoop;

[BepInPlugin("com.nemezis.fikascavcoop", "Fika Scav Coop", "1.0.3")]
[BepInDependency("com.fika.core")]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;

    private void Awake()
    {
        Log = Logger;

        if (AccessTools.Field(typeof(MatchMakerUIScript), "_matches") == null ||
            AccessTools.Field(typeof(MatchMakerUIScript), "RaidSettings") == null)
        {
            Log.LogError("Fika: expected fields not found — patch skipped (version mismatch?)");
            return;
        }

        new Harmony("com.nemezis.fikascavcoop").PatchAll();
        Log.LogInfo($"{Info.Metadata.Name} {Info.Metadata.Version} loaded!");
    }
}

[HarmonyPatch(typeof(MatchMakerUIScript), "RefreshUI")]
public static class MatchMakerRefreshUIPatch
{
    private static readonly FieldInfo MatchesField = AccessTools.Field(typeof(MatchMakerUIScript), "_matches");
    private static readonly FieldInfo RaidSettingsField = AccessTools.Field(typeof(MatchMakerUIScript), "RaidSettings");

    [HarmonyPrefix]
    public static void Prefix(MatchMakerUIScript __instance, out ESideType[] __state)
    {
        __state = null;
        try
        {
            var matches = MatchesField.GetValue(__instance) as LobbyEntry[];
            if (matches is not { Length: > 0 } ||
                RaidSettingsField.GetValue(__instance) is not RaidSettings raidSettings)
            {
                return;
            }

            __state = new ESideType[matches.Length];
            for (int i = 0; i < matches.Length; i++)
            {
                __state[i] = matches[i].Side;
                matches[i].Side = raidSettings.Side;
            }
        }
        catch (Exception e)
        {
            Plugin.Log?.LogError($"RefreshUI prefix failed: {e}");
        }
    }

    [HarmonyPostfix]
    public static void Postfix(MatchMakerUIScript __instance, ESideType[] __state)
    {
        if (__state == null)
        {
            return;
        }

        try
        {
            var matches = MatchesField.GetValue(__instance) as LobbyEntry[];
            if (matches == null || matches.Length != __state.Length)
            {
                return;
            }

            for (int i = 0; i < matches.Length; i++)
            {
                matches[i].Side = __state[i];
            }
        }
        catch (Exception e)
        {
            Plugin.Log?.LogError($"RefreshUI postfix failed: {e}");
        }
    }
}
