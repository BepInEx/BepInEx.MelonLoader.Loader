using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace MelonLoader
{
    public static class MelonLaunchOptions
    {
        private static Dictionary<string, Action> WithoutArg = new Dictionary<string, Action>();
        private static Dictionary<string, Action<string>> WithArg = new Dictionary<string, Action<string>>();

        static MelonLaunchOptions()
        {
            AnalyticsBlocker.Setup();
            Core.Setup();
            Console.Setup();
            Debug.Setup();
            Il2CppAssemblyGenerator.Setup();
            Logger.Setup();
        }

        internal static void LoadConfig(ConfigFile config)
        {
            Core.DebugMode =
                config.Bind("LaunchArguments", "DebugMode", false, "Launches MelonLoader in debug mode, i.e. makes everything more verbose").Value;
            const string loadModeDescription = "Sets the loading mode for {0}.\nNORMAL: Does not load {0} ending with .dev.dll\nDEV: Only loads {0} ending with .dev.dll\nBOTH: Loads all .dll files";
            Core.LoadMode_Plugins =
                config.Bind("LaunchArguments", "LoadMode_Plugins", Core.LoadModeEnum.NORMAL, string.Format(loadModeDescription, "plugins")).Value;
            Core.LoadMode_Mods =
                config.Bind("LaunchArguments", "LoadMode_Mods", Core.LoadModeEnum.NORMAL, string.Format(loadModeDescription, "mods")).Value;

            Core.QuitFix =
                config.Bind("LaunchArguments", "QuitFix", false, "Ensures that if a mod / plugin / MelonLoader itself requests the game to close, the game's process will be forcefully terminated if it does not close").Value;

            Core.StartScreen =
                config.Bind("LaunchArguments", "ShowStartScreen", false, "Enables the MelonLoader loading screen on game boot.").Value;
            
            Core.EnablePatchShield =
                config.Bind("Framework", "EnablePatchShield", true, "If true, configures Harmony in such a way that prevents patching critical / sensitive code. Some plugins will refuse to load if this is disabled.").Value;

            Core.EnableCompatibilityLayers =
                config.Bind("Framework", "EnableCompatibilityLayers", true, "If true, MelonLoader will load compatibility layer modules so it can load other modloader plugins (such as IPA and MDML), and other misc. integrations.\nIs required to remain true, as disabling this seems to break regular plugin loading.").Value;

            Core.EnableBHapticsIntegration =
                config.Bind("Framework", "EnableBHapticsIntegration", true, "If true, MelonLoader will load its BHaptics library module.").Value;
            
            Core.EnableAssemblyGeneration =
                config.Bind("Framework", "EnableAssemblyGeneration", false, "If true, MelonLoader will generate it's own set of unhollowed assemblies alongside BepInEx.").Value;
            
            Core.EnableFixes =
                config.Bind("Framework", "EnableFixes", false, "If true, MelonLoader's Unity fixes will be applied. Untested and could possibly cause issues with BepInEx code.").Value;
        }

        internal static void LoadCLI()
        {
            List<string> foundOptions = new List<string>();

            LemonEnumerator<string> argEnumerator = new LemonEnumerator<string>(Environment.GetCommandLineArgs());
            while (argEnumerator.MoveNext())
            {
                string fullcmd = argEnumerator.Current;
                if (string.IsNullOrEmpty(fullcmd))
                    continue;

                if (!fullcmd.StartsWith("--"))
                    continue;

                string cmd = fullcmd.Remove(0, 2);

                if (WithoutArg.TryGetValue(cmd, out Action withoutArgFunc))
                {
                    foundOptions.Add(fullcmd);
                    withoutArgFunc();
                }
                else if (WithArg.TryGetValue(cmd, out Action<string> withArgFunc))
                {
                    if (!argEnumerator.MoveNext())
                        continue;

                    string cmdArg = argEnumerator.Current;
                    if (string.IsNullOrEmpty(cmdArg))
                        continue;

                    if (cmdArg.StartsWith("--"))
                        continue;

                    foundOptions.Add($"{fullcmd} = {cmdArg}");
                    withArgFunc(cmdArg);
                }
            }

            if (foundOptions.Count <= 0)
                return;

            MelonLogger.WriteLine(ConsoleColor.Magenta);
            MelonLogger.Msg(ConsoleColor.Cyan, "Launch Options:");
            foreach (string cmd in foundOptions)
                MelonLogger.Msg($"\t{cmd}");
            MelonLogger.WriteLine(ConsoleColor.Magenta);
            MelonLogger.WriteSpacer();
        }

#region Args
        public static class AnalyticsBlocker
        {
            public static bool ShouldDAB { get; internal set; }

            internal static void Setup()
            {
                WithoutArg["melonloader.dab"] = () => ShouldDAB = true;

            }
        }

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
            public static bool QuitFix { get; internal set; }
            public static bool StartScreen { get; internal set; } = true;
            public static string UnityVersion { get; internal set; }
            public static bool EnablePatchShield { get; internal set; }
            public static bool EnableCompatibilityLayers { get; internal set; }
            public static bool EnableBHapticsIntegration { get; internal set; }
            public static bool EnableAssemblyGeneration { get; internal set; }
            public static bool EnableFixes { get; internal set; }
            public static bool DebugMode { get; internal set; }

            internal static void Setup()
            {
                WithoutArg["quitfix"] = () => QuitFix = true;
                WithoutArg["melonloader.disablestartscreen"] = () => StartScreen = false;
                WithArg["melonloader.loadmodeplugins"] = (string arg) =>
                {
                    if (int.TryParse(arg, out int valueint))
                        LoadMode_Plugins = (LoadModeEnum)MelonUtils.Clamp(valueint, (int)LoadModeEnum.NORMAL, (int)LoadModeEnum.BOTH);
                };
                WithArg["melonloader.loadmodemods"] = (string arg) =>
                {
                    if (int.TryParse(arg, out int valueint))
                        LoadMode_Mods = (LoadModeEnum)MelonUtils.Clamp(valueint, (int)LoadModeEnum.NORMAL, (int)LoadModeEnum.BOTH);
                };
                WithArg["melonloader.unityversion"] = (string arg) => UnityVersion = arg;
            }
        }

        public static class Console
        {
            public enum DisplayMode
            {
                NORMAL,
                MAGENTA,
                RAINBOW,
                RANDOMRAINBOW,
                LEMON
            };
            public static DisplayMode Mode { get; internal set; }
            public static bool CleanUnityLogs { get; internal set; } = true;
            public static bool ShouldSetTitle { get; internal set; } = true;
            public static bool AlwaysOnTop { get; internal set; }
            public static bool ShouldHide { get; internal set; }
            public static bool HideWarnings { get; internal set; }

            internal static void Setup()
            {
                WithoutArg["melonloader.disableunityclc"] = () => CleanUnityLogs = false;
                WithoutArg["melonloader.consoledst"] = () => ShouldSetTitle = false;
                WithoutArg["melonloader.consoleontop"] = () => AlwaysOnTop = true;
                WithoutArg["melonloader.hideconsole"] = () => ShouldHide = true;
                WithoutArg["melonloader.hidewarnings"] = () => HideWarnings = true;

                WithArg["melonloader.consolemode"] = (string arg) =>
                {
                    if (int.TryParse(arg, out int valueint))
                        Mode = (DisplayMode)MelonUtils.Clamp(valueint, (int)DisplayMode.NORMAL, (int)DisplayMode.LEMON);
                };
            }
        }

        public static class Debug
        {
            public static bool Enabled { get; internal set; }

            internal static void Setup()
            {
                WithoutArg["melonloader.debug"] = () => Enabled = true;

            }
        }

        public static class Il2CppAssemblyGenerator
        {
            public static bool ForceRegeneration { get; internal set; }
            public static bool OfflineMode { get; internal set; }
            public static string ForceVersion_Dumper { get; internal set; }
            public static string ForceVersion_Il2CppAssemblyUnhollower { get; internal set; }
            public static string ForceRegex { get; internal set; }

            internal static void Setup()
            {
                WithoutArg["melonloader.agfoffline"] = () => OfflineMode = true;
                WithoutArg["melonloader.agfregenerate"] = () => ForceRegeneration = true;
                WithArg["melonloader.agfvdumper"] = (string arg) => ForceVersion_Dumper = arg;
                WithArg["melonloader.agfvunhollower"] = (string arg) => ForceVersion_Il2CppAssemblyUnhollower = arg;
                WithArg["melonloader.agfregex"] = (string arg) => ForceRegex = arg;
            }
        }

        public static class Logger
        {
            public static int MaxLogs { get; internal set; } = 10;
            public static int MaxWarnings { get; internal set; } = 10;
            public static int MaxErrors { get; internal set; } = 10;

            internal static void Setup()
            {
                WithArg["melonloader.maxlogs"] = (string arg) =>
                {
                    if (int.TryParse(arg, out int valueint))
                        MaxLogs = valueint;
                };
                WithArg["melonloader.maxwarnings"] = (string arg) =>
                {
                    if (int.TryParse(arg, out int valueint))
                        MaxWarnings = valueint;
                };
                WithArg["melonloader.maxerrors"] = (string arg) =>
                {
                    if (int.TryParse(arg, out int valueint))
                        MaxErrors = valueint;
                };
            }
        }
        #endregion
    }
}