using Harmony;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using BattleTech.Data;
using BattleTech;

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
		[HarmonyPatch(typeof(BattleTech.SimGameState), "AddContract")]
		public static class AddContract_Patch
		{
			public static void Prefix(ref SimGameState __instance, ref bool __result, Dictionary<string, StarSystem> ___starDict, SimGameState.AddContractData contractData)
			{
				ModState.EnableGRMAEBCTAOPatch = contractData.ContractName;
				
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

			public static void Postfix(ref SimGameState __instance, ref bool __result, SimGameState.AddContractData contractData)
			{
				if (ModState.EnableGRMAEBCTAOPatch != null)
				{
					ModState.EnableGRMAEBCTAOPatch = null;
				}
				if (ModState.AddContractBiomes != null)
				{
					ModState.AddContractBiomes = null;
				}

			}

		}

		[HarmonyPatch(typeof(BattleTech.Data.MapsAndEncounters_MDDExtensions), "GetReleasedMapsAndEncountersByContractTypeAndOwnership")]
		public static class GetReleasedMapsAndEncountersByContractTypeAndOwnership_Patch
		{
			public static bool Prefix(ref List<MapAndEncounters> __result, MetadataDatabase mdd, int contractTypeID, bool includeUnpublishedContractTypes)

			{
				if (ModState.EnableGRMAEBCTAOPatch == null) return true;

				List<MapAndEncounters> result = new List<MapAndEncounters>();
				string text = "SELECT m.*, el.* FROM EncounterLayer AS el ";
				text += "INNER JOIN ContractType as ct ON el.ContractTypeId = ct.ContractTypeId ";
				text += "INNER JOIN Map as m ON el.MapID = m.MapID ";
				text += "INNER JOIN BiomeSkin as bs ON m.BiomeSkinID = bs.BiomeSkinID ";
				text += "LEFT JOIN ContentPackItem as cpi ON m.MapID = cpi.ContentPackItemID ";
				text += "LEFT JOIN ContentPack as cp ON cpi.ContentPackID = cp.ContentPackID ";
				text += "WHERE el.IncludeInBuild = 1 AND m.IncludeInBuild = 1 AND ct.ContractTypeID = @ContractTypeID ";
				text += "AND (cp.IsOwned=1 OR cp.IsOwned IS NULL) ";
				text += "AND bs.BiomeSkinID IN @Name ";
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
					Name = ModState.AddContractBiomes.ToArray()
				}, null, true, "MapID", null, null); ;

				result.Shuffle();
				__result = result;
				return false;
			}
		}

	}
}