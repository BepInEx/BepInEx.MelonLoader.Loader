using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using Harmony;
using MelonLoader.ICSharpCode.SharpZipLib.Zip;

namespace MelonLoader
{
    public static class MelonHandler
    {
        internal static bool HasLoadedPlugins = false;
        internal static Assembly Assembly_CSharp = null;
        private static List<MelonPlugin> _TempPlugins = null;
        public static List<MelonPlugin> Plugins { get; internal set; } = new List<MelonPlugin>();
        public static List<MelonMod> Mods { get; internal set; } = new List<MelonMod>();

        internal static void LoadAll(bool plugins = false)
        {
            string searchdir = Path.Combine(Paths.GameRootPath, "MelonLoader", (plugins ? "Plugins" : "Mods"));
            if (!Directory.Exists(searchdir))
            {
                Directory.CreateDirectory(searchdir);
                return;
            }
            LoadMode loadmode = LoadMode.BOTH;

            // DLL
            string[] files = Directory.GetFiles(searchdir, "*.dll");
            if (files.Length > 0)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];
                    if (string.IsNullOrEmpty(file))
                        continue;

                    bool file_extension_check = Path.GetFileNameWithoutExtension(file).EndsWith("-dev");
                    if ((loadmode != LoadMode.BOTH) && ((loadmode == LoadMode.DEV) ? !file_extension_check : file_extension_check))
                        continue;

