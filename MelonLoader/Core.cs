using System;
using System.Diagnostics;
using MelonLoader.InternalUtils;
using MelonLoader.MonoInternals;
using bHapticsLib;
using BepInEx.Configuration;

#pragma warning disable IDE0051 // Prevent the IDE from complaining about private unreferenced methods

namespace MelonLoader
{
	internal static class Core
    {
        internal static HarmonyLib.Harmony HarmonyInstance;
        internal static bool IsIl2Cpp = false;

        internal static int Initialize(ConfigFile config, bool isIl2Cpp)
        {
            IsIl2Cpp = isIl2Cpp;
            MelonLaunchOptions.LoadConfig(config);

            AppDomain curDomain = AppDomain.CurrentDomain;

            if (MelonLaunchOptions.Core.EnableFixes)
            {
	            Fixes.UnhandledException.Install(curDomain);
	            Fixes.ServerCertificateValidation.Install();
            }

            MelonUtils.Setup(curDomain);
            Assertions.LemonAssertMapping.Setup();

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

            if (MelonLaunchOptions.Core.EnableCompatibilityLayers)
				MelonCompatibilityLayer.LoadModules();

            if (MelonLaunchOptions.Core.EnableBHapticsIntegration)
				bHapticsManager.Connect(BuildInfo.Name, UnityInformationHandler.GameName);

            MelonHandler.LoadMelonsFromDirectory<MelonPlugin>(MelonHandler.PluginsDirectory);
            MelonEvents.MelonHarmonyEarlyInit.Invoke();
            MelonEvents.OnPreInitialization.Invoke();

            return 0;
        }

        internal static int PreStart()
        {
            MelonEvents.OnApplicationEarlyStart.Invoke();
            
            if (MelonLaunchOptions.Core.EnableAssemblyGeneration && MelonUtils.IsGameIl2Cpp())
                Il2CppAssemblyGenerator.Run();
            return 1;
        }

        internal static int Start()
        {
            MelonEvents.OnPreModsLoaded.Invoke();
            MelonHandler.LoadMelonsFromDirectory<MelonMod>(MelonHandler.ModsDirectory);

            MelonEvents.OnPreSupportModule.Invoke();
            if (!SupportModule.Setup())
                return 1;

            AddUnityDebugLog();
            RegisterTypeInIl2Cpp.SetReady();

            MelonEvents.MelonHarmonyInit.Invoke();
            MelonEvents.OnApplicationStart.Invoke();

            return 0;
        }

        internal static void Quit()
        {
            MelonPreferences.Save();

            HarmonyInstance.UnpatchSelf();

            if (MelonLaunchOptions.Core.EnableBHapticsIntegration)
				bHapticsManager.Disconnect();

            MelonLogger.Flush();

            if (MelonLaunchOptions.Core.QuitFix)
                Process.GetCurrentProcess().Kill();
        }

        private static void AddUnityDebugLog()
        {
            var msg = "~   This Game has been MODIFIED using MelonLoader. DO NOT report any issues to the Game Developers!   ~";
            var line = new string('-', msg.Length);
            SupportModule.Interface.UnityDebugLog(line);
            SupportModule.Interface.UnityDebugLog(msg);
            SupportModule.Interface.UnityDebugLog(line);
        }
    }
}