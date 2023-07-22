using System.Linq;
using System.Collections.Generic;
using BattleTech.Data;
using BattleTech;
using HBS.Logging;
using UnityEngine;
using MapRandomizer.source;

namespace MapRandomizer.Patches
{
    public static class Util
    {
		public static string GenerateContractQuasiGUID(this Contract contract, FactionValue employer, FactionValue target, int baseDiff, Biome.BIOMESKIN biome, StarSystem system)
        {
            var sim = UnityGameInstance.BattleTechGame.Simulation;
            return $"MR_DIFFICULTY_{contract.GetContractTypeString(sim)}_{contract.Name}_{employer.Name}_{target.Name}_{baseDiff}_{biome}_{system.ID}";
        }
    }

    public static class IntegerExtensions
	{
		public static int ParseInt(this string value, int defaultIntValue = 0)
		{
            if (int.TryParse(value, out var parsedInt))
			{
				return parsedInt;
			}

			return defaultIntValue;
		}

		public static int? ParseNullableInt(this string value)
		{
			if (string.IsNullOrEmpty(value))
				return null;

			return value.ParseInt();
		}
	}
	static class MapRandomizerPatches
	{
		//		private static System.Random rng = new System.Random();
		//		public static void Shuffle<T>(this IList<T> list)
		//
		//		{
		//			int n = list.Count;
		//			while (n > 1)
		//			{
		//				n--;
		//				int k = rng.Next(n + 1);
		//				T value = list[k];
		//				list[k] = list[n];
		//				list[n] = value;
        //			}
        //		}
        [HarmonyPatch(typeof(BattleTech.SimGameState), "OnDayPassed")]
        public static class OnDayPassed_Patch
        {
            public static void Postfix(SimGameState __instance, int timeLapse)
            {
                __instance.CompanyStats.AddStatistic<int>("HasTravelContract", 0);
                if (__instance.HasTravelContract == true)
                {
                    __instance.CompanyStats.Set<int>("HasTravelContract", 1);
                }
                else
                {
                    __instance.CompanyStats.Set<int>("HasTravelContract", 0);
                }
            }
        }

		[HarmonyPatch(typeof(BattleTech.SimGameState), "ParseContractActionData")]
		public static class ParseContractActionData_Patch
		{
			public static void Prefix(SimGameState __instance, string actionValue, string[] additionalValues)
			{
				ModState.IsSystemActionPatch = actionValue;
				ModState.SpecMapID = additionalValues.ElementAtOrDefault(4);
				ModState.IgnoreBiomes = additionalValues.ElementAtOrDefault(5);
				ModState.CustomDifficulty = additionalValues.ElementAtOrDefault(6).ParseInt();
				ModState.SysAdjustDifficulty = additionalValues.ElementAtOrDefault(7).ParseInt();

                ModInit.modLog.LogAtLevel(LogLevel.Log,$"[ParseContractActionData] Parsed override values: IsSystemActionPatch: {ModState.IsSystemActionPatch}\nSpecMapID:{ModState.SpecMapID}\nIgnoreBiomes: {ModState.IgnoreBiomes}\nCustomDifficulty: {ModState.CustomDifficulty}\nSysAdjustDifficulty: {ModState.SysAdjustDifficulty}");
			}
		}

        [HarmonyPatch(typeof(BattleTech.SimGameState), "AddContract")]
		public static class AddContract_Patch
		{
			public static void Prefix(SimGameState __instance, Dictionary<string, StarSystem> ___starDict, SimGameState.AddContractData contractData)
			{
				StarSystem AddContractSystem;
				
				if (!string.IsNullOrEmpty(contractData.TargetSystem))
				{
					string validatedSystemString = __instance.GetValidatedSystemString(contractData.TargetSystem);
					if (!___starDict.ContainsKey(validatedSystemString))
					{
						return;
					}
					AddContractSystem = ___starDict[validatedSystemString];
				}
				else
				{
					AddContractSystem = __instance.CurSystem;
				}
				ModState.AddContractBiomes = AddContractSystem.Def.SupportedBiomes;
				
			}

