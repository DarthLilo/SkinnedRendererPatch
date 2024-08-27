using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace SkinnedRendererPatch.Patches;


[HarmonyPatch(typeof(RoundManager))]
[HarmonyPatch("SyncScrapValuesClientRpc")]
public class MeshRendererPatch
{
    public static bool Prefix(RoundManager __instance, ref NetworkObjectReference[] spawnedScrap)
    {   
        var ScrapValuesRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 210);
        for (int i = 0; i < spawnedScrap.Length; i++)
        {
            if (spawnedScrap[i].TryGet(out NetworkObject networkObject))
            {
                GrabbableObject component = networkObject.GetComponent<GrabbableObject>();
                if (component != null)
                {
                    try
                    {   
                        // Check for any mesh variants on the assigned gameobject
                        if (component.itemProperties.meshVariants.Length != 0)
                        {   
                            // Does it have a mesh filter? If so apply a new random mesh based on the level seed
                            var mesh_filter = component.gameObject.GetComponent<MeshFilter>();
                            if (mesh_filter != null)
                            {
                                component.gameObject.GetComponent<MeshFilter>().mesh = component.itemProperties.meshVariants[ScrapValuesRandom.Next(0, component.itemProperties.meshVariants.Length)];
                                SkinnedRendererPatch.Logger.LogDebug($"Changed {component.gameObject.name} material using MeshRenderer");
                                //ItemStateSaving.SaveItemState(component.gameObject,false);
                            }

                            // Create a list of gameobjects that are children of the main object
                            List<GameObject> objectChildren = [];
                            foreach (UnityEngine.Transform child in component.gameObject.transform)
                            {   
                                // Does the game object have a skinned mesh renderer, if so save it
                                if (child.gameObject.GetComponent<SkinnedMeshRenderer>() && !child.gameObject.GetComponent<ScanNodeProperties>() && !objectChildren.Contains(child.gameObject))
                                {   
                                    objectChildren.Add(child.gameObject);
                                }
                            }

                            // Apply a new random mesh based on the level seed for each child component with a skinned mesh renderer
                            foreach (var child in objectChildren)
                            {
                                var skinned_mesh_renderer = child.gameObject.GetComponent<SkinnedMeshRenderer>();
                                skinned_mesh_renderer.sharedMesh = component.itemProperties.meshVariants[ScrapValuesRandom.Next(0, component.itemProperties.meshVariants.Length)];
                                SkinnedRendererPatch.Logger.LogDebug($"Changed {child.gameObject.name} mesh using SkinnedMeshRenderer");
                                //ItemStateSaving.SaveItemState(component.gameObject,true);
                            }
                        }
                        // Check for any material variants on the assigned gameobject
                        if (component.itemProperties.materialVariants.Length != 0)
                        {
                            // Does it have a mesh renderer? If so apply the new material based off the level seed
                            var mesh_renderer = component.gameObject.GetComponent<MeshRenderer>();
                            if (mesh_renderer != null)
                            {
                                mesh_renderer.sharedMaterial = component.itemProperties.materialVariants[ScrapValuesRandom.Next(0, component.itemProperties.materialVariants.Length)];
                                SkinnedRendererPatch.Logger.LogDebug($"Changed {component.gameObject.name} material using MeshRenderer");
                                //ItemStateSaving.SaveItemState(component.gameObject,false);
                            }

                            // Create a list of gameobjects that are children of the main object
                            List<GameObject> objectChildren = [];
                            foreach (Transform child in component.gameObject.transform)
                            {
                                if (child.gameObject.GetComponent<SkinnedMeshRenderer>() && !child.gameObject.GetComponent<ScanNodeProperties>() && !objectChildren.Contains(child.gameObject))
                                {
                                    // Does the game object have a skinned mesh renderer, if so save it
                                    objectChildren.Add(child.gameObject);
                                }
                            }

                            // Apply a new random material based on the level seed for each child component with a skinned mesh renderer
                            foreach (var child in objectChildren)
                            {
                                var skinned_mesh_renderer = child.gameObject.GetComponent<SkinnedMeshRenderer>();
                                skinned_mesh_renderer.sharedMaterial = component.itemProperties.materialVariants[ScrapValuesRandom.Next(0, component.itemProperties.materialVariants.Length)];;
                                SkinnedRendererPatch.Logger.LogDebug($"Changed {child.gameObject.name} material using SkinnedMeshRenderer");
                                //ItemStateSaving.SaveItemState(component.gameObject,true);
                            }

                            
                        }
                    } catch (Exception arg)
                    {
                        SkinnedRendererPatch.Logger.LogError($"Error when adjusting material on {component.gameObject.name}; {arg}");
                    }
                }
            }
        }
        return true;
    }
}