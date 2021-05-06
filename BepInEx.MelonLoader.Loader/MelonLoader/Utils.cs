using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using MelonLoader.TinyJSON;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
#pragma warning disable 0618

namespace MelonLoader
{
	public static class MelonUtils
    {
        internal static void Setup()
        {
            GameDeveloper = string.Copy(Internal_GetGameDeveloper());
            GameName = string.Copy(Internal_GetGameName());
            HashCode = string.Copy(Internal_GetHashCode());
            CurrentGameAttribute = new MelonGameAttribute(GameDeveloper, GameName);
            GameDirectory = string.Copy(Internal_GetGameDirectory());
            UserDataDirectory = Path.Combine(GameDirectory, "UserData");
            if (!Directory.Exists(UserDataDirectory))
                Directory.CreateDirectory(UserDataDirectory);
            Main.IsBoneworks = IsBONEWORKS;
        }

        public static string GameDirectory { get; private set; }
        public static string UserDataDirectory { get; private set; }
        public static MelonGameAttribute CurrentGameAttribute { get; private set; }
        public static string GameDeveloper { get; private set; }
        public static string GameName { get; private set; }
        public static bool IsBONEWORKS { get => (!string.IsNullOrEmpty(GameDeveloper) && GameDeveloper.Equals("Stress Level Zero") && !string.IsNullOrEmpty(GameName) && GameName.Equals("BONEWORKS")); }
        public static bool IsVRChat { get => (!string.IsNullOrEmpty(GameDeveloper) && GameDeveloper.Equals("VRChat") && !string.IsNullOrEmpty(GameName) && GameName.Equals("VRChat")); }
        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T> { if (value.CompareTo(min) < 0) return min; if (value.CompareTo(max) > 0) return max; return value; }
        public static string HashCode { get; private set; }

        public static string RandomString(int length)
        {
            StringBuilder builder = new StringBuilder();
            Random rand = new Random();
            for (int i = 0; i < length; i++)
                builder.Append(Convert.ToChar(Convert.ToInt32(Math.Floor(25 * rand.NextDouble())) + 65));
            return builder.ToString();
        }

        public static MelonBase GetMelonFromStackTrace()
        {
            StackTrace st = new StackTrace(3, true);
            if (st.FrameCount <= 0)
                return null;
            MelonBase output = CheckForMelonInFrame(st);
            if (output == null)
                output = CheckForMelonInFrame(st, 1);
            if (output == null)
                output = CheckForMelonInFrame(st, 2);
            return output;
        }
        private static MelonBase CheckForMelonInFrame(StackTrace st, int frame = 0)
        {
            StackFrame sf = st.GetFrame(frame);
            if (sf == null)
                return null;
            MethodBase method = sf.GetMethod();
            if (method == null)
                return null;
            Type methodClassType = method.DeclaringType;
            if (methodClassType == null)
                return null;
            Assembly asm = methodClassType.Assembly;
            if (asm == null)
                return null;
            MelonBase melon = MelonHandler.Plugins.Find(x => (x.Assembly == asm));
            if (melon == null)
                melon = MelonHandler.Mods.Find(x => (x.Assembly == asm));
            return melon;
        }

        public static string ColorToANSI(ConsoleColor color)
        {
            return color switch
            {
                ConsoleColor.Black => "\x1b[30m",
                ConsoleColor.DarkBlue => "\x1b[34m",
                ConsoleColor.DarkGreen => "\x1b[32m",
                ConsoleColor.DarkCyan => "\x1b[36m",
                ConsoleColor.DarkRed => "\x1b[31m",
                ConsoleColor.DarkMagenta => "\x1b[35m",
                ConsoleColor.DarkYellow => "\x1b[33m",
                ConsoleColor.Gray => "\x1b[37m",
                ConsoleColor.DarkGray => "\x1b[90m",
                ConsoleColor.Blue => "\x1b[94m",
                ConsoleColor.Green => "\x1b[92m",
                ConsoleColor.Cyan => "\x1b[96m",
                ConsoleColor.Red => "\x1b[91m",
                ConsoleColor.Magenta => "\x1b[95m",
                ConsoleColor.Yellow => "\x1b[93m",
                _ => "\x1b[97m",
            };
        }

