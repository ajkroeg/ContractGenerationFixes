using Harmony;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using BattleTech.Data;
using BattleTech;
using JetBrains.Annotations;
using System.Runtime.Remoting.Messaging;
using System;
using UnityEngine;

namespace MapRandomizer.Patches
{
	static class MapRandomizerPatches
	{
		private static System.Random rng = new System.Random();
		public static void Shuffle<T>(this IList<T> list)

		{
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = rng.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}

		[HarmonyPatch(typeof(BattleTech.SimGameState), "ParseContractActionData")]
		public static class ParseContractActionData_Patch
		{
			public static bool Prefix(SimGameState __instance, ref bool __result, string actionValue, string[] additionalValues)
			{
				ModState.IsSystemActionPatch = actionValue;
				ModState.SpecMapID = additionalValues.ElementAtOrDefault(4);
				ModState.IgnoreBiomes = additionalValues.ElementAtOrDefault(5);
				ModState.CustomDifficulty = Convert.ToInt32(additionalValues.ElementAtOrDefault(6));
				ModState.SysAdjustDifficulty = Convert.ToInt32(additionalValues.ElementAtOrDefault(7));
				return true;
			}
		}

		


		[HarmonyPatch(typeof(BattleTech.SimGameState), "AddContract")]
		public static class AddContract_Patch
		{
			public static void Prefix(SimGameState __instance, ref bool __result, Dictionary<string, StarSystem> ___starDict, SimGameState.AddContractData contractData)
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

			public static void Postfix(SimGameState __instance, ref bool __result, SimGameState.AddContractData contractData)
			{
				if (ModState.IsSystemActionPatch != null)
				{
					ModState.IsSystemActionPatch = null;
				}
				if (ModState.AddContractBiomes != null)
				{
					ModState.AddContractBiomes = null;
				}
				if (ModState.IgnoreBiomes != null)
				{
					ModState.IgnoreBiomes = null;
				}
				if (ModState.SpecMapID !=null)
				{
					ModState.SpecMapID = null;
				}
				if (ModState.CustomDifficulty != 0)
				{
					ModState.CustomDifficulty = 0;
				}
			}

		}
		[HarmonyPatch(typeof(BattleTech.SimGameState), "PrepContract")]
		public static class PrepContractPatch
		{
			public static bool Prefix(SimGameState __instance, Contract contract, FactionValue employer, FactionValue employersAlly, FactionValue target, FactionValue targetsAlly, FactionValue NeutralToAll, FactionValue HostileToAll, Biome.BIOMESKIN skin, int presetSeed, StarSystem system)
			{
				{
					if (ModState.IsSystemActionPatch == null)
					{
						return true;
					}
					//if (presetSeed != 0 && !contract.IsPriorityContract)
					{
						int baseDiff = system.Def.GetDifficulty(__instance.SimGameMode) + Mathf.FloorToInt(__instance.GlobalDifficulty);
						int min;
						int num;
						min = baseDiff - 1;
						num = baseDiff + 1;
						int finalDifficulty = new NetworkRandom
						{
							seed = presetSeed
						}.Int(min, num + 1);
						contract.SetFinalDifficulty(finalDifficulty);
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
					}
					else
					{
						num2 = __instance.CalculateContractValueByContractType(contract.ContractTypeValue, finalDifficulty2, (float)__instance.Constants.Finances.ContractPricePerDifficulty, __instance.Constants.Finances.ContractPriceVariance, presetSeed);
					}
					num2 = SimGameState.RoundTo((float)num2, 1000);
					contract.SetInitialReward(num2);
					contract.SetBiomeSkin(skin);
				}
				return false;
			}
		}


		[HarmonyPatch(typeof(BattleTech.StarSystemDef), "GetDifficulty")]
		public static class GetDifficultyPatch
		{
			public static bool Prefix(StarSystemDef __instance, SimGameState.SimGameType type, ref int __result)
			{
				if (ModState.IsSystemActionPatch == null)
				{
					return true;
				}
				
				if (ModState.CustomDifficulty > 0)
				{
					__result = ModState.CustomDifficulty;
					return false;
				}
				if (ModState.SysAdjustDifficulty != 0)
				{
					var SysDefaultDiff = Traverse.Create(__instance).Field("DefaultDifficulty").GetValue<int>();
					var SysDiffModes = Traverse.Create(__instance).Field("DifficultyModes").GetValue<List<SimGameState.SimGameType>>();
					var SysDiffList = Traverse.Create(__instance).Field("DifficultyList").GetValue<List<int>>();
					

					if (SysDiffModes != null && SysDiffModes.Contains(type))
					{
						__result = SysDiffList[SysDiffModes.IndexOf(type)];
					}
					__result = ModState.SysAdjustDifficulty + SysDefaultDiff;
				}
				return false;
			}
		}

		[HarmonyPatch(typeof(BattleTech.Data.MapsAndEncounters_MDDExtensions), "GetReleasedMapsAndEncountersByContractTypeAndOwnership")]
		public static class GetReleasedMapsAndEncountersByContractTypeAndOwnership_Patch
		{
			public static bool Prefix(ref List<MapAndEncounters> __result, MetadataDatabase mdd, int contractTypeID, bool includeUnpublishedContractTypes)

			{
				if (ModState.IsSystemActionPatch == null)
				{
					return true;
				}
				if (ModState.SpecMapID != null)
				{
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
				}
				if (ModState.SpecMapID != null)
				{
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
					}
					mapAndEncounters.AddEncounter(e);

					return mapAndEncounters;
				}, new
				{
					ContractTypeID = contractTypeID,
					Name = ModState.AddContractBiomes.ToArray(),
					MapID = ModState.SpecMapID
					
				}, null, true, "MapID", null, null); ;

				result.Shuffle();
				__result = result;
				return false;
			}
		}

	}
}