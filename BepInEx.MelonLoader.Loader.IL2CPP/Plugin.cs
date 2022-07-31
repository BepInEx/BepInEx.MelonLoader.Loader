using System;
using BepInEx.IL2CPP;
using MLCore = MelonLoader.Core;


namespace BepInEx.MelonLoader.Loader.IL2CPP
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        public override void Load()
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
}