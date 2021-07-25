using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx;
using BepInEx.IL2CPP.Hook;
using MonoMod.Utils;
using MonoMod.Cil;
using HarmonyLib;
using MelonLoader.TinyJSON;
using MonoMod.RuntimeDetour;

#pragma warning disable 0618

namespace MelonLoader
{
    public static class MelonUtils
    {
        internal static void Setup()
        {
            GameDeveloper = Internal_GetGameDeveloper();
            GameName = Internal_GetGameName();
            HashCode = Internal_GetHashCode();
            CurrentGameAttribute = new MelonGameAttribute(GameDeveloper, GameName);
            BaseDirectory = Internal_GetBaseDirectory();
            GameDirectory = Internal_GetGameDirectory();
            UserDataDirectory = Path.Combine(BaseDirectory, "UserData");
            if (!Directory.Exists(UserDataDirectory))
                Directory.CreateDirectory(UserDataDirectory);
            Main.IsBoneworks = IsBONEWORKS;
        }

        public static string BaseDirectory { get; private set; }
        public static string GameDirectory { get; private set; }
        public static string UserDataDirectory { get; private set; }
        public static MelonGameAttribute CurrentGameAttribute { get; private set; }
        public static string GameDeveloper { get; private set; }
        public static string GameName { get; private set; }
        public static string GameVersion { get => GameVersionHandler.Version; }
        public static bool IsBONEWORKS { get => (!string.IsNullOrEmpty(GameDeveloper) && GameDeveloper.Equals("Stress Level Zero") && !string.IsNullOrEmpty(GameName) && GameName.Equals("BONEWORKS")); }
        public static bool IsDemeo { get => (!string.IsNullOrEmpty(GameDeveloper) && GameDeveloper.Equals("Resolution Games") && !string.IsNullOrEmpty(GameName) && GameName.Equals("Demeo")); }
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

        public static void SetCurrentDomainBaseDirectory(string dirpath, AppDomain domain = null)
        {
            if (domain == null)
                domain = AppDomain.CurrentDomain;
            try
            {
                ((AppDomainSetup)typeof(AppDomain).GetProperty("SetupInformationNoCopy", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(domain, new object[0]))
                    .ApplicationBase = dirpath;
            }
            catch (Exception ex) { MelonLogger.Warning($"AppDomainSetup.ApplicationBase Exception: {ex}"); }
            Directory.SetCurrentDirectory(dirpath);
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

        public static T PullAttributeFromAssembly<T>(Assembly asm, bool inherit = false) where T : Attribute
        {
            T[] attributetbl = PullAttributesFromAssembly<T>(asm, inherit);
            if ((attributetbl == null) || (attributetbl.Length <= 0))
                return null;
            return attributetbl[0];
        }

        public static T[] PullAttributesFromAssembly<T>(Assembly asm, bool inherit = false) where T : Attribute
        {
            Attribute[] att_tbl = Attribute.GetCustomAttributes(asm, inherit);

            if ((att_tbl == null) || (att_tbl.Length <= 0))
                return null;

            Type requestedType = typeof(T);
            List<T> output = new List<T>();
            foreach (Attribute att in att_tbl)
            {
                Type attType = att.GetType();
                string attAssemblyName = attType.Assembly.GetName().Name;
                string requestedAssemblyName = requestedType.Assembly.GetName().Name;

                if ((attType == requestedType)
                    || attType.FullName.Equals(requestedType.FullName)
                    || ((attAssemblyName.Equals("MelonLoader")
                        || attAssemblyName.Equals("MelonLoader.ModHandler"))
                        && (requestedAssemblyName.Equals("MelonLoader")
                        || requestedAssemblyName.Equals("MelonLoader.ModHandler"))
                        && attType.Name.Equals(requestedType.Name)))
                    output.Add(att as T);
            }

            return output.ToArray();
        }

        public static IEnumerable<Type> GetValidTypes(this Assembly asm)
            => GetValidTypes(asm, null);
        public static IEnumerable<Type> GetValidTypes(this Assembly asm, Func<Type, bool> predicate)
        {
            IEnumerable<Type> returnval = Enumerable.Empty<Type>();
            try { returnval = asm.GetTypes().AsEnumerable(); }
            catch (ReflectionTypeLoadException ex) { returnval = ex.Types; }
            return returnval.Where(x =>
                ((x != null)
                    && ((predicate != null)
                        ? predicate(x)
                        : true)));
        }

        public static bool IsNotImplemented(this MethodBase methodBase)
        {
            if (methodBase == null)
                throw new ArgumentNullException(nameof(methodBase));

            DynamicMethodDefinition method = methodBase.ToNewDynamicMethodDefinition();
            ILContext ilcontext = new ILContext(method.Definition);
            ILCursor ilcursor = new ILCursor(ilcontext);

            bool returnval = (ilcursor.Instrs.Count == 2)
                && (ilcursor.Instrs[1].OpCode.Code == Mono.Cecil.Cil.Code.Throw);

            ilcontext.Dispose();
            method.Dispose();
            return returnval;
        }

        public static HarmonyMethod ToNewHarmonyMethod(this MethodInfo methodInfo)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));
            return new HarmonyMethod(methodInfo);
        }

