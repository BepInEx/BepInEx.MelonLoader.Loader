using System;
using System.Runtime.CompilerServices;
using BepInEx.Logging;

namespace MelonLoader
{
    public static class MelonDebug
    {
        public static void Msg(string txt)
        {
            if (!IsEnabled())
                return;
            SendMsg(MelonLogger.DefaultTextColor, txt);
        }
        public static void Msg(string txt, params object[] args)
        {
            if (!IsEnabled())
                return;
            SendMsg(MelonLogger.DefaultTextColor, string.Format(txt, args));
        }
        public static void Msg(object obj)
        {
            if (!IsEnabled())
                return;
            SendMsg(MelonLogger.DefaultTextColor, obj.ToString());
        }

        public static void Error(string txt)
        {
            if (!IsEnabled())
                return;
            SendError(txt);
        }

        private static void SendError(string txt) => MelonLogger.ManualMelonError(MelonUtils.GetMelonFromStackTrace(), txt ?? "null");

        private static void SendMsg(ConsoleColor msgcolor, string msg)
        {
            ConsoleColor meloncolor = MelonLogger.DefaultMelonColor;
            string namesection = null;
            MelonBase melon = MelonUtils.GetMelonFromStackTrace();
            if (melon != null)
            {
                namesection = melon.Info.Name.Replace(" ", "_");
                msgcolor = melon.ConsoleColor;
            }
            MelonLogger.PushMessageToBepInEx(namesection, msg, LogLevel.Debug);
            MsgCallbackHandler?.Invoke(meloncolor, msgcolor, namesection, msg);
        }

        public static event Action<ConsoleColor, ConsoleColor, string, string> MsgCallbackHandler;
        public static bool IsEnabled() => MelonLaunchOptions.Core.DebugMode;
    }
}