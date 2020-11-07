using System;
using System.Diagnostics;
using BepInEx.Logging;

namespace MelonLoader
{
    public class MelonLogger
    {
	    public static ManualLogSource BepInExLogger { get; } = Logger.CreateLogSource("MelonLoader");

	    public static void Log(string s)
        {
            string namesection = GetNameSection();
            BepInExLogger.LogMessage(namesection + s);
            MelonConsole.RunLogCallbacks(namesection, s);
        }

        public static void Log(ConsoleColor color, string s)
        {
            string namesection = GetNameSection();
            BepInExLogger.LogMessage(namesection + s);
            MelonConsole.RunLogCallbacks(namesection, s);
        }

        public static void Log(string s, params object[] args)
        {
            string namesection = GetNameSection();
            string fmt = string.Format(s, args);
            BepInExLogger.LogMessage(namesection + fmt);
            MelonConsole.RunLogCallbacks(namesection, fmt);
        }

        public static void Log(ConsoleColor color, string s, params object[] args)
        {
            string namesection = GetNameSection();
            string fmt = string.Format(s, args);
            BepInExLogger.LogMessage(namesection + fmt);
            MelonConsole.RunLogCallbacks(namesection, fmt);
        }

        public static void Log(object o)
        {
            Log(o.ToString());
        }

        public static void Log(ConsoleColor color, object o)
        {
            Log(color, o.ToString());
        }

        public static void LogWarning(string s)
        {
            string namesection = GetNameSection();
            BepInExLogger.LogWarning(namesection + s);
            MelonConsole.RunWarningCallbacks(namesection, s);
        }

        public static void LogWarning(string s, params object[] args)
        {
            string namesection = GetNameSection();
            string fmt = string.Format(s, args);
            BepInExLogger.LogWarning(namesection + fmt);
            MelonConsole.RunWarningCallbacks(namesection, fmt);
        }

        public static void LogError(string s)
        {
            string namesection = GetNameSection();
            BepInExLogger.LogError(namesection + s);
            MelonConsole.RunErrorCallbacks(namesection, s);
        }
        public static void LogError(string s, params object[] args)
        {
            string namesection = GetNameSection();
            string fmt = string.Format(s, args);
            BepInExLogger.LogError(namesection + fmt);
            MelonConsole.RunErrorCallbacks(namesection, fmt);
        }

        internal static void LogMelonError(string msg, string modname)
        {
            string namesection = string.IsNullOrEmpty(modname) ? "" : $"[{modname}] ";
            BepInExLogger.LogError(namesection + msg);
            MelonConsole.RunErrorCallbacks(namesection, msg);
        }

        internal static string GetNameSection()
        {
            var stackTrace = new StackTrace(2, true);
            
            var assembly = stackTrace.GetFrame(0)?.GetMethod()?.DeclaringType?.Assembly;

            if (assembly == null)
	            return string.Empty;

            var plugin = MelonHandler.Plugins.Find(x => Equals(x.Assembly, assembly));
            if (plugin != null)
            {
	            if (!string.IsNullOrEmpty(plugin.Info.Name))
		            return $"[{plugin.Info.Name}] ";
            }
            else
            {
	            var mod = MelonHandler.Mods.Find(x => Equals(x.Assembly, assembly));
	            if (!string.IsNullOrEmpty(mod?.Info.Name))
		            return $"[{mod.Info.Name}] ";
            }
            return string.Empty;
        }
    }
}