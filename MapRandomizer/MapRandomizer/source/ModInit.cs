using Harmony;
using System.Reflection;

namespace MapRandomizer
{

    public static class Mod
    {
        public const string HarmonyPackage = "us.tbone.MapRandomizer";
        public static void Init(string directory, string settingsJSON)
        {
            var harmony = HarmonyInstance.Create(HarmonyPackage);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

    }
}