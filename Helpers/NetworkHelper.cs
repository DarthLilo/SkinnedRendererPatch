using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Newtonsoft.Json;
using SkinnedRendererPatch.ModCompatability;
using SkinnedRendererPatch.Patches;
using Unity.Netcode;
using UnityEngine;

namespace SkinnedRendererPatch.Helpers
{
    internal class SRPNetworkHelper : NetworkBehaviour
    {
        public static SRPNetworkHelper Instance { get; private set; }

        private void Start()
        {
            Instance = this;

            SkinnedRendererPatch.Logger.LogInfo("SRPNetworkHelper.Start() initialized!");
        }

        [ClientRpc]
        public void SyncItemDataClientRpc(string meshIndexes, string matIndexes, string LSEAIndexes, ClientRpcParams clientParams)
        {
            // Creating reference of all active gameobjects
            GrabbableObject[] grabbableObjects = UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
            Dictionary<ulong, GrabbableObject> grabObjIds = [];

            if (grabbableObjects.Length == 0) return;
            
            // CREATING REFERENCES
            foreach (var obj in grabbableObjects)
            {
                ulong objectId = obj.gameObject.GetComponent<NetworkObject>().NetworkObjectId;
                grabObjIds.Add(objectId,obj);
            }

            // DEBUG
            SkinnedRendererPatch.Logger.LogDebug("DEBUGGING:");
            SkinnedRendererPatch.Logger.LogDebug($"MESH INDEXES: {meshIndexes}");
            SkinnedRendererPatch.Logger.LogDebug($"MATERIAL INDEXES: {matIndexes}");
            SkinnedRendererPatch.Logger.LogDebug($"LSEA INDEXES: {LSEAIndexes}");
            

            // IF MESH CHANGES ARE VALID, CONTINUE
            if (meshIndexes != null && meshIndexes != "")
            {
                SkinnedRendererPatch.Logger.LogInfo($"Recieved valid mesh indexes, applying changes");
                ApplyMeshChanges(grabObjIds,meshIndexes);
            } else {
                SkinnedRendererPatch.Logger.LogInfo("No valid mesh changes recieved, skipping");
            }

            // IF MAT CHANGES ARE VALID, CONTINUE
            if (matIndexes != null && matIndexes != "")
            {
                SkinnedRendererPatch.Logger.LogInfo($"Recieved valid material indexes, applying changes");
                ApplyMatChanges(grabObjIds,matIndexes);
            } else {
                SkinnedRendererPatch.Logger.LogInfo("No valid material changes recieved, skipping");
            }


            // LSEA CHANGES
            if (SkinnedRendererPatch.LilosScrapExtensionPresent)
            {
                SkinnedRendererPatch.Logger.LogInfo("DarthLilo.LilosScrapExtention is present, checking for extra changes");
                if (LSEAIndexes != null && LSEAIndexes != "")
                {
                    SkinnedRendererPatch.Logger.LogInfo("Recieved valid DarthLilo.LilosScrapExtention indexes, applying changes");
                    ApplyLSEAChanges(grabObjIds,LSEAIndexes);
                } else {
                    SkinnedRendererPatch.Logger.LogInfo("No valid DarthLilo.LilosScrapExtention changes recieved, skipping");
                }
            }  
        }

