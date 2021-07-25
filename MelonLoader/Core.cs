using System;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;

[assembly: InternalsVisibleTo("BepInEx.MelonLoader.Loader")]

namespace MelonLoader
{
    internal static class Core
    {
        internal static HarmonyLib.Harmony HarmonyInstance = null;

        internal static int Initialize(ConfigFile configFile)
        {
            AppDomain curDomain = AppDomain.CurrentDomain;
            HarmonyInstance = new HarmonyLib.Harmony(BuildInfo.Name);

            if (MelonLaunchOptions.Core.EnableFixes)
            {
                Fixes.UnhandledException.Run(curDomain);
                Fixes.InvariantCurrentCulture.Install();
            }

            try { MelonUtils.Setup(); } catch (Exception ex) { MelonLogger.Error($"MelonUtils.Setup Exception: {ex}"); throw; }

            if (MelonLaunchOptions.Core.EnableFixes)
            {
                Fixes.ApplicationBase.Run(curDomain);
                Fixes.ExtraCleanup.Run();
            }

            MelonPreferences.Load();
            MelonLaunchOptions.Load(configFile);

            if (MelonLaunchOptions.Core.EnableCompatibilityLayers)
                MelonCompatibilityLayer.Setup();
            
            if (MelonLaunchOptions.Core.EnablePatchShield)
                PatchShield.Install();

            if (MelonLaunchOptions.Core.EnableBHapticsIntegration)
                bHaptics.Load();

            if (MelonLaunchOptions.Core.EnableCompatibilityLayers)
                MelonCompatibilityLayer.SetupModules(MelonCompatibilityLayer.SetupType.OnPreInitialization);

            MelonHandler.LoadPlugins();
            MelonHandler.OnPreInitialization();

            return 0;
        }

        internal static int PreStart()
        {
            if (!MelonUtils.IsGameIl2Cpp())
                GameVersionHandler.Setup();

            MelonHandler.OnApplicationEarlyStart();

            if (MelonUtils.IsGameIl2Cpp())
            {
                if (MelonLaunchOptions.Core.EnableAssemblyGeneration)
                {
                    if (!Il2CppAssemblyGenerator.Run())
                        return 1;
                }

                HarmonyLib.Public.Patching.PatchManager.ResolvePatcher += HarmonyIl2CppMethodPatcher.TryResolve;

                GameVersionHandler.Setup();
            }

            return 0;
        }

        internal static int Start()
        {
            if (!SupportModule.Initialize())
                return 1;

            AddUnityDebugLog();

            if (MelonLaunchOptions.Core.EnableBHapticsIntegration)
                bHaptics.Start();

            if (MelonLaunchOptions.Core.EnableCompatibilityLayers)
                MelonCompatibilityLayer.SetupModules(MelonCompatibilityLayer.SetupType.OnApplicationStart);

            MelonHandler.OnApplicationStart_Plugins();
            MelonHandler.LoadMods();
            MelonHandler.OnApplicationStart_Mods();

            MelonHandler.OnApplicationLateStart_Plugins();
            MelonHandler.OnApplicationLateStart_Mods();

            return 0;
        }

        internal static void Quit()
        {
            MelonHandler.OnApplicationQuit();
            MelonPreferences.Save();

            HarmonyInstance.UnpatchAll();
            bHaptics.Quit();
            
            Fixes.QuitFix.Run();
        }

        private static void AddUnityDebugLog()
        {
            SupportModule.Interface.UnityDebugLog("--------------------------------------------------------------------------------------------------");
            SupportModule.Interface.UnityDebugLog("~   This Game has been MODIFIED using MelonLoader. DO NOT report any issues to the Developers!   ~");
            SupportModule.Interface.UnityDebugLog("--------------------------------------------------------------------------------------------------");
        }
    }
}