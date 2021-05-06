using System;
using System.IO;
using System.Collections;
using System.Reflection;

namespace MelonLoader
{
    internal static class SupportModule
    {
        internal static ISupportModule_To Interface = null;

        internal static bool Initialize()
        {
            // initialy this would load the support module .dll but since it's been merged into the same assembly it needs to be modified

            MelonLogger.Msg("Loading Support Module...");

            if (!MelonUtils.IsGameIl2Cpp())
			{
                MelonLogger.ThrowInternalFailure("BepInEx.MelonLoader.Loader currently only supports IL2CPP games");
			}

            try
            {
                Type type = typeof(MelonLoader.Support.Main);
                if (type == null)
                {
                    MelonLogger.Error("Failed to Get Type MelonLoader.Support.Main!");
                    return false;
                }
                MethodInfo method = type.GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Static);
                if (method == null)
                {
                    MelonLogger.Error("Failed to Get Method Initialize!");
                    return false;
                }
                Interface = (ISupportModule_To)method.Invoke(null, new object[] { new SupportModule_From() });
                if (Interface == null)
                {
                    MelonLogger.Error("Failed to Initialize Interface!");
                    return false;
                }
            }
            catch(Exception ex) { MelonLogger.Error(ex.ToString()); return false; }
            return true;
        }

        private static bool IsOldUnity()
        {
            try
            {
                Assembly unityengine = Assembly.Load("UnityEngine");
                if (unityengine == null)
                    return true;
                Type scenemanager = unityengine.GetType("UnityEngine.SceneManagement.SceneManager");
                if (scenemanager == null)
                    return true;
                EventInfo sceneLoaded = scenemanager.GetEvent("sceneLoaded");
                if (sceneLoaded == null)
                    return true;
                return false;
            }
            catch { return true; }
        }
    }

    public interface ISupportModule_To
    {
        object StartCoroutine(IEnumerator coroutine);
        void StopCoroutine(object coroutineToken);
        void UnityDebugLog(string msg);
    }

    public interface ISupportModule_From
    {
        void OnSceneWasLoaded(int buildIndex, string sceneName);
        void OnSceneWasInitialized(int buildIndex, string sceneName);
        void Update();
        void FixedUpdate();
        void LateUpdate();
        void OnGUI();
        void Quit();
        void BONEWORKS_OnLoadingScreen();
    }

    internal class SupportModule_From : ISupportModule_From
    {
        public void OnSceneWasLoaded(int buildIndex, string sceneName) => MelonHandler.OnSceneWasLoaded(buildIndex, sceneName);
        public void OnSceneWasInitialized(int buildIndex, string sceneName) => MelonHandler.OnSceneWasInitialized(buildIndex, sceneName);
        public void Update() => MelonHandler.OnUpdate();
        public void FixedUpdate() => MelonHandler.OnFixedUpdate();
        public void LateUpdate() => MelonHandler.OnLateUpdate();
        public void OnGUI() => MelonHandler.OnGUI();
        public void Quit() => Core.Quit();
        public void BONEWORKS_OnLoadingScreen() => MelonHandler.BONEWORKS_OnLoadingScreen();
    }
}