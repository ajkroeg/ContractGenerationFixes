using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;
using BattleTech.Save.SaveGameStructure;
using HBS.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SVGImporter;

namespace MapRandomizer.source
{
    public class Classes
    {
        public class WorkOrderEntry_Notification_Timed : WorkOrderEntry
        {
            public WorkOrderEntry_Notification_Timed(string ID, string Description, int baseCost, string ToastDescription = "")
                : base(WorkOrderType.NotificationGeneric, ID, Description, baseCost, ToastDescription, 0)
            {
            }
        }

        public class PendingContractOverrideExtension
        {
            public string ID;
            public ContractOverrideExtension Extension;
        }

        public class ContractOverrideExtension
        {
            public bool ForceStartOnExpiration;
            public List<SimGameEventResult> ResultsOnExpiration = new List<SimGameEventResult>();
            public List<EffectData> UniversalContractEffects = new List<EffectData>(); // applied to all units in contract, no matter what.
        }
    }

    public static class Utils
    {
        public static bool IDRehydratePredicate(string memberName)
        {
            return memberName == "ID";
        }
        public static string FetchCachedOverrideID(this ContractOverride contractOverride)
        {
            if (!string.IsNullOrEmpty(contractOverride.ID)) return contractOverride.ID;
            if (ModState.OverrideIDCache.TryGetValue(contractOverride, out var cachedID)) return cachedID;
            if (string.IsNullOrEmpty(contractOverride.cachedJson))
            {
                foreach (var result in contractOverride.OnContractSuccessResults)
                {
                    if (result.Actions != null)
                    {
                        SimGameResultAction[] actions = result.Actions;
                        for (int i = 0; i < actions.Length; i++)
                        {
                            if (actions[i].Type == SimGameResultAction.ActionType.System_StartNonProceduralContract)
                            {
                                if (actions[i].additionalValues.Length > 3)
                                {
                                    if (!string.IsNullOrEmpty(actions[i].additionalValues[3]))
                                        return actions[i].additionalValues[3];
                                }
                            }
                        }
                    }
                }
                return null;
            }

            var deserializedOverride = new ContractOverride();

            JSONSerializationUtility.FromJSON<ContractOverride>(contractOverride, contractOverride.cachedJson, new Func<string, bool>[]
            {
                new Func<string, bool>(IDRehydratePredicate)
            });
            ModState.OverrideIDCache.Add(contractOverride, deserializedOverride.ID);
            return deserializedOverride.ID;
            //JSONSerializationUtility.FromJSON<ContractOverride>(deserializedOverride, contractOverride.cachedJson);
            //deserializedOverride.UpgradeToDataDrivenEnums();
            //contractOverride.ID = deserializedOverride.ID;
        }

        public static void ForceTakeContractExpiration(this SimGameState sim, Contract contract)
        {
            if (sim.CompletedContract != null)
            {
                sim.PendingMilestoneContract = contract;
                sim.IsPendingMilestoneContractBreadcrumb = false;
                return;
            }
            sim.SetSelectedContract(contract, false, false);
            sim._selectedContractForced = true;
            if (sim.TimeMoving)
            {
                sim.StopPlayMode();
            }
            if (contract.CarryOverNegotationValues)
            {
                if (sim.activeBreadcrumb != null)
                {
                    contract.SetNegotiatedValues(sim.activeBreadcrumb.PercentageContractValue, sim.activeBreadcrumb.PercentageContractSalvage);
                    sim.ClearBreadcrumb();
                    sim.OnForcedContractNegotiationComplete();
                    return;
                }
                SimGameState.logger.LogError("Attempting to carry over negotiated values without a breadcrumb");
                contract.SetNegotiatedValues(1f, 0f);
                return;
            }
            else
            {
                contract.SetNegotiatedValues(contract.Override.negotiatedSalary, contract.Override.negotiatedSalvage);
                sim.OnForcedContractNegotiationComplete();
                return;
            }
        }

        public static void InitializeIcon()
        {
            DataManager dm = UnityGameInstance.BattleTechGame.DataManager;
            LoadRequest loadRequest = dm.CreateLoadRequest();
            loadRequest.AddLoadRequest<SVGAsset>(BattleTechResourceType.SVGAsset, ModInit.modSettings.ContractTimeoutIcon, null);
            loadRequest.ProcessRequests();
        }
    }
}
