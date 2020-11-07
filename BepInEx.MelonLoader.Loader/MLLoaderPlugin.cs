using BepInEx.IL2CPP;
using MelonLoader;

namespace BepInEx.MelonLoader.Loader
{
	[BepInPlugin("io.bepis.mlloaderplugin", "MelonLoader Plugin Loader", "1.0")]
    public class MLLoaderPlugin : BasePlugin
    {
	    public override void Load()
	    {
		    MelonLoaderBase.Initialize();
		    MelonLoaderBase.Startup();
	    }
    }
}