        public static DynamicMethodDefinition ToNewDynamicMethodDefinition(this MethodBase methodBase)
        {
            if (methodBase == null)
                throw new ArgumentNullException(nameof(methodBase));
            return new DynamicMethodDefinition(methodBase);
        }

        public static void GetDelegate<T>(this IntPtr ptr, out T output) where T : Delegate
            => output = GetDelegate<T>(ptr);
        public static T GetDelegate<T>(this IntPtr ptr) where T : Delegate
            => GetDelegate(ptr, typeof(T)) as T;
        public static Delegate GetDelegate(this IntPtr ptr, Type type)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(ptr));
            Delegate del = Marshal.GetDelegateForFunctionPointer(ptr, type);
            if (del == null)
                throw new Exception($"Unable to Get Delegate of Type {type.FullName} for Function Pointer!");
            return del;
        }
        public static IntPtr GetFunctionPointer(this Delegate del)
            => Marshal.GetFunctionPointerForDelegate(del);

        public static NativeLibrary ToNewNativeLibrary(this IntPtr ptr)
            => new NativeLibrary(ptr);
        public static NativeLibrary<T> ToNewNativeLibrary<T>(this IntPtr ptr)
            => new NativeLibrary<T>(ptr);
        public static IntPtr GetNativeLibraryExport(this IntPtr ptr, string name)
            => NativeLibrary.GetExport(ptr, name);

        public static bool IsGame32Bit()
            => IntPtr.Size == 4; // there will never be a situation where this is wrong. the bitness of the C# code is reliant on the launch process

        public static bool IsGameIl2Cpp()
        {
            // bad news: this can be wrong in edge cases
            // good news: none of the games that ML has been used with that i know of fail with this
            // even better news: this is the implementation in ML's C++ code anyway

            return File.Exists(Path.Combine(Paths.GameRootPath, "GameAssembly.dll"));
        }

        public static bool IsOldMono()
            => File.Exists(Path.Combine(Paths.GameRootPath, "mono.dll"));

        // this is not used by anything?? C++ implementation always returns null
        public static string GetApplicationPath()
            => null; // BepInEx.Paths.GameRootPath;

        public static string GetGameDataDirectory()
            => Path.Combine(Paths.GameRootPath, $"{Paths.ProcessName}_Data");

        private static string cachedUnityVersion { get; }
            = System.Text.RegularExpressions.Regex.Match(UnityEngine.Application.unityVersion,
                @"^(\d+\.\d+\.\d+(?:\.\d+)*)").Value;

        public static string GetUnityVersion()
            => cachedUnityVersion;

        public static string GetManagedDirectory()
            => IsGameIl2Cpp()
                ? Utility.CombinePaths(Paths.GameRootPath, "mono", "Managed")
                : Utility.CombinePaths(Paths.GameRootPath, $"{Paths.ProcessName}_Data", "Managed");

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

        public static string Internal_GetBaseDirectory()
            => Utility.CombinePaths(Paths.GameRootPath, "MelonLoader");

        private const string UnknownItem = "UNKNOWN";
        
        private static string Internal_GetGameName()
            => AppInfo.ProductName ?? UnknownItem;
        
        private static string Internal_GetGameDeveloper()
            => AppInfo.CompanyName ?? UnknownItem;

        private static string Internal_GetGameDirectory()
            => Paths.GameRootPath;

        // seems to be a hash of Bootstrap.dll. not sure what algorithm
        private static string Internal_GetHashCode()
            => "DEADBEEF";
    }

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
}