using BattleTech;
using System.Collections.Generic;

namespace MapRandomizer
{

    public static class ModState
    {
        
        public static string EnableGRMAEBCTAOPatch = null;
        public static List<Biome.BIOMESKIN> AddContractBiomes = null;
        public static string SpecMapID = null;
        public static string IgnoreBiomes = null;
        public static void Reset()
        {
            EnableGRMAEBCTAOPatch = null;
            AddContractBiomes = null;
            SpecMapID = null;
            IgnoreBiomes = null;

        }

    }
}