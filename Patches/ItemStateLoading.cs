using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SkinnedRendererPatch.Helpers;
using SkinnedRendererPatch.ModCompatability;
using Unity.Netcode;
using UnityEngine;

namespace SkinnedRendererPatch.Patches;

[HarmonyPatch(typeof(StartOfRound))]
public class ItemStateLoading
{

    // ClientRPC Data
    public static ClientRpcParams clientParamData;
    // NETWORKING DATA
    public static Dictionary<ulong, int> MeshIndexesDICT = [];
    public static Dictionary<ulong, int> MatIndexesDICT = [];
    public static Dictionary<ulong, bool> LSEAIndexesDICT = [];

    [HarmonyPatch("LoadShipGrabbableItems")]
    [HarmonyPostfix]
    public static void LoadItemState(StartOfRound __instance) // Load item data after finishing LoadShipGrabbableItems
    {
        DetermineItemStatesOnSaveData();
    }

    [HarmonyPatch("OnClientConnect")]
    [HarmonyPrefix]
    public static void OnClientConnectPatch(StartOfRound __instance, ulong clientId)
    {
        if (__instance.IsServer)
        {
            if (SRPNetworkHelper.Instance != null)
            {
                clientParamData = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } };

                // Sync Item State Data
                SkinnedRendererPatch.Logger.LogInfo("Syncing Item State Data");
                
                SRPNetworkHelper.Instance.SyncItemDataClientRpc(
                    JsonConvert.SerializeObject(MeshIndexesDICT) ?? "",
                    JsonConvert.SerializeObject(MatIndexesDICT) ?? "",
                    JsonConvert.SerializeObject(LSEAIndexesDICT) ?? "",
                    clientParamData);
            }
        }
    }



    public static void DetermineItemStatesOnSaveData()
    {
        SkinnedRendererPatch.Logger.LogInfo("Started item syncing");
        if (!ES3.KeyExists("shipGrabbableItemIDs", GameNetworkManager.Instance.currentSaveFileName)) return;

        bool flag1 = ES3.KeyExists("shipGrabbableMeshIndexes", GameNetworkManager.Instance.currentSaveFileName);
        bool flag2 = ES3.KeyExists("shipGrabbableMaterialIndexes", GameNetworkManager.Instance.currentSaveFileName);
        bool flag3 = ES3.KeyExists("lilosScrapExtensionTriggered", GameNetworkManager.Instance.currentSaveFileName);

        //int[] grabbableObjects = ES3.Load<int[]>("shipGrabbableItemIDs", GameNetworkManager.Instance.currentSaveFileName);
        GrabbableObject[] grabbableObjectsActual = UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
        int[]? meshIndexes = null;
        int[]? matIndexes = null;
        bool[]? lilosTriggerIndexes = null;

        if (flag1) {meshIndexes = ES3.Load<int[]>("shipGrabbableMeshIndexes",GameNetworkManager.Instance.currentSaveFileName);}
        if (flag2) {matIndexes = ES3.Load<int[]>("shipGrabbableMaterialIndexes",GameNetworkManager.Instance.currentSaveFileName);}
        if (SkinnedRendererPatch.LilosScrapExtensionPresent && flag3) {lilosTriggerIndexes = ES3.Load<bool[]>("lilosScrapExtensionTriggered",GameNetworkManager.Instance.currentSaveFileName);}
        
        int curMeshIndex = 0;
        int curMatIndex = 0;
        int curTriggerIndex = 0;
        

        GrabbableObject[] grabbableObjectsSorted = HierarchicalSorting.Sort(grabbableObjectsActual);

        // Clear old networking data
        MeshIndexesDICT.Clear();
        MatIndexesDICT.Clear();
        LSEAIndexesDICT.Clear();
        
        SkinnedRendererPatch.Logger.LogInfo("Beginning heiracrhy search");
        for (int i = 0; i < grabbableObjectsSorted.Length; i++)
        {  
            GrabbableObject component = grabbableObjectsSorted[i];
            if (grabbableObjectsSorted[i].itemProperties.isScrap)
            {
                try
                {
                    if (meshIndexes != null)
                    {
                        if (meshIndexes[curMeshIndex] != -1)
                        {
                            // Store Networking Data
                            MeshIndexesDICT.Add(component.gameObject.GetComponent<NetworkObject>().NetworkObjectId,meshIndexes[curMeshIndex]);
                            
                            // Apply custom mesh data
                            var mesh_filter = component.gameObject.GetComponent<MeshFilter>();
                            if (mesh_filter != null)
                            {
                                mesh_filter.mesh = component.itemProperties.meshVariants[meshIndexes[curMeshIndex]];
                            }

                            foreach (var child in ItemStateSaving.GetSkinnedChildren(component.gameObject.transform))
                            {
                                child.GetComponent<SkinnedMeshRenderer>().sharedMesh = component.itemProperties.meshVariants[meshIndexes[curMeshIndex]]; 
                            }
                            
                        }
                        curMeshIndex++;
                    }
                } catch (Exception ex) {
                    SkinnedRendererPatch.Logger.LogError($"Error when updating item states, most likely due to a removed mod! " + ex);
                }

                try
                {
                    if (matIndexes != null)
                    {
                        if (matIndexes[curMatIndex] != -1)
                        {
                            // Store Networking Data
                            MatIndexesDICT.Add(component.gameObject.GetComponent<NetworkObject>().NetworkObjectId,matIndexes[curMatIndex]);
                            // Apply custom material data
                            var mesh_renderer = component.gameObject.GetComponent<MeshRenderer>();
                            if (mesh_renderer != null)
                            {
                                mesh_renderer.sharedMaterial = component.itemProperties.materialVariants[matIndexes[curMatIndex]];
                            }
                            foreach (var child in ItemStateSaving.GetSkinnedChildren(component.gameObject.transform))
                            {
                                child.GetComponent<SkinnedMeshRenderer>().sharedMaterial = component.itemProperties.materialVariants[matIndexes[curMatIndex]];
                            }
                        }
                        curMatIndex++;
                    }
                } catch (Exception ex) {
                    SkinnedRendererPatch.Logger.LogError($"Error when updating item states, most likely due to a removed mod! " + ex);
                }

                if (SkinnedRendererPatch.LilosScrapExtensionPresent && lilosTriggerIndexes != null) {
                    //Type CollectedScrapTriggerType = LSEA.GetType("LilosScrapExtension.Scripts.CollectedScrapTrigger");
                    if (lilosTriggerIndexes[curTriggerIndex] != false)
                    {
                        LSEAIndexesDICT.Add(component.gameObject.GetComponent<NetworkObject>().NetworkObjectId,lilosTriggerIndexes[curTriggerIndex]);
                        LilosScrapExtensionCompat.ApplyCustomLSEAData(component,lilosTriggerIndexes[curTriggerIndex]);
                    }
                    curTriggerIndex++;

                }

            }
        }
    }

    public static class HierarchicalSorting
    {
        private static int Compare([CanBeNull] Component x, [CanBeNull] Component y)
        {
            return Compare(x != null ? x.transform : null, y != null ? y.transform : null);
        }

        private static int Compare([CanBeNull] GameObject x, [CanBeNull] GameObject y)
        {
            return Compare(x != null ? x.transform : null, y != null ? y.transform : null);
        }

        private static int Compare([CanBeNull] Transform x, [CanBeNull] Transform y)
        {
            if (x == null && y == null)
                return 0;

            if (x == null)
                return -1;

            if (y == null)
                return +1;

            var hierarchy1 = GetHierarchy(x);
            var hierarchy2 = GetHierarchy(y);

            while (true)

            {
                if (!hierarchy1.Any())
                    return -1;

                var pop1 = hierarchy1.Pop();

                if (!hierarchy2.Any())
                    return +1;

                var pop2 = hierarchy2.Pop();

                var compare = pop1.CompareTo(pop2);

                if (compare == 0)
                    continue;

                return compare;
            }
        }

        [NotNull]
        private static Stack<int> GetHierarchy([NotNull] Transform transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            var stack = new Stack<int>();

            var current = transform;

            while (current != null)
            {
                var index = current.GetSiblingIndex();

                stack.Push(index);

                current = current.parent;
            }

            return stack;
        }

        [PublicAPI]
        [NotNull]
        [ItemNotNull]
        public static T[] Sort<T>([NotNull] [ItemNotNull] T[] components) where T : Component
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            Array.Sort(components, new RelayComparer<T>(Compare));

            return components;
        }

        [PublicAPI]
        [NotNull]
        [ItemNotNull]
        public static GameObject[] Sort([NotNull] [ItemNotNull] GameObject[] gameObjects)
        {
            if (gameObjects == null)
                throw new ArgumentNullException(nameof(gameObjects));

            Array.Sort(gameObjects, new RelayComparer<GameObject>(Compare));

            return gameObjects;
        }

        [PublicAPI]
        [NotNull]
        [ItemNotNull]
        public static Transform[] Sort([NotNull] [ItemNotNull] Transform[] transforms)
        {
            if (transforms == null)
                throw new ArgumentNullException(nameof(transforms));

            Array.Sort(transforms, new RelayComparer<Transform>(Compare));

            return transforms;
        }

        private sealed class RelayComparer<T> : Comparer<T>
        {
            public RelayComparer([NotNull] Func<T, T, int> func)
            {
                Func = func ?? throw new ArgumentNullException(nameof(func));
            }

            [NotNull]
            private Func<T, T, int> Func { get; }

            public override int Compare(T x, T y)
            {
                return Func(x, y);
            }
        }
    }
}