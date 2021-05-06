using System;
using System.IO;
using System.Reflection;
using BepInEx.IL2CPP;

namespace BepInEx
{
	[BepInPlugin("io.bepis.mlloaderplugin", "MelonLoader Plugin Loader", "1.0")]
    public class MLLoaderPlugin : BasePlugin
    {
	    public override void Load()
	    {
		    AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
		    {
			    if (args.Name.Contains("MelonLoader"))
				    return typeof(MLLoaderPlugin).Assembly;

			    return null;
		    };


			// Force melonloader's copies of unhollowed assemblies to be used
		    if (Directory.Exists(MelonLoader.MelonUtils.GetEmulatedManagedDirectory()))
		    {
				foreach (var file in Directory.EnumerateFiles(
					MelonLoader.MelonUtils.GetEmulatedManagedDirectory(),
					"*.dll",
					SearchOption.TopDirectoryOnly))
				{
					Assembly.LoadFrom(file);
				}
		    }

		    MelonLoader.Core.Initialize();
		    MelonLoader.Core.Start();
	    }
    }
}