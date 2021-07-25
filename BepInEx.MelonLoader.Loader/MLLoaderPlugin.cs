using System;
using BepInEx.IL2CPP;

namespace BepInEx.MelonLoaderLoader
{
	[BepInPlugin("io.bepis.melonloader.loader", "MelonLoader Plugin Loader", "0.4.0.0")]
	public class MLLoaderPlugin : BasePlugin
	{
		public override void Load()
		{
			AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
			{
				if (args.Name.Contains("MelonLoader"))
					return typeof(MelonLoader.Core).Assembly;

				return null;
			};
			
			MelonLoader.Core.Initialize(Config);
			MelonLoader.Core.PreStart();
			MelonLoader.Core.Start();
		}
	}
}