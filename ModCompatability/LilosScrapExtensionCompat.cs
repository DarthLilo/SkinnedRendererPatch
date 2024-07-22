using System.Runtime.CompilerServices;
using HarmonyLib;
using LilosScrapExtension.Scripts;
using SkinnedRendererPatch.Patches;
using UnityEngine;

namespace SkinnedRendererPatch.ModCompatability;

public static class LilosScrapExtensionCompat
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void ApplyCustomLSEAData(GrabbableObject component, bool curTriggerState)
    {
        // Apply custom LSEA data

        var collected_scrap_trigger = component.gameObject.GetComponent<CollectedScrapTrigger>();
        var mesh_filter = component.gameObject.GetComponent<MeshFilter>();
        var mesh_render = component.gameObject.GetComponent<MeshRenderer>();
        if (collected_scrap_trigger != null)
        {
            collected_scrap_trigger.Triggered = curTriggerState;
            Mesh newMesh = collected_scrap_trigger.newMesh;
            Material newMaterial = collected_scrap_trigger.newMaterial;

            if (mesh_filter != null && newMesh != null)
            {
                mesh_filter.mesh = newMesh;
            }

            if (mesh_render != null && newMaterial != null)
            {
                mesh_render.sharedMaterial = newMaterial;
            }

            foreach (var child in ItemStateSaving.GetSkinnedChildren(component.gameObject.transform))
            {
                if (newMesh != null)
                {
                    child.GetComponent<SkinnedMeshRenderer>().sharedMesh = newMesh;
                }

                if (newMaterial != null)
                {
                    child.GetComponent<SkinnedMeshRenderer>().sharedMaterial = newMaterial;
                }
            }

        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static bool DetermineTriggered(GrabbableObject component)
    {
        bool stateToAdd = false;
        var collected_scrap_trigger = component.gameObject.GetComponent<CollectedScrapTrigger>();
        if (collected_scrap_trigger != null)
        {

            if (collected_scrap_trigger.Triggered)
            {
                stateToAdd = true;
            } else {
                stateToAdd = false;
            }
        } else {
            stateToAdd = false;
        }
        return stateToAdd;
    }
}