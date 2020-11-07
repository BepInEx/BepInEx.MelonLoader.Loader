using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace MelonLoader
{
    public static class MelonLoaderBase
    {
        internal static MelonGameAttribute CurrentGameAttribute;
        public static bool IsVRChat { get; internal set; }
        public static bool IsBoneworks { get; internal set; }

        public static void Initialize()
        {
            Setup();
            MelonHandler.LoadAll(true);
            if (!MelonHandler.HasLoadedPlugins)
                return;
            MelonPrefs.Setup();
            MelonHandler.OnPreInitialization();
        }

        public static void Startup()
        {
            SetupSupport();
            WelcomeLog();
            MelonHandler.LoadAll();
            MelonHandler.LogAndPrune();
            if (!MelonHandler.HasLoadedPlugins)
                return;
            MelonPrefs.Setup();
            AddUnityDebugLog();
            MelonHandler.OnApplicationStart();
            if (!MelonHandler.HasLoadedPlugins)
                SupportModule.Destroy();
        }

        internal static void Quit()
        {
            if (MelonHandler.HasLoadedPlugins)
                MelonPrefs.SaveConfig();
        }

        private static void Setup()
        {
            FixCurrentBaseDirectory();
            AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;
            CurrentGameAttribute = new MelonGameAttribute(Imports.GetCompanyName(), Imports.GetProductName());
            if (Imports.IsIl2CppGame())
            {
                IsVRChat = CurrentGameAttribute.IsGame("VRChat", "VRChat");
                IsBoneworks = CurrentGameAttribute.IsGame("Stress Level Zero", "BONEWORKS");
            }
        }

        private static void SetupSupport()
        {
            if (Imports.IsIl2CppGame())
            {
                if (IsVRChat)
                    MelonHandler.Assembly_CSharp = Assembly.Load("Assembly-CSharp");
                UnhollowerSupport.Initialize();
            }
            SupportModule.Initialize();
        }

        private static void WelcomeLog()
        {
            MelonLogger.Log("------------------------------");
            MelonLogger.Log("Unity " + _UnityVersion);
            MelonLogger.Log("OS: " + Environment.OSVersion);
            MelonLogger.Log("------------------------------");
            MelonLogger.Log("Name: " + CurrentGameAttribute.GameName);
            MelonLogger.Log("Developer: " + CurrentGameAttribute.Developer);
            MelonLogger.Log("Type: " + (Imports.IsIl2CppGame() ? "Il2Cpp" : "Mono"));
            MelonLogger.Log("------------------------------");
            MelonLogger.Log($"Using {BuildInfo.Name} v{BuildInfo.Version}");
            MelonLogger.Log("------------------------------");
        }

        private static void AddUnityDebugLog()
        {
            SupportModule.UnityDebugLog("--------------------------------------------------------------------------------------------------");
            SupportModule.UnityDebugLog("~   This Game has been MODIFIED using MelonLoader. DO NOT report any issues to the Developers!   ~");
            SupportModule.UnityDebugLog("--------------------------------------------------------------------------------------------------");
        }

        public static void FixCurrentBaseDirectory()
        {
            //((AppDomainSetup)typeof(AppDomain).GetProperty("SetupInformationNoCopy", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(AppDomain.CurrentDomain, new object[0])).ApplicationBase = Imports.GetGameDirectory();
            //Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        }

        private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs e) => MelonLogger.LogError((e.ExceptionObject as Exception).ToString());

        private static string _UserDataPath = null;
        public static string UserDataPath
        {
            get
            {
                if (_UserDataPath == null)
                {
                    _UserDataPath = Path.Combine(Imports.GetGameDirectory(), "UserData");
                    if (!Directory.Exists(_UserDataPath))
                        Directory.CreateDirectory(_UserDataPath);
                }
                return _UserDataPath;
            }
        }

        private static string _UnityVersion = null;
        public static string UnityVersion
        {
            get
            {
                if (_UnityVersion != null)
                    return _UnityVersion;
                string exepath = Imports.GetExePath();
                string ggm_path = Path.Combine(Imports.GetGameDataDirectory(), "globalgamemanagers");
                if (!File.Exists(ggm_path))
                {
                    FileVersionInfo versioninfo = FileVersionInfo.GetVersionInfo(exepath);
                    if ((versioninfo == null) || string.IsNullOrEmpty(versioninfo.FileVersion))
                        return "UNKNOWN";
                    return versioninfo.FileVersion.Substring(0, versioninfo.FileVersion.LastIndexOf('.'));
                }
                byte[] ggm_bytes = File.ReadAllBytes(ggm_path);
                if ((ggm_bytes == null) || (ggm_bytes.Length <= 0))
                    return "UNKNOWN";
                int start_position = 0;
                for (int i = 10; i < ggm_bytes.Length; i++)
                {
                    byte pos_byte = ggm_bytes[i];
                    if ((pos_byte <= 0x39) && (pos_byte >= 0x30))
                    {
                        start_position = i;
                        break;
                    }
                }
                if (start_position == 0)
                    return "UNKNOWN";
                int end_position = 0;
                for (int i = start_position; i < ggm_bytes.Length; i++)
                {
                    byte pos_byte = ggm_bytes[i];
                    if ((pos_byte != 0x2E) && ((pos_byte > 0x39) || (pos_byte < 0x30)))
                    {
                        end_position = (i - 1);
                        break;
                    }
                }
                if (end_position == 0)
                    return "UNKNOWN";
                int verstr_byte_pos = 0;
                byte[] verstr_byte = new byte[((end_position - start_position) + 1)];
                for (int i = start_position; i <= end_position; i++)
                {
                    verstr_byte[verstr_byte_pos] = ggm_bytes[i];
                    verstr_byte_pos++;
                }
                return _UnityVersion = Encoding.UTF8.GetString(verstr_byte, 0, verstr_byte.Length);
            }
        }
    }
}