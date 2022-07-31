using System;
using System.Diagnostics;
using BepInEx.Configuration;
using BepInEx.Logging;
using MelonLoader.InternalUtils;
using MelonLoader.MonoInternals;

namespace MelonLoader
{
    // Based on commit: 80025c4445b5791d1f8195de9b03d0e84c1954cb
	internal static class Core
    {
        internal static HarmonyLib.Harmony HarmonyInstance = null;
        internal static bool IsIl2Cpp = false;

        internal static int Initialize(ConfigFile config, bool isIl2Cpp)
        {
            IsIl2Cpp = isIl2Cpp;
            MelonLaunchOptions.Load(config);

            AppDomain curDomain = AppDomain.CurrentDomain;
            if (MelonLaunchOptions.Core.EnableFixes)
            {
                Fixes.UnhandledException.Install(curDomain);
                Fixes.ServerCertificateValidation.Install();    
            }

            MelonUtils.Setup(curDomain);
            Assertions.LemonAssertMapping.Setup();
            MelonHandler.Setup();

            if (!MonoResolveManager.Setup())
                return 1;

            HarmonyInstance = new HarmonyLib.Harmony(BuildInfo.Name);

            if (MelonLaunchOptions.Core.EnableFixes)
            {
                Fixes.ForcedCultureInfo.Install();
                Fixes.InstancePatchFix.Install();
                Fixes.ProcessFix.Install();    
            }

            if (MelonLaunchOptions.Core.EnablePatchShield)
                PatchShield.Install();

            MelonPreferences.Load();
            if (MelonLaunchOptions.Core.EnableBHapticsIntegration)
                bHaptics.Load();

            if (MelonLaunchOptions.Core.EnableCompatibilityLayers)
            {
                MelonCompatibilityLayer.Setup();
                MelonCompatibilityLayer.SetupModules(MelonCompatibilityLayer.SetupType.OnPreInitialization);    
            }

            MelonHandler.LoadPlugins();
            MelonHandler.OnPreInitialization();

            return 0;
        }

        internal static int PreStart()
        {
            MelonHandler.OnApplicationEarlyStart();
            if (MelonLaunchOptions.Core.EnableAssemblyGeneration && MelonUtils.IsGameIl2Cpp())
                Il2CppAssemblyGenerator.Run();
            return 1;
        }

        private static int Il2CppGameSetup()
            => (MelonUtils.IsGameIl2Cpp()
                && !Il2CppAssemblyGenerator.Run())
                ? 1 : 0;

        internal static int Start()
        {
            bHaptics.Start();

            MelonHandler.OnApplicationStart_Plugins();
            MelonHandler.LoadMods();
            MelonHandler.OnPreSupportModule();

            if (!SupportModule.Setup())
                return 1;
            
            if (MelonLaunchOptions.Core.EnableCompatibilityLayers)
                MelonCompatibilityLayer.SetupModules(MelonCompatibilityLayer.SetupType.OnApplicationStart);
            AddUnityDebugLog();
            MelonHandler.OnApplicationStart_Mods();
            MelonStartScreen.DisplayModLoadIssuesIfNeeded();

            return 0;
        }

        internal static void OnApplicationLateStart()
        {
            MelonHandler.OnApplicationLateStart_Plugins();
            MelonHandler.OnApplicationLateStart_Mods();
            MelonStartScreen.Finish();
        }

        internal static void Quit()
        {
            MelonHandler.OnApplicationQuit();
            MelonPreferences.Save();

            HarmonyInstance.UnpatchSelf();
            if (MelonLaunchOptions.Core.EnableBHapticsIntegration)
                bHaptics.Quit();

            MelonLogger.Flush();

            if (MelonLaunchOptions.Core.QuitFix)
                Process.GetCurrentProcess().Kill();
        }

        private static void AddUnityDebugLog()
        {
            SupportModule.Interface.UnityDebugLog("--------------------------------------------------------------------------------------------------");
            SupportModule.Interface.UnityDebugLog("~   This Game has been MODIFIED using MelonLoader. DO NOT report any issues to the Developers!   ~");
            SupportModule.Interface.UnityDebugLog("--------------------------------------------------------------------------------------------------");
        }
    }
}