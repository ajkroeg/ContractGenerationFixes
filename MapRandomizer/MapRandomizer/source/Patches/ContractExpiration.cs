using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;
using BattleTech.UI.Tooltips;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SVGImporter;
using HBS.Logging;

namespace MapRandomizer.source.Patches
{
    public class ContractExpiration
    {
        [HarmonyPatch(typeof(SimGameState), "OnDayPassed", new Type[] { typeof(int) })]
        public static class SimGameState_OnDayPassed
        {
            static bool Prepare() => true;

            public static void Postfix(SimGameState __instance, int timeLapse)
            {
                for (int i = __instance.CurSystem.SystemContracts.Count - 1; i >= 0; i--)
                {
                    if (__instance.CurSystem.SystemContracts[i].OnDayPassed())
                    {
                        __instance.CurSystem.SystemContracts.RemoveAt(i);
                    }
                }
                for (int i = __instance.CurSystem.SystemBreadcrumbs.Count - 1; i >= 0; i--)
                {
                    if (__instance.CurSystem.SystemBreadcrumbs[i].OnDayPassed())
                    {
                        __instance.CurSystem.SystemBreadcrumbs.RemoveAt(i);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Contract), "OnDayPassed", new Type[] { })]
        public static class Contract_OnDayPassed
        {
            static bool Prepare() => true;
            public static void Postfix(Contract __instance, ref bool __result)
            {
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                if (sim == null) return;
                var contractWidget = sim.RoomManager.CmdCenterRoom.contractsWidget;//Traverse.Create(sim.RoomManager.CmdCenterRoom).Field("contractsWidget").GetValue<SGContractsWidget>();
                if (__instance.UsingExpiration)
                {
                    ModInit.modLog.LogAtLevel(LogLevel.Log,$"[Contract_OnDayPassed] - Contract {__instance?.Name} using expiration; {__instance.ExpirationTime} remaining.");
                }

                if (__result)
                {
                    contractWidget.ListContracts(sim.GetAllCurrentlySelectableContracts(true), null);
                    
                    if (__instance.Override.GetContractOverrideExtension(out var extension))
                    {
                        if (extension.ResultsOnExpiration.Count > 0)
                        {
                            SimGameState.ApplySimGameEventResult(extension
                                .ResultsOnExpiration);
                        }

                        if (extension.ForceStartOnExpiration)
                        {
                            __instance.Override.disableCancelButton = true;
                            sim.ForceTakeContractExpiration(__instance);
                        }
                    }
                }
                var listedContracts = contractWidget.listedContracts;//Traverse.Create(contractWidget).Field("listedContracts").GetValue<List<SGContractsListItem>>();
                foreach (var contractElement in listedContracts)
                {
                    if (__instance == contractElement.Contract && contractElement.Contract.UsingExpiration)
                    {
                        var expirationElement = contractElement.expirationElement;//Traverse.Create(contractElement).Field("expirationElement").GetValue<GameObject>();
                        var title = $"Time-Limited Contract!";
                        var details = $"This contract will expire in {contractElement.Contract.ExpirationTime} days!";
                        if (contractElement.Contract.Override.GetContractOverrideExtension(out var extension))
                        {
                            if (extension.ForceStartOnExpiration)
                            {
                                details = $"Countdown: Contract will launch in {contractElement.Contract.ExpirationTime} days!";
                            }
                        }
                        if (expirationElement != null)
                        {
                            var tooltipComponent = expirationElement.gameObject.GetComponent<HBSTooltip>();
                            BaseDescriptionDef def =
                                new BaseDescriptionDef("ContractExpirationData", title, details, null);
                            tooltipComponent.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(def));
                            expirationElement.gameObject.SetActive(true);
                        }
                        
                        foreach (var entry in sim.RoomManager.timelineWidget.ActiveItems)
                        {
                            var overrideID = __instance.Override.FetchCachedOverrideID();
                            if (entry.Key is Classes.WorkOrderEntry_Notification_Timed timed &&
                                entry.Key.ID == $"{overrideID}_TimeLeft")
                            {
                                timed.PayCost(1);
                                if (timed.IsCostPaid())
                                {
                                    sim.RoomManager.RemoveWorkQueueEntry(entry.Key);
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Contract), MethodType.Constructor,
            new Type[] { typeof(string), typeof(string), typeof(string), typeof(ContractTypeValue), typeof(GameInstance), typeof(ContractOverride), typeof(GameContext), typeof(bool), typeof(int), typeof(int), typeof(int?) })]
        public static class Contract_Constructor_Long
        {
            static bool Prepare() => true;
            public static void Postfix(Contract __instance, string mapName, string mapPath, string encounterLayerGuid, ContractTypeValue contractTypeValue, GameInstance game, ContractOverride contractOverride, GameContext baseContext, bool fromSim = false, int difficulty = -1, int initialContractValue = 0, int? playerOneMoraleOverride = null)
            {
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                if (sim == null) return;
                if (contractOverride.usesExpiration && contractOverride.expirationTimeOverride > 0)
                {
                    __instance.SetExpiration(contractOverride.expirationTimeOverride); // need to patch sim.CreateTravelContract bc it "Reconstructs" ConctractOVerride liek a fucking moron

                    //var entry = new Classes.WorkOrderEntry_Notification_Timed($"{contractOverride.ID}_TimeLeft", $"Time-limited contract!", __instance.ExpirationTime);
                    //sim.RoomManager.AddWorkQueueEntry(entry); //probably need to patch TaskTimelineWidget/RefreshEntries
                }
            }
        }

        [HarmonyPatch(typeof(ContractOverride), "CopyContractTypeData", new Type[] { typeof(ContractOverride) })]
        public static class ContractOverride_CopyContractTypeData
        {
            static bool Prepare() => true;
            public static void Postfix(ContractOverride __instance, ContractOverride ovr)
            {
                __instance.usesExpiration = ovr.usesExpiration;
                __instance.expirationTimeOverride = ovr.expirationTimeOverride;
                //if (!string.IsNullOrEmpty(ovr.ID) && string.IsNullOrEmpty(__instance.ID))
                //{
                //    __instance.ID = ovr.ID;
                //}
            }
        }

        [HarmonyPatch(typeof(SGContractsListItem), "Init", new Type[] { typeof(Contract), typeof(SimGameState) })]
        public static class SGContractsListItem_Postfix
        {
            static bool Prepare() => true;
            public static void Postfix(SGContractsListItem __instance, Contract contract, SimGameState sim)
            {
                if (contract.UsingExpiration && __instance.expirationElement == null)
                {
                    var title = $"Time-Limited Contract!";
                    var details = $"This contract will expire in {contract.ExpirationTime} days!";
                    if (contract.Override.GetContractOverrideExtension(out var extension))
                    {
                        if (extension.ForceStartOnExpiration)
                        {
                            details = $"Countdown: Contract will launch in {contract.ExpirationTime} days!";
                        }
                    }
                    var timeLimitIcon =
                        UnityEngine.Object.Instantiate<GameObject>(__instance.travelIndicator,
                            __instance.travelIndicator.transform.parent);
                    var timeLimitRect = timeLimitIcon.gameObject.GetComponent<RectTransform>();
                    timeLimitRect.anchoredPosition = __instance.travelIndicator.activeSelf
                        ? new Vector2(-50f, -8.5f)
                        : new Vector2(-20f, -8.5f);

                    var iconComponent = timeLimitIcon.gameObject.GetComponent<SVGImage>();
                    var asset = UnityGameInstance.BattleTechGame.DataManager.GetObjectOfType<SVGAsset>(
                        ModInit.modSettings.ContractTimeoutIcon, BattleTechResourceType.SVGAsset);
                    iconComponent.vectorGraphics = asset;

                    var tooltipComponent = timeLimitIcon.gameObject.GetComponent<HBSTooltip>();
                    BaseDescriptionDef def = new BaseDescriptionDef("ContractExpirationData", title, details, null);
                    tooltipComponent.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject(def));
                    __instance.expirationElement = timeLimitIcon;
                    __instance.expirationElement.gameObject.SetActive(true);
                }
            }
        }

        [HarmonyPatch(typeof(TaskTimelineWidget), "AddEntry", new Type[] { typeof(WorkOrderEntry), typeof(bool) })]
        public static class TaskTimelineWidget_AddEntry
        {
            static bool Prepare() => false; //fuckit
            public static void Prefix(ref bool __runOriginal, TaskTimelineWidget __instance, WorkOrderEntry entry, bool sortEntries = true)
            {
                if (!__runOriginal) return;
                if (__instance.ActiveItems.Keys.Any(x =>
                        x is Classes.WorkOrderEntry_Notification_Timed && x.ID == entry.ID))
                {
                    __runOriginal = false;
                    return;
                }
                __runOriginal = true;
                return;
            }
        }

        [HarmonyPatch(typeof(TaskTimelineWidget), "RefreshEntries", new Type[] { })]
        public static class TaskTimelineWidget_RefreshEntries
        {
            static bool Prepare() => false; //fuckit

            public static void Prefix(ref bool __runOriginal, TaskTimelineWidget __instance)
            {
                if (!__runOriginal) return;
                foreach (WorkOrderEntry workOrderEntry in new List<WorkOrderEntry>(__instance.ActiveItems.Keys))
                {
                    if (workOrderEntry is Classes.WorkOrderEntry_Notification_Timed) continue; //keep from removing
                    if (!__instance.Sim.MechLabQueue.Contains(workOrderEntry) && __instance.Sim.TravelOrder != workOrderEntry && !__instance.Sim.MedBayQueue.SubEntries.Contains(workOrderEntry) && __instance.Sim.FinancialReportNotification != workOrderEntry && __instance.Sim.CurrentUpgradeEntry != workOrderEntry)
                    {
                        __instance.RemoveEntry(workOrderEntry, false);
                    }
                }
                int num = 0;
                for (int i = 0; i < __instance.Sim.MechLabQueue.Count; i++)
                {
                    WorkOrderEntry workOrderEntry2 = __instance.Sim.MechLabQueue[i];
                    if (__instance.ActiveItems.TryGetValue(workOrderEntry2, out var taskManagementElement))
                    {
                        if (__instance.Sim.WorkOrderIsMechTech(workOrderEntry2.Type))
                        {
                            num = taskManagementElement.UpdateItem(num);
                        }
                        else
                        {
                            taskManagementElement.UpdateItem(0);
                        }
                    }
                }
                for (int j = 0; j < __instance.Sim.MedBayQueue.SubEntryCount; j++)
                {
                    WorkOrderEntry workOrderEntry3 = __instance.Sim.MedBayQueue.SubEntries[j];
                    TaskManagementElement taskManagementElement2 = null;
                    if (__instance.ActiveItems.TryGetValue(workOrderEntry3, out taskManagementElement2))
                    {
                        taskManagementElement2.UpdateItem(0);
                    }
                }
                if (__instance.Sim.FinancialReportNotification != null)
                {
                    TaskManagementElement taskManagementElement3 = null;
                    if (__instance.ActiveItems.TryGetValue(__instance.Sim.FinancialReportNotification, out taskManagementElement3))
                    {
                        taskManagementElement3.UpdateItem(0);
                    }
                    else
                    {
                        __instance.Sim.FinancialReportItem = __instance.AddEntry(__instance.Sim.FinancialReportNotification, false);
                    }
                }
                if (__instance.Sim.CurrentUpgradeEntry != null)
                {
                    TaskManagementElement taskManagementElement4 = null;
                    if (__instance.ActiveItems.TryGetValue(__instance.Sim.CurrentUpgradeEntry, out taskManagementElement4))
                    {
                        taskManagementElement4.UpdateItem(0);
                    }
                }
                __instance.SortEntries();
                __runOriginal = false;
                return;
            }
        }

        [HarmonyPatch(typeof(MainMenu), "Init")]
        public class MainMenuInitPatch
        {
            public static void Postfix(MainMenu __instance)
            {
                Utils.InitializeIcon();
            }
        }
    }
}
