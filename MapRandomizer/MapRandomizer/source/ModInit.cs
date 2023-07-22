using System;
using System.Reflection;
using HBS.Logging;
using Newtonsoft.Json;
using IRBTModUtils.Logging;

namespace MapRandomizer
{
    public static class ModInit
    {
        //internal static DeferringLogger modLog;
        internal static ILog modLog;
        
        internal static string modDir;
        internal static Settings modSettings;
        public const string HarmonyPackage = "us.tbone.MapRandomizer";
        public static void Init(string directory, string settingsJSON)
        {
            modDir = directory;
            modLog = Logger.GetLogger("MapRandomizer");
            //modLog = new DeferringLogger(modDir, "MapRandomizer", true, true);
            try
            {
                ModInit.modSettings = JsonConvert.DeserializeObject<Settings>(settingsJSON);
            }
            catch (Exception ex)
            {
                ModInit.modLog.LogAtLevel(LogLevel.Error, ex);
                //ModInit.modLog?.Error?.Write(ex);
                ModInit.modSettings = new Settings();
            }
            

            ModInit.modLog.LogAtLevel(LogLevel.Log,$"Initializing {HarmonyPackage} - Version {typeof(Settings).Assembly.GetName().Version}");
            //var harmony = HarmonyInstance.Create(HarmonyPackage);
            //harmony.PatchAll(Assembly.GetExecutingAssembly());
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), HarmonyPackage);

        }
    }
    class Settings
    {
        public bool enableLogging = true;
        public bool enableTravelFix = false;
        public string ContractTimeoutIcon = "";
    }
}