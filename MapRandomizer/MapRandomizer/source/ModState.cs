using System.Collections.Concurrent;
using System.Collections.Generic;
using BattleTech;
using BattleTech.Framework;
using HBS.Collections;
using MapRandomizer.source;
using Newtonsoft.Json.Linq;
using static MapRandomizer.source.Classes;

namespace MapRandomizer
{
    public static class ModState
    {
        public static string IsSystemActionPatch = null;
        public static List<Biome.BIOMESKIN> AddContractBiomes = null;
        public static string SpecMapID = null;
        public static string IgnoreBiomes = null;
        public static int CustomDifficulty = 0;
        public static int SysAdjustDifficulty = 0;
        public static Dictionary<string, int> SavedDiffs = new Dictionary<string, int>();
        public static Dictionary<string, int> SavedDiffOverrides = new Dictionary<string, int>();

        public static Dictionary<ContractOverride, string> OverrideIDCache = new Dictionary<ContractOverride, string>();

        public static void Reset()
        {
            IsSystemActionPatch = null;
            AddContractBiomes = null;
            SpecMapID = null;
            IgnoreBiomes = null;
            CustomDifficulty = 0;
            SysAdjustDifficulty = 0;
            SavedDiffs = new Dictionary<string, int>();
        }
    }
    public static class OverrideExtensionManager
    {
        public static PendingContractOverrideExtension PendingExtension = new PendingContractOverrideExtension();

        public static ConcurrentDictionary<string, ContractOverrideExtension> ContractOverrideExtensionDict = new ConcurrentDictionary<string, ContractOverrideExtension>();
        public static bool GetContractOverrideExtension(this ContractOverride contractOverride, out ContractOverrideExtension extension)
        {
            extension = new ContractOverrideExtension();
            var extensionID = contractOverride.ID;
            if (string.IsNullOrEmpty(extensionID)) extensionID = contractOverride.FetchCachedOverrideID();
            if (string.IsNullOrEmpty(extensionID)) return false;
            if (ContractOverrideExtensionDict.TryGetValue(extensionID, out var contractOverrideExtension))
            {
                extension = contractOverrideExtension;
                return true;
            }
            return false;
        }

        public static void ResetContractExtension()
        {
            PendingExtension = new PendingContractOverrideExtension();
        }

        public static SimGameEventResult ProcessExpirationResult(this JObject jObject)
        {
            var simResult = new SimGameEventResult();

            simResult.Scope = jObject["Scope"].ToObject<EventScope>();
            simResult.Requirements = jObject["Requirements"].ToObject<RequirementDef>();
            simResult.AddedTags = new TagSet();
            simResult.AddedTags.FromJSON(jObject["AddedTags"].ToString());
            simResult.RemovedTags = new TagSet();
            simResult.RemovedTags.FromJSON(jObject["RemovedTags"].ToString());

            simResult.Stats = jObject["Stats"].ToObject<SimGameStat[]>();
            simResult.Actions = jObject["Actions"].ToObject<SimGameResultAction[]>();
            simResult.ForceEvents = jObject["ForceEvents"].ToObject<SimGameForcedEvent[]>();
            simResult.TemporaryResult = jObject["TemporaryResult"].ToObject<bool>();
            simResult.ResultDuration = jObject["ResultDuration"].ToObject<int>();

            return simResult;
        }
    }
}