        public void ApplyMeshChanges(Dictionary<ulong, GrabbableObject> grabbableObjs, string meshChangesString)
        {
            Dictionary<ulong, int>? MeshIndexesDICT = JsonConvert.DeserializeObject<Dictionary<ulong, int>>(meshChangesString);
            if (MeshIndexesDICT != null && MeshIndexesDICT.Count != 0)
            {
                foreach (var kvp in MeshIndexesDICT)
                {
                    try
                    {
                        SkinnedRendererPatch.Logger.LogDebug($"[MESH APPLY] - GameObject: [{grabbableObjs[kvp.Key].gameObject.name}] - NetworkObjectID: [{kvp.Key}] - Mesh Variants: [{grabbableObjs[kvp.Key].itemProperties.meshVariants}] - Selected Mesh Index: [{kvp.Value}]");
                        GrabbableObject targetOBJ = grabbableObjs[kvp.Key];
                        var mesh_filter = targetOBJ.gameObject.GetComponent<MeshFilter>();
                        Mesh newMesh = targetOBJ.itemProperties.meshVariants[kvp.Value];
                        if (mesh_filter != null)
                        {
                            mesh_filter.mesh = newMesh;
                        }
                        foreach (var child in ItemStateSaving.GetSkinnedChildren(targetOBJ.gameObject.transform))
                        {
                            child.GetComponent<SkinnedMeshRenderer>().sharedMesh = newMesh;
                        }
                    } catch (Exception ex) {
                        SkinnedRendererPatch.Logger.LogError($"ERROR WHEN APPLYING MESH FOR [{grabbableObjs[kvp.Key].gameObject.name}]: {ex}");
                    }
                }
            }
        }

        public void ApplyMatChanges(Dictionary<ulong, GrabbableObject> grabbableObjs, string matChangesString)
        {
            Dictionary<ulong, int>? MatIndexesDICT = JsonConvert.DeserializeObject<Dictionary<ulong, int>>(matChangesString);
            if (MatIndexesDICT != null && MatIndexesDICT.Count != 0)
            {
                foreach (var kvp in MatIndexesDICT)
                {
                    try
                    {
                        SkinnedRendererPatch.Logger.LogDebug($"[MATERIAL APPLY] - GameObject: [{grabbableObjs[kvp.Key].gameObject.name}] - NetworkObjectID: [{kvp.Key}] - Material Variants: [{grabbableObjs[kvp.Key].itemProperties.materialVariants}] - Selected Material Index: [{kvp.Value}]");
                        GrabbableObject targetOBJ = grabbableObjs[kvp.Key];
                        var mesh_renderer = targetOBJ.gameObject.GetComponent<MeshRenderer>();
                        Material newMaterial = targetOBJ.itemProperties.materialVariants[kvp.Value];
                        if (mesh_renderer != null)
                        {
                            mesh_renderer.sharedMaterial = newMaterial;
                        }
                        foreach (var child in ItemStateSaving.GetSkinnedChildren(targetOBJ.gameObject.transform))
                        {
                            child.GetComponent<SkinnedMeshRenderer>().sharedMaterial = newMaterial;
                        }
                    } catch (Exception ex) {
                        SkinnedRendererPatch.Logger.LogError($"ERROR WHEN APPLYING MATERIAL FOR [{grabbableObjs[kvp.Key].gameObject.name}]: {ex}");
                    }
                }
            }
        }

        public void ApplyLSEAChanges(Dictionary<ulong, GrabbableObject> grabbableObjs, string LSEAChangesString)
        {
            Dictionary<ulong, bool>? LSEAIndexesDICT = JsonConvert.DeserializeObject<Dictionary<ulong, bool>>(LSEAChangesString);
            
            if (LSEAIndexesDICT != null && LSEAIndexesDICT.Count != 0)
            {
                foreach (var kvp in LSEAIndexesDICT)
                {
                    try
                    {
                        SkinnedRendererPatch.Logger.LogDebug($"[LSEA APPLY] - GameObject: [{grabbableObjs[kvp.Key].gameObject.name}] - NetworkObjectID: [{kvp.Key}]");
                        GrabbableObject targetOBJ = grabbableObjs[kvp.Key];
                        LilosScrapExtensionCompat.ApplyCustomLSEAData(targetOBJ,true);
                    } catch (Exception ex) {
                        SkinnedRendererPatch.Logger.LogError($"ERROR WHEN APPLYING LSEA FOR [{grabbableObjs[kvp.Key].gameObject.name}]: {ex}");
                    }

                    
                }
            }
        }
    }
}