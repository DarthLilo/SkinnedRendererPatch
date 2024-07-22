using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using LilosScrapExtension.Scripts;
using Mono.Cecil.Cil;
using UnityEngine;

namespace SkinnedRendererPatch.Patches;

[HarmonyPatch(typeof(GameNetworkManager))]
public class ItemStateSaving
{   
    [HarmonyPatch("SaveItemsInShip")]
    [HarmonyPrefix]
    public static void SaveItemState(GameNetworkManager __instance)
    {
        // List of Objects currently loaded
        GrabbableObject[] grabbableObjects = UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);

        if (grabbableObjects == null) return; // No grabbable objects, skip
        if (StartOfRound.Instance.isChallengeFile) return; // Challenge file loaded, don't save
        

        // Save Variables
        List<int> ItemIDs = [];
        List<int> meshIndexes = [];
        List<int> matIndexes = [];
        List<bool> triggeredIndexes = [];
        int curItemIndex = 0;

        // Soft Dependencies
        Assembly LSEA = SkinnedRendererPatch.AssemblyLilosScrapExtension;



        while (curItemIndex < grabbableObjects.Length && curItemIndex <= StartOfRound.Instance.maxShipItemCapacity)
        {
            // Check if the grabbable object is vaild in the round and its not deactivated
            if (StartOfRound.Instance.allItemsList.itemsList.Contains(grabbableObjects[curItemIndex].itemProperties) && !grabbableObjects[curItemIndex].deactivated)
            {
                if (grabbableObjects[curItemIndex].itemProperties.spawnPrefab == null)
                {

                    SkinnedRendererPatch.Logger.LogDebug($"{grabbableObjects[curItemIndex].itemProperties.name} has no spawn prefab set, please fix this!");
                
                } else if (!grabbableObjects[curItemIndex].itemUsedUp)
                {
                    for (int i = 0; i < StartOfRound.Instance.allItemsList.itemsList.Count; i++)
                    {
                        if (StartOfRound.Instance.allItemsList.itemsList[i] == grabbableObjects[curItemIndex].itemProperties)
                        {
                            ItemIDs.Add(i);
                            var mesh_filter = grabbableObjects[curItemIndex].gameObject.GetComponent<MeshFilter>();
                            var mesh_renderer = grabbableObjects[curItemIndex].gameObject.GetComponent<MeshRenderer>();
                            int meshIndex = -1;
                            int matIndex = -1;
                            if (grabbableObjects[curItemIndex].itemProperties.meshVariants.Length != 0)
                            {
                                if (mesh_filter != null)
                                {
                                    meshIndex = Array.IndexOf(grabbableObjects[curItemIndex].itemProperties.meshVariants, mesh_filter.mesh);
                                }
                                foreach (var child in GetSkinnedChildren(grabbableObjects[curItemIndex].gameObject.transform))
                                {
                                    meshIndex = Array.IndexOf(grabbableObjects[curItemIndex].itemProperties.meshVariants, child.GetComponent<SkinnedMeshRenderer>().sharedMesh);
                                }
                                meshIndexes.Add(meshIndex);
                                
                            } else {
                                meshIndexes.Add(meshIndex);
                            }

                            if (grabbableObjects[curItemIndex].itemProperties.materialVariants.Length != 0)
                            {
                                if (mesh_renderer != null)
                                {
                                    matIndex = Array.IndexOf(grabbableObjects[curItemIndex].itemProperties.materialVariants, mesh_renderer.sharedMaterial);
                                }
                                foreach (var child in GetSkinnedChildren(grabbableObjects[curItemIndex].gameObject.transform))
                                {
                                    matIndex = Array.IndexOf(grabbableObjects[curItemIndex].itemProperties.materialVariants, child.GetComponent<SkinnedMeshRenderer>().sharedMaterial);
                                }
                                matIndexes.Add(matIndex);
                            } else {
                                matIndexes.Add(matIndex);
                            }
                            
                            if (LSEA != null)
                            {
                                Type CollectedScrapTriggerType = LSEA.GetType("LilosScrapExtension.Scripts.CollectedScrapTrigger");
                                var collected_scrap_trigger = grabbableObjects[curItemIndex].gameObject.GetComponent(CollectedScrapTriggerType);
                                if (collected_scrap_trigger != null)
                                {
                                    FieldInfo triggeredField = CollectedScrapTriggerType.GetField("Triggered");

                                    if ((bool)triggeredField.GetValue(collected_scrap_trigger))
                                    {
                                        triggeredIndexes.Add(true);
                                    } else {
                                        triggeredIndexes.Add(false);
                                    }
                                } else {
                                    triggeredIndexes.Add(false);
                                }
                            }


                            break;
                        }
                    }
                }
            }
            curItemIndex++;
        }

        ES3.Save<int[]>("shipGrabbableMeshIndexes", meshIndexes.ToArray(), __instance.currentSaveFileName);
        ES3.Save<int[]>("shipGrabbableMaterialIndexes", matIndexes.ToArray(), __instance.currentSaveFileName);
        if (LSEA != null)
        {
            ES3.Save<bool[]>("lilosScrapExtensionTriggered",triggeredIndexes.ToArray(),__instance.currentSaveFileName);
        
        } else if (ES3.KeyExists("lilosScrapExtensionTriggered",__instance.currentSaveFileName)) { // REMOVING KEY IF LILOS SCRAP EXTENSION ISN'T FOUND
            
            ES3.DeleteKey("lilosScrapExtensionTriggered",__instance.currentSaveFileName);
        }
    }

    public static List<GameObject> GetSkinnedChildren(Transform itemTransform)
    {
        List<GameObject> objectChildren = [];
        foreach (Transform child in itemTransform)
        {   
            // Does the game object have a skinned mesh renderer, if so save it
            if (child.gameObject.GetComponent<SkinnedMeshRenderer>() && !child.gameObject.GetComponent<ScanNodeProperties>() && !objectChildren.Contains(child.gameObject))
            {   
                objectChildren.Add(child.gameObject);
            }
        }
        return objectChildren;
    }
}