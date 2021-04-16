using System;
using System.IO;
using Harmony;
using System.Reflection;
using Newtonsoft.Json;

namespace MapRandomizer
{
    public static class ModInit
    {
        internal static Logger modLog;
        internal static string modDir;
        internal static Settings modSettings;
        public const string HarmonyPackage = "us.tbone.MapRandomizer";
        public static void Init(string directory, string settingsJSON)
        {
            modDir = directory;
            modLog = new Logger(modDir, "MapRandomizer", true);
            try
            {
                ModInit.modSettings = JsonConvert.DeserializeObject<Settings>(settingsJSON);

            }
            catch (Exception ex)
            {
                ModInit.modLog.LogException(ex);
                ModInit.modSettings = new Settings();
            }
            

            ModInit.modLog.LogMessage($"Initializing {HarmonyPackage} - Version {typeof(Settings).Assembly.GetName().Version}");
            var harmony = HarmonyInstance.Create(HarmonyPackage);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

        }
    }
    class Settings
    {
        public bool enableLogging = true;
        public bool enableTravelFix = false;
    }
}