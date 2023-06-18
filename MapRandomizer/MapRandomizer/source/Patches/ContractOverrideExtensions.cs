using BattleTech;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech.Data;
using BattleTech.Framework;
using IRBTModUtils;
using static MapRandomizer.source.Classes;
using UnityEngine;

namespace MapRandomizer.source.Patches
{
    public class ContractOverrideExtensions
    {
        [HarmonyPatch(typeof(ContractOverride), "FromJSON")]
        public static class ContractOverride_FromJSON
        {
            public static void Prefix(ContractOverride __instance, ref string json)
            {
                var contracOverrideJO= JObject.Parse(json);
                var state = new ContractOverrideExtension();
                OverrideExtensionManager.PendingExtension = new PendingContractOverrideExtension();
                
                if (contracOverrideJO["ForceStartOnExpiration"] != null)
                {
                    state.ForceStartOnExpiration = (bool)contracOverrideJO["ForceStartOnExpiration"];
                    contracOverrideJO.Remove("ForceStartOnExpiration");
                }

                if (contracOverrideJO["ResultsOnExpiration"] != null)
                {
                    var results = new List<SimGameEventResult>();
                    foreach (var resultJT in contracOverrideJO["ResultsOnExpiration"])
                    {
                        var resultJO = (JObject) resultJT;
                        if (resultJO == null) continue;
                        var result = resultJO.ProcessExpirationResult();
                        results.Add(result);
                        ModInit.modLog?.Trace?.Write($"[TRACE] Processed result with scope {result.Scope}");
                    }
                    state.ResultsOnExpiration = results;
                    contracOverrideJO.Remove("ResultsOnExpiration");
                }

                if (contracOverrideJO["UniversalContractEffects"] != null)
                {
                    var effectDatas = new List<EffectData>();
                    foreach (var erffectJT in contracOverrideJO["UniversalContractEffects"])
                    {
                        var effectJO = (JObject)erffectJT;
                        if (effectJO == null) continue;
                        var effectData = new EffectData();
                        effectData.FromJSON(effectJO.ToString());
                        effectDatas.Add(effectData);
                        ModInit.modLog?.Trace?.Write($"[TRACE] Processed effectData with Id {effectData.Description.Id}");
                    }
                    state.UniversalContractEffects = effectDatas;
                    contracOverrideJO.Remove("UniversalContractEffects");
                }

                OverrideExtensionManager.PendingExtension.Extension = state;
                json = contracOverrideJO.ToString(Formatting.Indented);
                ModInit.modLog?.Trace?.Write($"[TRACE] ContractOverride_FromJSON PREFIX RAN");
            }
        }

        [HarmonyPatch(typeof(DataManager.ContractOverrideLoadRequest), "OnLoadedWithJSON")]
        public static class DataManagerContractOverrideLoadRequest__OnLoadedWithJSON
        {
            public static void Postfix(DataManager.ContractOverrideLoadRequest __instance, string json)
            {
                OverrideExtensionManager.ContractOverrideExtensionDict.AddOrUpdate(__instance.resourceId, OverrideExtensionManager.PendingExtension.Extension, (k, v) => OverrideExtensionManager.PendingExtension.Extension);
                ModInit.modLog?.Trace?.Write($"[TRACE] ContractOverride_FromJSON - added {__instance.resourceId} to dict with values ForceStartOnExpiration [{OverrideExtensionManager.PendingExtension.Extension.ForceStartOnExpiration}]\rResultsOnExpiration [{OverrideExtensionManager.PendingExtension.Extension.ResultsOnExpiration.Count}]\rUniversalContractEffects [{OverrideExtensionManager.PendingExtension.Extension.UniversalContractEffects.Count}]");
                OverrideExtensionManager.ResetContractExtension();
            }
        }
    }
}
