using Harmony;
using BattleTech;

namespace MapRandomizer.Patches
{
	static class MapRandomizerPatches_Contracts
	{
		[HarmonyPatch(typeof(BattleTech.SimGameState), "OnDayPassed")]
		public static class OnDayPassed_Patch
		{
			public static void Postfix(SimGameState __instance, int timeLapse)
			{
				__instance.CompanyStats.AddStatistic<int>("HasTravelContract", 0);
				if (__instance.HasTravelContract==true)
                {
					__instance.CompanyStats.Set<int>("HasTravelContract", 1);
				}
                else
                {
					__instance.CompanyStats.Set<int>("HasTravelContract", 0);
				}

			}
		}
	}
}
