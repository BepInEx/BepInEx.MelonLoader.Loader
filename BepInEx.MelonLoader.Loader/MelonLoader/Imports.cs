using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.IL2CPP.Hook;
using MonoMod.RuntimeDetour;

namespace MelonLoader
{
    public static class Imports
    {
	    public static string GetCompanyName()
		    => Process.GetCurrentProcess().MainModule.FileVersionInfo.CompanyName;

        public static string GetProductName()
	        => Process.GetCurrentProcess().MainModule.FileVersionInfo.ProductName;

        public static string GetGameDirectory()
	        => Paths.GameRootPath;

        public static string GetGameDataDirectory()
	        => Path.Combine(Paths.GameRootPath, $"{Paths.ProcessName}_Data");

        public static string GetAssemblyDirectory()
	        => Path.Combine(Paths.GameRootPath, $"{Paths.ProcessName}_Data", "Managed");

        public static string GetExePath()
	        => Paths.ExecutablePath;

        public static bool IsIl2CppGame()
	        => true; // As far as I know, the only games that primarily use MelonLoader are IL2CPP so I don't care about mono support

        public static bool IsDebugMode()
	        => false;

        private static Dictionary<IntPtr, NativeDetour> InstalledHooks { get; } = new Dictionary<IntPtr, NativeDetour>();

        public static void Hook(IntPtr target, IntPtr detour)
        {
	        var newDetour = new NativeDetour(target, detour);
	        newDetour.Apply();
	        InstalledHooks[target] = newDetour;
        }

        public static void Unhook(IntPtr target, IntPtr detour)
        {
	        var fastNativeDetour = InstalledHooks[target];
	        fastNativeDetour.Dispose();
        }
    }
}