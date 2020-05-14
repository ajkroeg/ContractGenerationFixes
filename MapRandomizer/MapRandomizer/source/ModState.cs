using BattleTech;
using System.Collections.Generic;

namespace MapRandomizer
{

    public static class ModState
    {
        
        public static string EnableGRMAEBCTAOPatch = null;
        public static List<Biome.BIOMESKIN> AddContractBiomes = null;
        public static void Reset()
        {
            EnableGRMAEBCTAOPatch = null;
            AddContractBiomes = null;
    }

    }
}