                    try
                    {
                        LoadFromFile(file, plugins);
                    }
                    catch (Exception e)
                    {
                        MelonLogger.LogError("Unable to load " + file + ":\n" + e.ToString());
                        MelonLogger.Log("------------------------------");
                    }
                }
            }

            // ZIP
            string[] zippedFiles = Directory.GetFiles(searchdir, "*.zip");
            if (zippedFiles.Length > 0)
            {
                for (int i = 0; i < zippedFiles.Length; i++)
                {
                    string file = zippedFiles[i];
                    if (string.IsNullOrEmpty(file))
                        continue;
                    try
                    {
                        using (var fileStream = File.OpenRead(file))
                        {
                            using (var zipInputStream = new ZipInputStream(fileStream))
                            {
                                ZipEntry entry;
                                while ((entry = zipInputStream.GetNextEntry()) != null)
                                {
                                    string filename = Path.GetFileName(entry.Name);
                                    if (string.IsNullOrEmpty(filename) || !filename.EndsWith(".dll"))
                                        continue;

                                    bool file_extension_check = Path.GetFileNameWithoutExtension(file).EndsWith("-dev");
                                    if ((loadmode != LoadMode.BOTH) && ((loadmode == LoadMode.DEV) ? !file_extension_check : file_extension_check))
                                        continue;

                                    using (var unzippedFileStream = new MemoryStream())
                                    {
                                        int size = 0;
                                        byte[] buffer = new byte[4096];
                                        while (true)
                                        {
                                            size = zipInputStream.Read(buffer, 0, buffer.Length);
                                            if (size > 0)
                                                unzippedFileStream.Write(buffer, 0, size);
                                            else
                                                break;
                                        }
                                        LoadFromAssembly(Assembly.Load(unzippedFileStream.ToArray()), plugins, (file + "/" + filename));
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MelonLogger.LogError("Unable to load " + file + ":\n" + e.ToString());
                        MelonLogger.Log("------------------------------");
                    }
                }
            }
            Main.LegacySupport(Mods, Plugins, MelonLoaderBase.IsVRChat, MelonLoaderBase.IsBoneworks);
        }

        public static void LoadFromFile(string filelocation, bool isPlugin = false) => LoadFromAssembly(
	        (Imports.IsDebugMode() ? Assembly.LoadFrom(filelocation) : Assembly.Load(File.ReadAllBytes(filelocation))),
	        isPlugin, filelocation);

        public static void LoadFromAssembly(Assembly asm, bool isPlugin = false, string filelocation = null)
        {
            if (!asm.Equals(null))
            {
                MelonLegacyAttributeSupport.Response_Info response_Info = MelonLegacyAttributeSupport.GetMelonInfoAttribute(asm, isPlugin);
                MelonInfoAttribute InfoAttribute = response_Info.Default;
                if ((InfoAttribute != null) && (InfoAttribute.SystemType != null) && InfoAttribute.SystemType.IsSubclassOf((isPlugin ? typeof(MelonPlugin) : typeof(MelonMod))))
                {
                    bool isCompatible = false;
                    bool isUniversal = false;
                    bool hasAttribute = true;
                    MelonLegacyAttributeSupport.Response_Game response_Game = MelonLegacyAttributeSupport.GetMelonGameAttributes(asm, isPlugin);
                    MelonGameAttribute[] GameAttributes = response_Game.Default;
                    int GameAttributes_Count = GameAttributes.Length;
                    if (GameAttributes_Count > 0)
                    {
                        for (int i = 0; i < GameAttributes_Count; i++)
                        {
                            MelonGameAttribute GameAttribute = GameAttributes[i];
                            if (MelonLoaderBase.CurrentGameAttribute.IsCompatible(GameAttribute))
                            {
                                isCompatible = true;
                                isUniversal = MelonLoaderBase.CurrentGameAttribute.IsCompatibleBecauseUniversal(GameAttribute);
                                break;
                            }
                        }
                    }
                    else
                        hasAttribute = false;
                    MelonBase baseInstance = Activator.CreateInstance(InfoAttribute.SystemType) as MelonBase;
                    if (baseInstance != null)
                    {
                        response_Info.SetupMelon(baseInstance);
                        response_Game.SetupMelon(baseInstance);
                        baseInstance.OptionalDependenciesAttribute = asm.GetCustomAttributes(false).FirstOrDefault(x => (x.GetType() == typeof(MelonOptionalDependenciesAttribute))) as MelonOptionalDependenciesAttribute;
                        baseInstance.Location = filelocation;
                        baseInstance.Compatibility = (isUniversal ? MelonBase.MelonCompatibility.UNIVERSAL :
                            (isCompatible ? MelonBase.MelonCompatibility.COMPATIBLE :
                                (!hasAttribute ? MelonBase.MelonCompatibility.NOATTRIBUTE : MelonBase.MelonCompatibility.INCOMPATIBLE)
                            )
                        );
                        if (baseInstance.Compatibility < MelonBase.MelonCompatibility.INCOMPATIBLE)
                        {
                            baseInstance.Assembly = asm;
                            baseInstance.harmonyInstance = HarmonyInstance.Create(asm.FullName);
                        }
                        if (isPlugin)
                            Plugins.Add((MelonPlugin)baseInstance);
                        else
                            Mods.Add((MelonMod)baseInstance);
                    }
                    else
                        MelonLogger.LogError("Unable to load " + asm.GetName() + "! Failed to Create Instance!");
                }
            }
        }

        internal static void LogAndPrune()
        {
            if (Plugins.Count > 0)
            {
                for (int i = 0; i < Plugins.Count; i++)
                    if (Plugins[i] != null)
                        LogMelonInfo(Plugins[i]);
                Plugins = _TempPlugins;
            }
            if (Plugins.Count <= 0)
            {
                MelonLogger.Log("No Plugins Loaded!");
                MelonLogger.Log("------------------------------");
            }
            else
                HasLoadedPlugins = true;

            if (Mods.Count > 0)
            {
                for (int i = 0; i < Mods.Count; i++)
                    if (Mods[i] != null)
                        LogMelonInfo(Mods[i]);
                Mods.RemoveAll((MelonMod mod) => ((mod == null) || (mod.Compatibility >= MelonBase.MelonCompatibility.INCOMPATIBLE)));
                DependencyGraph<MelonMod>.TopologicalSort(Mods, mod => mod.Info.Name);
            }
            if (Mods.Count <= 0)
            {
                MelonLogger.Log("No Mods Loaded!");
                MelonLogger.Log("------------------------------");
            }
            else
                HasLoadedPlugins = true;
        }

        private static void LogMelonInfo(MelonBase melon)
        {
            MelonLogger.Log(melon.Info.Name
                            + (!string.IsNullOrEmpty(melon.Info.Version)
                            ? $" v{melon.Info.Version}" : "")
                            + (!string.IsNullOrEmpty(melon.Info.Author)
                            ? $" by {melon.Info.Author}" : "")
                            + (!string.IsNullOrEmpty(melon.Info.DownloadLink)
                            ? $" ({melon.Info.DownloadLink})" : "")
                            );
            //MelonLogger.LogMelonCompatibility(melon.Compatibility);
            MelonLogger.Log("------------------------------");
        }

        internal static void OnPreInitialization()
        {
            if (Plugins.Count > 0)
            {
                HashSet<MelonPlugin> failedPlugins = new HashSet<MelonPlugin>();
                _TempPlugins = Plugins.Where(plugin => (plugin.Compatibility < MelonBase.MelonCompatibility.INCOMPATIBLE)).ToList();
                DependencyGraph<MelonPlugin>.TopologicalSort(_TempPlugins, plugin => plugin.Info.Name);
                for (int i = 0; i < _TempPlugins.Count; i++)
                    if (_TempPlugins[i] != null)
                        try { _TempPlugins[i].OnPreInitialization(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), _TempPlugins[i].Info.Name); failedPlugins.Add(_TempPlugins[i]); }
                _TempPlugins.RemoveAll(plugin => ((plugin == null) || failedPlugins.Contains(plugin)));
                Main.LegacySupport(Mods, _TempPlugins, MelonLoaderBase.IsVRChat, MelonLoaderBase.IsBoneworks);
            }
        }

        internal static void OnApplicationStart()
        {
            if (Plugins.Count > 0)
            {
                HashSet<MelonPlugin> failedPlugins = new HashSet<MelonPlugin>();
                for (int i = 0; i < Plugins.Count; i++)
                    if (Plugins[i] != null)
                        try { Plugins[i].harmonyInstance.PatchAll(Plugins[i].Assembly); Plugins[i].OnApplicationStart(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Plugins[i].Info.Name); HarmonyInstance.UnpatchAllMelonInstances(Plugins[i]); failedPlugins.Add(Plugins[i]); }
                Plugins.RemoveAll(plugin => ((plugin == null) || failedPlugins.Contains(plugin)));
                Main.LegacySupport(Mods, Plugins, MelonLoaderBase.IsVRChat, MelonLoaderBase.IsBoneworks);
            }
            if (Mods.Count > 0)
            {
                HashSet<MelonMod> failedMods = new HashSet<MelonMod>();
                for (int i = 0; i < Mods.Count; i++)
                    if (Mods[i] != null)
                        try { Mods[i].harmonyInstance.PatchAll(Mods[i].Assembly); Mods[i].OnApplicationStart(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Mods[i].Info.Name); HarmonyInstance.UnpatchAllMelonInstances(Mods[i]); failedMods.Add(Mods[i]); }
                Mods.RemoveAll(mod => ((mod == null) || failedMods.Contains(mod)));
                Main.LegacySupport(Mods, Plugins, MelonLoaderBase.IsVRChat, MelonLoaderBase.IsBoneworks);
            }
        }

        public static void OnApplicationQuit()
        {
            if (Plugins.Count > 0)
                for (int i = 0; i < Plugins.Count; i++)
                    if (Plugins[i] != null)
                        try { Plugins[i].OnApplicationQuit(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Plugins[i].Info.Name); }
            if (Mods.Count > 0)
                for (int i = 0; i < Mods.Count; i++)
                    if (Mods[i] != null)
                        try { Mods[i].OnApplicationQuit(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Mods[i].Info.Name); }
            MelonLoaderBase.Quit();
        }

        public static void OnModSettingsApplied()
        {
            if (Plugins.Count > 0)
                for (int i = 0; i < Plugins.Count; i++)
                    try { Plugins[i].OnModSettingsApplied(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Plugins[i].Info.Name); }
            if (Mods.Count > 0)
                for (int i = 0; i < Mods.Count; i++)
                    if (Mods[i] != null)
                        try { Mods[i].OnModSettingsApplied(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Mods[i].Info.Name); }
        }

        public static void OnUpdate()
        {
            SceneHandler.CheckForSceneChange();
            if (Imports.IsIl2CppGame() && MelonLoaderBase.IsVRChat)
                VRChat_CheckUiManager();
            if (Plugins.Count > 0)
                for (int i = 0; i < Plugins.Count; i++)
                    if (Plugins[i] != null)
                        try { Plugins[i].OnUpdate(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Plugins[i].Info.Name); }
            if (Mods.Count > 0)
                for (int i = 0; i < Mods.Count; i++)
                    if (Mods[i] != null)
                        try { Mods[i].OnUpdate(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Mods[i].Info.Name); }
        }

        public static void OnFixedUpdate()
        {
            if (Mods.Count > 0)
                for (int i = 0; i < Mods.Count; i++)
                    if (Mods[i] != null)
                        try { Mods[i].OnFixedUpdate(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Mods[i].Info.Name); }
        }

        public static void OnLateUpdate()
        {
            if (Plugins.Count > 0)
                for (int i = 0; i < Plugins.Count; i++)
                    if (Plugins[i] != null)
                        try { Plugins[i].OnLateUpdate(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Plugins[i].Info.Name); }
            if (Mods.Count > 0)
                for (int i = 0; i < Mods.Count; i++)
                    if (Mods[i] != null)
                        try { Mods[i].OnLateUpdate(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Mods[i].Info.Name); }
        }

        public static void OnGUI()
        {
            if (Plugins.Count > 0)
                for (int i = 0; i < Plugins.Count; i++)
                    if (Plugins[i] != null)
                        try { Plugins[i].OnGUI(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Plugins[i].Info.Name); }
            if (Mods.Count > 0)
                for (int i = 0; i < Mods.Count; i++)
                    if (Mods[i] != null)
                        try { Mods[i].OnGUI(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Mods[i].Info.Name); }
        }

        internal static void OnLevelIsLoading()
        {
            if (Mods.Count > 0)
                for (int i = 0; i < Mods.Count; i++)
                    if (Mods[i] != null)
                        try { Mods[i].OnLevelIsLoading(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Mods[i].Info.Name); }
        }

        internal static void OnLevelWasLoaded(int level)
        {
            if (Mods.Count > 0)
                for (int i = 0; i < Mods.Count; i++)
                    if (Mods[i] != null)
                        try { Mods[i].OnLevelWasLoaded(level); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Mods[i].Info.Name); }
        }

        internal static void OnLevelWasInitialized(int level)
        {
            if (Mods.Count > 0)
                for (int i = 0; i < Mods.Count; i++)
                    if (Mods[i] != null)
                        try { Mods[i].OnLevelWasInitialized(level); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Mods[i].Info.Name); }
        }

        private static bool ShouldCheckForUiManager = true;
        private static Type VRCUiManager = null;
        private static MethodInfo VRCUiManager_Instance = null;
        private static void VRChat_CheckUiManager()
        {
            if (!ShouldCheckForUiManager)
                return;
            if (VRCUiManager == null)
                VRCUiManager = Assembly_CSharp.GetType("VRCUiManager");
            if (VRCUiManager == null)
            {
                ShouldCheckForUiManager = false;
                return;
            }
            if (VRCUiManager_Instance == null)
                VRCUiManager_Instance = VRCUiManager.GetMethods().First(x => (x.ReturnType == VRCUiManager));
            if (VRCUiManager_Instance == null)
            {
                ShouldCheckForUiManager = false;
                return;
            }
            object returnval = VRCUiManager_Instance.Invoke(null, new object[0]);
            if (returnval == null)
                return;
            ShouldCheckForUiManager = false;
            if (Mods.Count > 0)
                for (int i = 0; i < Mods.Count; i++)
                    if (Mods[i] != null)
                        try { Mods[i].VRChat_OnUiManagerInit(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Mods[i].Info.Name); }
            if (Plugins.Count > 0)
                for (int i = 0; i < Plugins.Count; i++)
                    if (Plugins[i] != null)
                        try { Plugins[i].VRChat_OnUiManagerInit(); } catch (Exception ex) { MelonLogger.LogMelonError(ex.ToString(), Plugins[i].Info.Name); }
        }

        private enum LoadMode
        {
            NORMAL,
            DEV,
            BOTH
        }
    }
}
