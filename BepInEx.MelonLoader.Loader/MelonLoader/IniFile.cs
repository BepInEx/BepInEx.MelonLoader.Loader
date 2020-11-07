using System.Linq;
using BepInEx.Configuration;

namespace MelonLoader
{
	public class IniFile
	{
		public ConfigFile BepInExConfigFile { get; private set; }

		public string Path { get; }

		public IniFile(string INIPath)
		{
			Path = System.IO.Path.ChangeExtension(INIPath, "cfg");
			BepInExConfigFile = new ConfigFile(Path, true);
		}

		public bool HasKey(string section, string name)
		{
			return BepInExConfigFile.Any(x => x.Key.Section == section && x.Key.Key == name);
		}

		private T GetValue<T>(string section, string name, T defaultValue)
		{
			if (!BepInExConfigFile.TryGetEntry<T>(section, name, out var entry))
			{
				return defaultValue;
			}

			return entry.Value;
		}

		private void SetValue<T>(string section, string name, T value)
		{
			var bindedConfig = BepInExConfigFile.Bind(section, name, value);
			bindedConfig.Value = value;
			BepInExConfigFile.Save();
		}

		public string GetString(string section, string name, string defaultValue = "", bool autoSave = false)
			=> GetValue(section, name, defaultValue);

		public void SetString(string section, string name, string value)
			=> SetValue(section, name, value);

		public int GetInt(string section, string name, int defaultValue = 0, bool autoSave = false)
			=> GetValue(section, name, defaultValue);

		public void SetInt(string section, string name, int value)
			=> SetValue(section, name, value);

		public float GetFloat(string section, string name, float defaultValue = 0f, bool autoSave = false)
			=> GetValue(section, name, defaultValue);

		public void SetFloat(string section, string name, float value)
			=> SetValue(section, name, value);

		public bool GetBool(string section, string name, bool defaultValue = false, bool autoSave = false)
			=> GetValue(section, name, defaultValue);

		public void SetBool(string section, string name, bool value)
			=> SetValue(section, name, value);
	}
}