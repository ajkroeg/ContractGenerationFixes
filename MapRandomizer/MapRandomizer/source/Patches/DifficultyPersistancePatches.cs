using System;
using BattleTech;
using BattleTech.Save;
using BattleTech.Save.Test;
using MapRandomizer.Patches;
using UnityEngine;

namespace MapRandomizer.source.Patches
{
    internal class DifficultyPersistancePatches
    {
        [HarmonyPatch(typeof(SimGameState), "Dehydrate",
            new Type[] {typeof(SimGameSave), typeof(SerializableReferenceContainer)})]
        public static class SGS_Dehydrate_Patch//MR_DIFFICULTY_
        {
            public static void Prefix(SimGameState __instance)
            {
                foreach (var diffOverride in ModState.SavedDiffOverrides)
                {
                    __instance.CompanyStats.AddStatistic(diffOverride.Key, diffOverride.Value);
                }
            }
        }

        [HarmonyPatch(typeof(SimGameState), "Rehydrate", new Type[] {typeof(GameInstanceSave)})]
        public static class SGS_Rehydrate_Patch
        {
            public static void Postfix(SimGameState __instance)
            {
                foreach (var statistic in __instance.CompanyStats)
                {
                    if (statistic.Key.StartsWith("MR_DIFFICULTY_"))
                    {
                        ModState.SavedDiffOverrides.Add(statistic.Key, statistic.Value.Value<int>());
                    }
                }

                foreach (var statOverride in ModState.SavedDiffOverrides)
                {
                    __instance.CompanyStats.RemoveStatistic(statOverride.Key);
                }
            }
        }

        [HarmonyPatch(typeof(Contract), "CompleteContract", new Type[] {typeof(MissionResult), typeof(bool)})]
        public static class Contract_CompleteContract_Patch
        {
            public static void Prefix(Contract __instance, MissionResult result, bool isGoodFaithEffort)
            {
                var sim = UnityGameInstance.BattleTechGame.Simulation;
                if (sim == null) return;
                var baseDiff = sim.CurSystem.Def.GetDifficulty(sim.SimGameMode) + Mathf.FloorToInt(sim.GlobalDifficulty);
                var quid = __instance.GenerateContractQuasiGUID(__instance.Override.employerTeam.FactionValue, __instance.Override.targetTeam.FactionValue, baseDiff, __instance.ContractBiome, sim.CurSystem);
                if (ModState.SavedDiffOverrides.ContainsKey(quid))
                {
                    ModState.SavedDiffOverrides.Remove(quid);
                }
            }
        }
    }
}
