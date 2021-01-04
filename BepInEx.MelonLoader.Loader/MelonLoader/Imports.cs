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
	internal static class AppInfo
	{
		public static readonly string CompanyName;
		public static readonly string ProductName;
		
		static AppInfo()
		{
			// Why app.info? It's not even present on a majority of Unity games.
			// Yes, some developers delete them manually
			// Oh well...
			var appInfoPath = Path.Combine(Imports.GetGameDataDirectory(), "app.info");
			if (!File.Exists(appInfoPath))
				return;
			var lines = File.ReadAllLines(appInfoPath);
			string Read(string[] l, int i) => l.Length > i ? lines[i] : null;
			CompanyName = Read(lines, 0);
			ProductName = Read(lines, 1);
		}
	}
	
    public static class Imports
    {
	    private const string UnknownItem = "UNKNOWN";
	    
	    public static string GetCompanyName()
		    => AppInfo.CompanyName ?? UnknownItem;

        public static string GetProductName()
	        => AppInfo.ProductName ?? UnknownItem;

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

        private static Dictionary<IntPtr, FastNativeDetour> InstalledHooks { get; } = new Dictionary<IntPtr, FastNativeDetour>();

        public static void Hook(IntPtr target, IntPtr detour)
        {
	        var newDetour = new FastNativeDetour(target, detour);
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