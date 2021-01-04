using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using HarmonyLib.Tools;

namespace HarmonyXInterop
{
    internal static class HarmonyInterop
    {
        private static readonly Func<MethodBase, PatchInfo, MethodInfo> UpdateWrapper =
            AccessTools.MethodDelegate<Func<MethodBase, PatchInfo, MethodInfo>>(
                AccessTools.Method(typeof(HarmonyManipulator).Assembly.GetType("HarmonyLib.PatchFunctions"),
                    "UpdateWrapper"));

        private static readonly Action<Logger.LogChannel, Func<string>> HarmonyLog =
            AccessTools.MethodDelegate<Action<Logger.LogChannel, Func<string>>>(AccessTools.Method(typeof(Logger),
                "Log"));

        private static readonly Action<Logger.LogChannel, string> HarmonyLogText =
            AccessTools.MethodDelegate<Action<Logger.LogChannel, string>>(AccessTools.Method(typeof(Logger),
                "LogText"));
        
        public static void ApplyPatch(MethodBase target, PatchInfoWrapper add, PatchInfoWrapper remove)
        {
            PatchMethod[] WrapTranspilers(PatchMethod[] transpilers) => transpilers.Select(p => new PatchMethod
            {
                after = p.after,
                before = p.before,
                method = TranspilerInterop.WrapInterop(p.method),
                owner = p.owner,
                priority = p.priority
            }).ToArray();
            
            var pInfo = target.ToPatchInfo();
            lock (pInfo)
            {
                pInfo.prefixes = Sync(add.prefixes, remove.prefixes, pInfo.prefixes);
                pInfo.postfixes = Sync(add.postfixes, remove.postfixes, pInfo.postfixes);
                pInfo.transpilers = Sync(WrapTranspilers(add.transpilers), WrapTranspilers(remove.transpilers), pInfo.transpilers);
                pInfo.finalizers = Sync(add.finalizers, remove.finalizers, pInfo.finalizers);
            }

            UpdateWrapper(target, pInfo);
        }

        private static Patch[] Sync(PatchMethod[] add, PatchMethod[] remove, Patch[] current)
        {
            if (add.Length == 0 && remove.Length == 0)
                return current;
            current = current.Where(p => !remove.Any(r => r.method == p.PatchMethod && r.owner == p.owner)).ToArray();
            var initialIndex = current.Length;
            return current.Concat(add.Where(method => method != null).Select((method, i) =>
                new Patch(method.ToHarmonyMethod(), i + initialIndex, method.owner))).ToArray();
        }
    }
}