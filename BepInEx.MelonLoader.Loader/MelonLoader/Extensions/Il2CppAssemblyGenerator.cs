using System;
using System.IO;
using System.Reflection;

namespace MelonLoader
{
    internal static class Il2CppAssemblyGenerator_Internal
    {
        internal static bool Run()
        {
            if (!MelonUtils.IsGameIl2Cpp())
                return true;

            return Il2CppAssemblyGenerator.Core.Run() == 0;
        }
    }
}