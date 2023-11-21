using BepInEx.Logging;
using Mono.Cecil;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace MelonLoader.InternalUtils;

internal static class ObfuscationCheck
{
	// This was accurate as of MelonLoader 0.4.3
	public static void Initialize()
	{
		HarmonyLib.Harmony.CreateAndPatchAll(typeof(ObfuscationCheck));

		AppDomain.CurrentDomain.TypeResolve += (sender, args) =>
		{
			MelonLogger.PushMessageToBepInEx("Obfuscation Check", $"Type check failure: {args.Name}", LogLevel.Debug);

			if (args.Name == "HarmonyLib.Harmony")
				return typeof(HarmonyLib.Harmony).Assembly;

			return null;
		};

		AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
		{
			MelonLogger.PushMessageToBepInEx("Obfuscation Check", $"Assembly check failure: {args.Name}",
				LogLevel.Debug);

			if (args.Name == "HarmonyLib.Harmony")
				return typeof(HarmonyLib.Harmony).Assembly;

			return null;
		};
	}

	[HarmonyPrefix, HarmonyPatch(typeof(Assembly), nameof(Assembly.Load), typeof(byte[]))]
	public static void AssemblyLoadPrefix(byte[] rawAssembly)
	{
		using var memoryStream = new MemoryStream(rawAssembly);

		if (!CheckAssembly(memoryStream))
			throw new BadImageFormatException("Obfuscated assembly");
	}

	public static bool CheckAssembly(Stream stream)
	{
		using var assemblyDefinition = AssemblyDefinition.ReadAssembly(stream);

		if (assemblyDefinition.Modules.Count > 1)
		{
			MelonLogger.PushMessageToBepInEx("Obfuscation Check",
				$"Obfuscation check on {assemblyDefinition.FullName} failed due assembly containing more than one module",
				LogLevel.Error);
			return false;
		}

		foreach (var type in assemblyDefinition.MainModule.Types)
		{
			if (type.BaseType?.FullName == "System.MulticastDelegate"
			    || type.Name == "<Module>")
			{
				if (type.Methods.Any(x => x.Name == ".cctor"))
				{
					MelonLogger.PushMessageToBepInEx("Obfuscation Check",
						$"Obfuscation check on {assemblyDefinition.FullName} failed due to {type.FullName} containing methods",
						LogLevel.Error);
					return false;
				}

				if (type.HasFields)
				{
					MelonLogger.PushMessageToBepInEx("Obfuscation Check",
						$"Obfuscation check on {assemblyDefinition.FullName} failed due to {type.FullName} containing fields",
						LogLevel.Error);
					return false;
				}
			}
		}

		return true;
	}
}