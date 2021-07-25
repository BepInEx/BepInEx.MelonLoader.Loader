using BepInEx.Configuration;

namespace MelonLoader
{
    public static class MelonLaunchOptions
    {
        internal static void Load(ConfigFile configFile)
        {
            Core.DebugMode =
                configFile.Bind("LaunchArguments", "DebugMode", false, "Launches MelonLoader in debug mode, i.e. makes everything more verbose").Value;

            const string loadModeDescription = "Sets the loading mode for {0}.\nNORMAL: Does not load {0} ending with .dev.dll\nDEV: Only loads {0} ending with .dev.dll\nBOTH: Loads all .dll files";

            Core.LoadMode_Plugins =
                configFile.Bind("LaunchArguments", "LoadMode_Plugins", Core.LoadModeEnum.NORMAL, string.Format(loadModeDescription, "plugins")).Value;
            Core.LoadMode_Mods =
                configFile.Bind("LaunchArguments", "LoadMode_Mods", Core.LoadModeEnum.NORMAL, string.Format(loadModeDescription, "mods")).Value;

            Core.QuitFix =
                configFile.Bind("LaunchArguments", "QuitFix", false, "Ensures that if a mod / plugin / MelonLoader itself requests the game to close, the game's process will be forcefully terminated if it does not close").Value;
            
            Core.EnablePatchShield =
                configFile.Bind("Framework", "EnablePatchShield", true, "If true, configures Harmony in such a way that prevents patching critical / sensitive code. Many VRCMG plugins will refuse to load if this is disabled.").Value;

            Core.EnableCompatibilityLayers =
                configFile.Bind("Framework", "EnableCompatibilityLayers", true, "If true, MelonLoader will load compatibility layer modules so it can load other modloader plugins (such as IPA and MDML), and other misc. integrations.\nIs required to remain true, as disabling this seems to break regular plugin loading.").Value;

            Core.EnableBHapticsIntegration =
                configFile.Bind("Framework", "EnableBHapticsIntegration", true, "If true, MelonLoader will load its BHaptics library module.").Value;
            
            Core.EnableAssemblyGeneration =
                configFile.Bind("Framework", "EnableAssemblyGeneration", false, "If true, MelonLoader will generate it's own set of unhollowed assemblies alongside BepInEx.").Value;
        }

        #region Args
        public static class Core
        {
            public enum LoadModeEnum
            {
                NORMAL,
                DEV,
                BOTH
            }
            public static LoadModeEnum LoadMode_Plugins { get; internal set; }
            public static LoadModeEnum LoadMode_Mods { get; internal set; }
            public static bool DebugMode { get; internal set; }
            public static bool QuitFix { get; internal set; }
            public static bool EnablePatchShield { get; internal set; }
            public static bool EnableCompatibilityLayers { get; internal set; }
            public static bool EnableBHapticsIntegration { get; internal set; }
            public static bool EnableAssemblyGeneration { get; internal set; }
        }

        public static class Il2CppAssemblyGenerator
        {
            public static bool ForceRegeneration { get; internal set; }
            public static bool OfflineMode { get; internal set; }
            public static string ForceVersion_Dumper { get; internal set; }
            public static string ForceVersion_Il2CppAssemblyUnhollower { get; internal set; }
            public static string ForceVersion_UnityDependencies { get; internal set; }
        }
#endregion
    }
}