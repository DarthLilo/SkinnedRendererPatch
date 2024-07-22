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

    public static Assembly AssemblyLilosScrapExtension;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} has started, patching gamecode");

        AssemblyLilosScrapExtension = DynamicDependency("DarthLilo.LilosScrapExtension");

        Patch();
        
        // NETCODE PATCHING STUFF

        Logger.LogInfo($"Running netcode patchers");

        var types = Assembly.GetExecutingAssembly().GetTypes();
           foreach (var type in types)
           {
                Logger.LogInfo($"Type: {type}");
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {   
                    Logger.LogInfo($"Method: {method}");
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        Logger.LogInfo($"Invoking {method}");
                        method.Invoke(null, null);
                    }
                }
           }
        
        // NETCODE PATCHING STUFF

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    private static bool IsPluginInstalled(string targetPlugin)
    {
        return Chainloader.PluginInfos.ContainsKey(targetPlugin);
    }

    private static Assembly DynamicDependency(string targetPlugin)
    {   
        Assembly targetAssembly = null;

        if (!IsPluginInstalled(targetPlugin)) return targetAssembly;

        var pluginInfo = Chainloader.PluginInfos[targetPlugin];
        try
        {
            targetAssembly = Assembly.LoadFrom(pluginInfo.Location);
            Logger.LogInfo($"Loaded Soft Dependency {targetPlugin}!");
            return targetAssembly;
        } catch (FileNotFoundException) {
            Logger.LogInfo($"Unable to find {targetPlugin}, skipping");
            return targetAssembly;
        }

    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }
}
