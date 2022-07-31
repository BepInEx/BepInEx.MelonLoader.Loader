using System;
using MLCore = MelonLoader.Core;

namespace BepInEx.MelonLoader.Loader.UnityMono;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            if (args.Name.Contains("MelonLoader"))
                return typeof(MLCore).Assembly;
            return null;
        };

        MLCore.Initialize(Config, false);
        MLCore.PreStart();
        MLCore.Start();
    }
}