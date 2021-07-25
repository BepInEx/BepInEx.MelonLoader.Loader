using System;
using System.Runtime.CompilerServices;
using BepInEx.Logging;

namespace MelonLoader
{
    public class MelonLogger
    {
        public static readonly ConsoleColor DefaultMelonColor = ConsoleColor.Cyan;
        public static readonly ConsoleColor DefaultTextColor = ConsoleColor.Gray;

        public static ManualLogSource BepInExLog = Logger.CreateLogSource("MelonLoader");

        public static void Msg(string txt) => SendMsg(DefaultTextColor, txt);
        public static void Msg(string txt, params object[] args) => SendMsg(DefaultTextColor, string.Format(txt, args));
        public static void Msg(object obj) => SendMsg(DefaultTextColor, obj.ToString());
        public static void Msg(ConsoleColor txtcolor, string txt) => SendMsg(txtcolor, txt);
        public static void Msg(ConsoleColor txtcolor, string txt, params object[] args) => SendMsg(txtcolor, string.Format(txt, args));
        public static void Msg(ConsoleColor txtcolor, object obj) => SendMsg(txtcolor, obj.ToString());

        public static void Warning(string txt) => SendWarning(txt);
        public static void Warning(string txt, params object[] args) => SendWarning(string.Format(txt, args));
        public static void Warning(object obj) => SendWarning(obj.ToString());

        public static void Error(string txt) => SendError(txt);
        public static void Error(string txt, params object[] args) => SendError(string.Format(txt, args));
        public static void Error(object obj) => SendError(obj.ToString());

        internal static void PushMessageToBepInEx(string namesection, string message, LogLevel level)
        {
            message ??= "<null>";

            if (!string.IsNullOrEmpty(namesection))
            {
                BepInExLog.Log(level, $"[{namesection}] {message}");
            }
            else
            {
                BepInExLog.Log(level, message);
            }
        }

        private static void SendMsg(ConsoleColor txtcolor, string txt)
        {
            ConsoleColor meloncolor = DefaultMelonColor;
            string namesection = null;
            MelonBase melon = MelonUtils.GetMelonFromStackTrace();
            if (melon != null)
            {
                namesection = melon.Info.Name.Replace(" ", "_");
                meloncolor = melon.ConsoleColor;
            }

            PushMessageToBepInEx(namesection, txt, LogLevel.Message);
            RunMsgCallbacks(meloncolor, txtcolor, namesection, txt ?? "null");
        }

        private static void SendWarning(string txt)
        {
            string namesection = null;
            MelonBase melon = MelonUtils.GetMelonFromStackTrace();
            if (melon != null)
                namesection = melon.Info.Name;
            ManualWarning(namesection, txt);
        }

        private static void SendError(string txt) => ManualMelonError(MelonUtils.GetMelonFromStackTrace(), txt ?? "null");

        internal static void ManualWarning(string namesection, string txt)
        {
            namesection = namesection?.Replace(" ", "_");

            PushMessageToBepInEx(namesection, txt, LogLevel.Warning);
            RunWarningCallbacks(namesection, txt ?? "null");
        }

        internal static void ManualMelonError(MelonBase melon, string txt)
        {
            string namesection = null;
            if (melon != null)
                namesection = melon.Info.Name.Replace(" ", "_");

            PushMessageToBepInEx(namesection, txt, LogLevel.Error);
            RunErrorCallbacks(namesection, txt ?? "null");
        }

        internal static void RunMsgCallbacks(ConsoleColor meloncolor, ConsoleColor txtcolor, string namesection, string msg) => MsgCallbackHandler?.Invoke(meloncolor, txtcolor, namesection, msg);
        public static event Action<ConsoleColor, ConsoleColor, string, string> MsgCallbackHandler;
        internal static void RunWarningCallbacks(string namesection, string msg) => WarningCallbackHandler?.Invoke(namesection, msg);
        public static event Action<string, string> WarningCallbackHandler;
        internal static void RunErrorCallbacks(string namesection, string msg) => ErrorCallbackHandler?.Invoke(namesection, msg);
        public static event Action<string, string> ErrorCallbackHandler;

        internal static void ThrowInternalFailure(string v)
        {
            BepInExLog.LogError($"[{DateTime.Now}] [INTERNAL FAILURE] {v}");
            // process is usually killed here but i'm not about to do that
            throw new Exception("Melonloader internal failure");
        }

        internal static void WriteSpacer()
        {
            BepInExLog.LogMessage(string.Empty);
        }

        internal static void Internal_PrintModName(ConsoleColor consoleColor, string name, string version)
        {
            BepInExLog.LogMessage($"{name} v{version}");
        }

        [Obsolete("Log is obsolete. Please use Msg instead.")]
        public static void Log(string txt) => Msg(txt);
        [Obsolete("Log is obsolete. Please use Msg instead.")]
        public static void Log(string txt, params object[] args) => Msg(txt, args);
        [Obsolete("Log is obsolete. Please use Msg instead.")]
        public static void Log(object obj) => Msg(obj);
        [Obsolete("Log is obsolete. Please use Msg instead.")]
        public static void Log(ConsoleColor color, string txt) => Msg(color, txt);
        [Obsolete("Log is obsolete. Please use Msg instead.")]
        public static void Log(ConsoleColor color, string txt, params object[] args) => Msg(color, txt, args);
        [Obsolete("Log is obsolete. Please use Msg instead.")]
        public static void Log(ConsoleColor color, object obj) => Msg(color, obj);
        [Obsolete("LogWarning is obsolete. Please use Warning instead.")]
        public static void LogWarning(string txt) => Warning(txt);
        [Obsolete("LogWarning is obsolete. Please use Warning instead.")]
        public static void LogWarning(string txt, params object[] args) => Warning(txt, args);
        [Obsolete("LogError is obsolete. Please use Error instead.")]
        public static void LogError(string txt) => Error(txt);
        [Obsolete("LogError is obsolete. Please use Error instead.")]
        public static void LogError(string txt, params object[] args) => Error(txt, args);
    }
}
