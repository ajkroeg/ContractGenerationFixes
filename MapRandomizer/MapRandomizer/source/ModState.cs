using BattleTech;
using System.Collections.Generic;

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
}