        public static T ParseJSONStringtoStruct<T>(string jsonstr)
        {
            if (string.IsNullOrEmpty(jsonstr))
                return default;
            Variant jsonarr = null;
            try { jsonarr = JSON.Load(jsonstr); }
            catch (Exception ex)
            {
                MelonLogger.Error($"Exception while Decoding JSON String to JSON Variant: {ex}");
                return default;
            }
            if (jsonarr == null)
                return default;
            T returnobj = default;
            try { returnobj = jsonarr.Make<T>(); }
            catch (Exception ex) { MelonLogger.Error($"Exception while Converting JSON Variant to {typeof(T).Name}: {ex}"); }
            return returnobj;
        }

        public static bool IsGame32Bit()
            => IntPtr.Size == 4; // there will never be a situation where this is wrong. the bitness of the C# code is reliant on the launch process

        public static bool IsGameIl2Cpp()
        {
            // bad news: this can be wrong in edge cases
            // good news: none of the games that ML has been used with that i know of fail with this
            // even better news: this is the implementation in ML's C++ code anyway

            return File.Exists(Path.Combine(BepInEx.Paths.GameRootPath, "GameAssembly.dll"));
        }

        public static bool IsOldMono()
            => File.Exists(Path.Combine(BepInEx.Paths.GameRootPath, "mono.dll"));

        // this is not used by anything?? C++ implementation always returns null
        public static string GetApplicationPath()
            => null; // BepInEx.Paths.GameRootPath;

        public static string GetGameDataDirectory()
            => Path.Combine(BepInEx.Paths.GameRootPath, $"{BepInEx.Paths.ProcessName}_Data");

        public static string GetManagedDirectory()
            => IsGameIl2Cpp()
            ? Path.Combine(BepInEx.Paths.GameRootPath, "mono", "Managed")
            : Path.Combine(BepInEx.Paths.GameRootPath, $"{BepInEx.Paths.ProcessName}_Data", "Managed");

        public static string GetEmulatedManagedDirectory()
            => Path.Combine(BepInEx.Paths.GameRootPath, "MelonLoader", "EmulatedManaged");


        private static string cachedUnityVersion { get; }
            = System.Text.RegularExpressions.Regex.Match(UnityEngine.Application.unityVersion,
                @"^(\d+\.\d+\.\d+(?:\.\d+)*)").Value;

        public static string GetUnityVersion()
            => cachedUnityVersion;

		public static void SetConsoleTitle(string title) { } // stubbed out

        public static string GetFileProductName(string filepath)
            => FileVersionInfo.GetVersionInfo(filepath).ProductName;

        private static Dictionary<IntPtr, NativeDetour> InstalledHooks { get; } = new Dictionary<IntPtr, NativeDetour>();

        public static void NativeHookAttach(IntPtr target, IntPtr detour)
		{
            var newDetour = new NativeDetour(target, detour);
            newDetour.Apply();
            InstalledHooks[target] = newDetour;
        }

        public static void NativeHookDetach(IntPtr target, IntPtr detour)
		{
            var nativeDetour = InstalledHooks[target];
            nativeDetour.Dispose();
		}

        private static string Internal_GetGameName()
            => UnityEngine.Application.productName ?? "Unknown";

        private static string Internal_GetGameDeveloper()
            => UnityEngine.Application.companyName ?? "Unknown";

        private static string Internal_GetGameDirectory()
            => BepInEx.Paths.GameRootPath;

        // seems to be a hash of Bootstrap.dll. not sure what algorithm
        private static string Internal_GetHashCode()
            => "DEADBEEF";
    }
}