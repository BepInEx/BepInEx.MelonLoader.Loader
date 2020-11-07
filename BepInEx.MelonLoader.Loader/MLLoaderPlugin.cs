using System;
using BepInEx.IL2CPP;
using MelonLoader;

namespace BepInEx.MelonLoader.Loader
{
	[BepInPlugin("io.bepis.mlloaderplugin", "MelonLoader Plugin Loader", "1.0")]
    public class MLLoaderPlugin : BasePlugin
    {
	    public override void Load()
	    {
		    AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
		    {
			    if (args.Name.Contains("MelonLoader.ModHandler"))
				    return typeof(MLLoaderPlugin).Assembly;

			    return null;
		    };

		    MelonLoaderBase.Initialize();
		    MelonLoaderBase.Startup();
	    }
    }
}