			public static void Postfix(SimGameState __instance, SimGameState.AddContractData contractData)
			{
                ModState.IsSystemActionPatch = null;

				ModState.AddContractBiomes = null;

				ModState.IgnoreBiomes = null;

				ModState.SpecMapID = null;

				ModState.CustomDifficulty = 0;

				ModState.SysAdjustDifficulty = 0;
            }

		}
		[HarmonyPatch(typeof(BattleTech.SimGameState), "PrepContract")]
        [HarmonyAfter(new string[] { "blue.winds.WarTechIIC" })]
        [HarmonyPriority(Priority.Last)]
		public static class PrepContractPatch
        {
            public static void Prefix(ref bool __runOriginal, SimGameState __instance, Contract contract, FactionValue employer, FactionValue employersAlly, FactionValue target, FactionValue targetsAlly, FactionValue NeutralToAll, FactionValue HostileToAll, Biome.BIOMESKIN skin, int presetSeed, StarSystem system)
            {
                if (!__runOriginal) return;
                ModInit.modLog.LogAtLevel(LogLevel.Log,$"{contract.Name} presetSeed: {presetSeed}");
                ModInit.modLog.LogAtLevel(LogLevel.Log,$"{contract.Name} contract.IsPriorityContract: {contract.IsPriorityContract}");
                if (presetSeed != 0 && !contract.IsPriorityContract)
				{
                    int baseDiff = system.Def.GetDifficulty(__instance.SimGameMode) + Mathf.FloorToInt(__instance.GlobalDifficulty);

                    var quid = contract.GenerateContractQuasiGUID(employer, target, baseDiff, skin, system);
                    ModInit.modLog.LogAtLevel(LogLevel.Log,$"{contract.Name} generated quasi UID: {quid}");

					ModInit.modLog.LogAtLevel(LogLevel.Log,$"{contract.Name} baseDiff: {baseDiff}");
					int min;
					int num;
					if (ModState.SysAdjustDifficulty != 0 && ModState.IsSystemActionPatch != null && !ModState.SavedDiffOverrides.ContainsKey(quid))
					{
						baseDiff += ModState.SysAdjustDifficulty;
                        ModState.SavedDiffOverrides.Add(quid, baseDiff);
                        ModInit.modLog.LogAtLevel(LogLevel.Log,$"{contract.Name} baseDiff: {baseDiff} after + ModState.SysAdjustDifficulty {ModState.SysAdjustDifficulty}. Added to ModState.SavedDiffOverrides {quid}");
					}
					else if (ModState.CustomDifficulty > 0 && ModState.IsSystemActionPatch != null && !ModState.SavedDiffOverrides.ContainsKey(quid))
					{
						baseDiff = ModState.CustomDifficulty;
                        ModState.SavedDiffOverrides.Add(quid, baseDiff);
						ModInit.modLog.LogAtLevel(LogLevel.Log,$"{contract.Name} baseDiff: {baseDiff} after override from ModState.CustomDifficulty {ModState.CustomDifficulty}. Added to ModState.SavedDiffOverrides {quid}");
					}

                    if (ModState.SavedDiffOverrides.ContainsKey(quid))
                    {
                        baseDiff = ModState.SavedDiffOverrides[quid];
                        ModInit.modLog.LogAtLevel(LogLevel.Log,$"{contract.Name} using baseDiff: {baseDiff} after override from ModState.SavedDiffOverrides {quid}");
					}

					int contractDifficultyVariance = __instance.Constants.Story.ContractDifficultyVariance;
                    ModInit.modLog.LogAtLevel(LogLevel.Log,$"{contract.Name} contractDifficultyVariance: {contractDifficultyVariance}");

					min = Mathf.Max(1, baseDiff - contractDifficultyVariance);
                    ModInit.modLog.LogAtLevel(LogLevel.Log,$"{contract.Name}: [min = Mathf.Max(1, baseDiff - contractDifficultyVariance)] min: {min}");
					num = Mathf.Max(1, baseDiff + contractDifficultyVariance);
                    ModInit.modLog.LogAtLevel(LogLevel.Log,$"{contract.Name}: [max = Mathf.Max(1, baseDiff + contractDifficultyVariance)] max: {num}");

					int finalDifficulty = new NetworkRandom
					{
						seed = presetSeed
					}.Int(min, num + 1);
                    ModInit.modLog.LogAtLevel(LogLevel.Log,$"{contract.Name} finalDifficulty = random between min and max+1: {min} and {num+1}: {finalDifficulty}");

                    if (ModInit.modSettings.enableTravelFix)
                    {
                        if (!string.IsNullOrEmpty(contract.GUID))
                        {
                            if (__instance.HasTravelContract == true &&
                                ModState.SavedDiffs.ContainsKey(contract.GUID))
                            {
                                finalDifficulty = ModState.SavedDiffs[contract.GUID];
                                ModInit.modLog.LogAtLevel(LogLevel.Log,
                                    $"Found Travel Contract: {contract.Name}, using override finalDifficulty from ModState.LastDiff for {contract.GUID}: {finalDifficulty}");
                                ModState.SavedDiffs.Remove(contract.GUID);
                            }
                            else
                            {
                                if (contract.Override.travelOnly)
                                {
                                    ModState.SavedDiffs.Add(contract.GUID, finalDifficulty);
                                    ModInit.modLog.LogAtLevel(LogLevel.Log,
                                        $"Setting future travel contract override finalDifficulty at ModState.LastDiff: {contract.GUID} - {finalDifficulty}");
                                }
                            }
                        }
                    }
                    contract.SetFinalDifficulty(finalDifficulty);
                    ModInit.modLog.LogAtLevel(LogLevel.Log,$"Setting {contract.Name} finalDifficulty to: {finalDifficulty}");
				}
				
				FactionValue player1sMercUnitFactionValue = FactionEnumeration.GetPlayer1sMercUnitFactionValue();
				FactionValue player2sMercUnitFactionValue = FactionEnumeration.GetPlayer2sMercUnitFactionValue();
				contract.AddTeamFaction("bf40fd39-ccf9-47c4-94a6-061809681140", player1sMercUnitFactionValue.ID);
				contract.AddTeamFaction("757173dd-b4e1-4bb5-9bee-d78e623cc867", player2sMercUnitFactionValue.ID);
				contract.AddTeamFaction("ecc8d4f2-74b4-465d-adf6-84445e5dfc230", employer.ID);
				contract.AddTeamFaction("70af7e7f-39a8-4e81-87c2-bd01dcb01b5e", employersAlly.ID);
				contract.AddTeamFaction("be77cadd-e245-4240-a93e-b99cc98902a5", target.ID);
				contract.AddTeamFaction("31151ed6-cfc2-467e-98c4-9ae5bea784cf", targetsAlly.ID);
				contract.AddTeamFaction("61612bb3-abf9-4586-952a-0559fa9dcd75", NeutralToAll.ID);
				contract.AddTeamFaction("3c9f3a20-ab03-4bcb-8ab6-b1ef0442bbf0", HostileToAll.ID);
				contract.SetupContext();
				int finalDifficulty2 = contract.Override.finalDifficulty;

                int num2;
				if (contract.Override.contractRewardOverride >= 0)
				{
					num2 = contract.Override.contractRewardOverride;
                    ModInit.modLog.LogAtLevel(LogLevel.Log,$"Using {num2} for contract reward override");
                }

				else
				{
					num2 = __instance.CalculateContractValueByContractType(contract.ContractTypeValue, finalDifficulty2, (float)__instance.Constants.Finances.ContractPricePerDifficulty, __instance.Constants.Finances.ContractPriceVariance, presetSeed);
                    ModInit.modLog.LogAtLevel(LogLevel.Log,$"Calculated contract Value using Contract.CalculateContractValueByContractType");
				}
				num2 = SimGameState.RoundTo((float)num2, 1000);
                ModInit.modLog.LogAtLevel(LogLevel.Log,$"Final contract value: {num2}");
				contract.SetInitialReward(num2);
				contract.SetBiomeSkin(skin);
			
			__runOriginal = false;
            return;
            }
		}

