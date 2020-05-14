using Harmony;
using System.Reflection;
using System.Collections.Generic;
using BattleTech.Data;

static MapRandomizerPatches()
{
    HarmonyInstance harmony = HarmonyInstance.Create("MapRandomizerPatcher");

    MethodInfo targetmethod = AccessTools.Method(typeof(BattleTech.Data.MapsAndEncounters_MDDExtensions), "GetReleasedMapsAndEncountersByContractTypeAndOwnership");
    HarmonyMethod prefixmethod = new HarmonyMethod(typeof(MapRandomizer.MapRandomizerPatches).GetMethod("MapRandomizer_Prefix"));
    harmony.Patch(targetmethod, prefixmethod, null);
}
