using System;
using BepInEx;

namespace MelonLoader
{
    public class MelonConsole
    {
	    public static bool Enabled => ConsoleManager.ConsoleActive;

        internal static void RunLogCallbacks(string namesection, string msg) => LogCallbackHandler?.Invoke(namesection, msg);
        public static event Action<string, string> LogCallbackHandler;
        internal static void RunWarningCallbacks(string namesection, string msg) => WarningCallbackHandler?.Invoke(namesection, msg);
        public static event Action<string, string> WarningCallbackHandler;
        internal static void RunErrorCallbacks(string namesection, string msg) => ErrorCallbackHandler?.Invoke(namesection, msg);
        public static event Action<string, string> ErrorCallbackHandler;
    }
}