        [HarmonyPatch(typeof(BattleTech.Data.MapsAndEncounters_MDDExtensions), "GetReleasedMapsAndEncountersByContractTypeAndOwnership")]
		public static class GetReleasedMapsAndEncountersByContractTypeAndOwnership_Patch
		{
			public static void Prefix(ref bool __runOriginal, ref List<MapAndEncounters> __result, MetadataDatabase mdd, int contractTypeID, bool includeUnpublishedContractTypes)
            {

				if (string.IsNullOrEmpty(ModState.IsSystemActionPatch))
				{
                    ModInit.modLog.LogAtLevel(LogLevel.Log,$"[GetReleasedMapsAndEncountersByContractTypeAndOwnership_Patch] - found flag to skip implementation");
                    __runOriginal = true;
                    return;
                }
				if (!string.IsNullOrEmpty(ModState.SpecMapID))
				{
                    ModInit.modLog.LogAtLevel(LogLevel.Log,$"[GetReleasedMapsAndEncountersByContractTypeAndOwnership_Patch] - ignore biomes set to TRUE due to {ModState.SpecMapID}");
                    ModState.IgnoreBiomes = "TRUE";
				}
                
				List<MapAndEncounters> result = new List<MapAndEncounters>();
				string text = "SELECT m.*, el.* FROM EncounterLayer AS el ";
				text += "INNER JOIN ContractType as ct ON el.ContractTypeId = ct.ContractTypeId ";
				text += "INNER JOIN Map as m ON el.MapID = m.MapID ";
				text += "INNER JOIN BiomeSkin as bs ON m.BiomeSkinID = bs.BiomeSkinID ";
				text += "LEFT JOIN ContentPackItem as cpi ON m.MapID = cpi.ContentPackItemID ";
				text += "LEFT JOIN ContentPack as cp ON cpi.ContentPackID = cp.ContentPackID ";
				text += "WHERE el.IncludeInBuild = 1 AND m.IncludeInBuild = 1 AND ct.ContractTypeID = @ContractTypeID ";
				text += "AND (cp.IsOwned=1 OR cp.IsOwned IS NULL) ";
				if (ModState.IgnoreBiomes != "TRUE")
				{
					text += "AND bs.BiomeSkinID IN @Name ";
                    ModInit.modLog.LogAtLevel(LogLevel.Log,$"[GetReleasedMapsAndEncountersByContractTypeAndOwnership_Patch] - enforcing biomes");
                }
				if (ModState.SpecMapID != null)
				{
                    ModInit.modLog.LogAtLevel(LogLevel.Log,$"[GetReleasedMapsAndEncountersByContractTypeAndOwnership_Patch] - enforcing SpecMapID");
                    text += "AND m.MapID = @MapID ";
				}
				if (!includeUnpublishedContractTypes)
                {
                    text += "AND ct.IsPublished = 1 ";
                }
				text += "ORDER BY m.FriendlyName";
				mdd.Query<Map_MDD, EncounterLayer_MDD, MapAndEncounters>(text, delegate (Map_MDD m, EncounterLayer_MDD e)
				{
					MapAndEncounters mapAndEncounters = result.Find((MapAndEncounters x) => x.Map.MapID == m.MapID);
					if (mapAndEncounters == null)
					{
						mapAndEncounters = new MapAndEncounters(m);
						result.Add(mapAndEncounters);
                        ModInit.modLog.LogAtLevel(LogLevel.Log,$"[GetReleasedMapsAndEncountersByContractTypeAndOwnership_Patch] - added MapAndEncounters {string.Join("; ",mapAndEncounters.EncounterFriendlyNames())}");
                    }
					mapAndEncounters.AddEncounter(e);
                    ModInit.modLog.LogAtLevel(LogLevel.Log,$"[GetReleasedMapsAndEncountersByContractTypeAndOwnership_Patch] - AddEncounter with mapID {e.MapID} and encounterID {e.EncounterLayerID}");
                    return mapAndEncounters;
				}, new
				{
					ContractTypeID = contractTypeID,
					Name = ModState.AddContractBiomes.ToArray(),
					MapID = ModState.SpecMapID
					
				}, null, true, "MapID", null, null);

				result.Shuffle();
				__result = result;
                foreach (var r in __result)
                {
                    ModInit.modLog.LogAtLevel(LogLevel.Log,$"[GetReleasedMapsAndEncountersByContractTypeAndOwnership_Patch] - result MapAndEncounters BiomeSkinID {r.Map.BiomeSkinID} {string.Join("; ", r.EncounterFriendlyNames())}");
                }

                __runOriginal = false;
                return;
            }
		}

	}
}