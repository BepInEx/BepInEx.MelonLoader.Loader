﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MelonLoader
{
    public static class MelonCompatibilityLayer
    {
        internal static void Setup(AppDomain domain)
        {
            string versionending = ", Version=";
            domain.AssemblyResolve += (sender, args) =>
                (args.Name.StartsWith($"Mono.Cecil{versionending}")
                || args.Name.StartsWith($"Mono.Cecil.Mdb{versionending}")
                || args.Name.StartsWith($"Mono.Cecil.Pdb{versionending}")
                || args.Name.StartsWith($"Mono.Cecil.Rocks{versionending}")
                || args.Name.StartsWith($"MonoMod.RuntimeDetour{versionending}")
                || args.Name.StartsWith($"MonoMod.Utils{versionending}")
                || args.Name.StartsWith($"0Harmony{versionending}"))
                ? typeof(MelonCompatibilityLayer).Assembly
                : null;
            CompatibilityLayers.Melon_CL.Setup(domain);
            SetupSupportModule(domain);
            domain.AssemblyResolve += MelonHandler.AssemblyResolver;
        }

        private static void SetupSupportModule(AppDomain domain)
        {
            try
            {
                string BaseDirectory = Path.Combine(Path.Combine(Path.Combine(MelonUtils.GameDirectory, "MelonLoader"), "Dependencies"), "SupportModules");
                if (!Directory.Exists(BaseDirectory))
                {
                    MelonLogger.Error("Failed to Find SupportModules Directory!");
                    return;
                }
                Assembly CompatibilityLayerAssembly = Assembly.LoadFrom(Path.Combine(BaseDirectory, "CompatibilityLayer.dll"));
                if (CompatibilityLayerAssembly == null)
                {
                    MelonLogger.Error("Failed to Load Assembly for CompatibilityLayer!");
                    return;
                }
                Type CompatibilityLayerType = CompatibilityLayerAssembly.GetType("MelonLoader.Support.CompatibilityLayer");
                if (CompatibilityLayerType == null)
                {
                    MelonLogger.Error("Failed to Get Type for CompatibilityLayer!");
                    return;
                }
                MethodInfo CompatibilityLayerSetupMethod = CompatibilityLayerType.GetMethod("Setup", BindingFlags.NonPublic | BindingFlags.Static);
                if (CompatibilityLayerSetupMethod == null)
                {
                    MelonLogger.Error("Failed to Get Setup Method for CompatibilityLayer!");
                    return;
                }
                CompatibilityLayerSetupMethod.Invoke(null, new object[] { domain });
            }
            catch (Exception ex) { MelonLogger.Error(ex.ToString()); }
        }

        public class WrapperData
        {
            public Assembly Assembly = null;
            public MelonInfoAttribute Info = null;
            public MelonGameAttribute[] Games = null;
            public MelonOptionalDependenciesAttribute OptionalDependencies = null;
            public ConsoleColor ConsoleColor = MelonLogger.DefaultMelonColor;
            public int Priority = 0;
            public string Location = null;
        }
        public static MelonBase CreateMelonFromWrapperData(WrapperData creationData)
        {
            MelonBase instance = Activator.CreateInstance(creationData.Info.SystemType) as MelonBase;
            if (instance == null)
            {
                MelonLogger.Error($"Failed to Create Instance for {creationData.Location}");
                return null;
            }
            instance.Assembly = creationData.Assembly;
            instance.Info = creationData.Info;
            instance.Games = creationData.Games;
            instance.OptionalDependencies = creationData.OptionalDependencies;
            instance.Location = creationData.Location;
            instance.Priority = creationData.Priority;
            instance.ConsoleColor = creationData.ConsoleColor;
#pragma warning disable CS0618
            instance.HarmonyInstance = Harmony.HarmonyInstance.Create(instance.Assembly.FullName);
#pragma warning restore CS0618
            return instance;
        }

        // Assembly to Compatibility Layer Conversion
        private static event Action<LayerResolveEventArgs> ResolveAssemblyToLayerResolverEvents;
        public static void AddResolveAssemblyToLayerResolverEvent(Action<LayerResolveEventArgs> evt) => ResolveAssemblyToLayerResolverEvents += evt;
        internal static Resolver ResolveAssemblyToLayerResolver(Assembly asm)
        {
            LayerResolveEventArgs args = new LayerResolveEventArgs();
            args.assembly = asm;
            ResolveAssemblyToLayerResolverEvents?.Invoke(args);
            return args.inter;
        }

        // Refresh Event - Plugins
        private static event Action RefreshPluginsTableEvents;
        public static void AddRefreshPluginsTableEvent(Action evt) => RefreshPluginsTableEvents += evt;
        public static void RefreshPluginsTable() => RefreshPluginsTableEvents?.Invoke();

        // Refresh Event - Mods
        private static event Action RefreshModsTableEvents;
        public static void AddRefreshModsTableEvent(Action evt) => RefreshModsTableEvents += evt;
        public static void RefreshModsTable() => RefreshModsTableEvents?.Invoke();

        // Resolver Base and Resolver Event Args
        public class Resolver { public virtual void CheckAndCreate(string filelocation, bool is_plugin, ref List<MelonBase> melonTbl) { } }
        public class LayerResolveEventArgs : EventArgs
        {
            public Assembly assembly;
            public Resolver inter;
        }
    }
}