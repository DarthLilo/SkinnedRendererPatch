using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using SkinnedRendererPatch.Patches;
using UnityEngine;

namespace SkinnedRendererPatch;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("DarthLilo.LilosScrapExtension", BepInDependency.DependencyFlags.SoftDependency)]
public class SkinnedRendererPatch : BaseUnityPlugin
{
    public static SkinnedRendererPatch Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    public static bool LilosScrapExtensionPresent;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} has started, patching gamecode");

        LilosScrapExtensionPresent = IsPluginInstalled("DarthLilo.LilosScrapExtension");

        Patch();
        
        // NETCODE PATCHING STUFF

        Logger.LogInfo($"Running netcode patchers");

        var types = Assembly.GetExecutingAssembly().GetTypes();
           foreach (var type in types)
           {
                //Logger.LogInfo($"Type: {type}");
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {   
                    //Logger.LogInfo($"Method: {method}");
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        //Logger.LogInfo($"Invoking {method}");
                        method.Invoke(null, null);
                    }
                }
           }
        
        // NETCODE PATCHING STUFF

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");

        Logger.LogInfo($"If you see errors relating to objects not containing a grabbable object during level loading, these are safe to ignore and won't affect gameplay");
    }

    private static bool IsPluginInstalled(string targetPlugin)
    {
        return Chainloader.PluginInfos.ContainsKey(targetPlugin);
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }
}
