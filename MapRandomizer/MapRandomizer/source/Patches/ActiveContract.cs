using BattleTech;
using System;
using HBS.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;

namespace MapRandomizer.source.Patches
{
    public class ActiveContract
    {
        [HarmonyPatch(typeof(Team), "AddUnit", new Type[] {typeof(AbstractActor)})]
        public static class Team_AddUnit
        {
            public static void Postfix(Team __instance, AbstractActor unit)
            {
                if (__instance.Combat.ActiveContract.Override.GetContractOverrideExtension(out var extension))
                {
                    if (extension?.UniversalContractEffects.Count > 0)
                    {
                        foreach (EffectData effectData in extension.UniversalContractEffects)
                        {
                            ModInit.modLog.LogAtLevel(LogLevel.Log,$"[Team_AddUnit - UniversalContractEffects] processing {effectData.Description.Name} for {unit.DisplayName} - {unit.GUID}");

                            if (effectData.targetingData.effectTriggerType == EffectTriggerType.Passive &&
                                effectData.targetingData.effectTargetType == EffectTargetType.Creator)
                            {
                                string id = ($"UniversalContractEffects_{unit.DisplayName}_{effectData.Description.Id}");
                                ModInit.modLog.LogAtLevel(LogLevel.Log,$"Applying {id}");
                                unit.Combat.EffectManager.CreateEffect(effectData, id, -1, unit, unit, default(WeaponHitInfo), 1);
                            }
                        }
                    }
                }
            }
        }
    }
}
