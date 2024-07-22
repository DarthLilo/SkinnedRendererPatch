using HarmonyLib;
using SkinnedRendererPatch.Helpers;
using Unity.Netcode;

namespace SkinnedRendererPatch.Patches;

[HarmonyPatch(typeof(GameNetworkManager))]
public class GameNetworkManagerPatch
{

    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    private static void StartPatch(GameNetworkManager __instance)
    {
        __instance.gameObject.AddComponent<SRPNetworkHelper>();
        __instance.gameObject.AddComponent<NetworkObject>();
        SkinnedRendererPatch.Logger.LogInfo("Network Helper Added!");
    }
}