using System;
using System.Collections;

namespace MelonLoader
{
    internal static class SupportModule
    {
        internal static ISupportModule_To Interface = null;

        internal static bool Initialize()
        {
            MelonLogger.Msg("Loading Support Module...");

            if (!MelonUtils.IsGameIl2Cpp())
	            throw new Exception("Non-IL2CPP MelonLoader plugins are currently not supported");

            Interface = SM_Il2Cpp.Main.Initialize(new SupportModule_From());
            return true;
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
        void OnSceneWasUnloaded(int buildIndex, string sceneName);
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
        public void OnSceneWasUnloaded(int buildIndex, string sceneName) => MelonHandler.OnSceneWasUnloaded(buildIndex, sceneName);
        public void Update() => MelonHandler.OnUpdate();
        public void FixedUpdate() => MelonHandler.OnFixedUpdate();
        public void LateUpdate() => MelonHandler.OnLateUpdate();
        public void OnGUI() => MelonHandler.OnGUI();
        public void Quit() => Core.Quit();
        public void BONEWORKS_OnLoadingScreen() => MelonHandler.BONEWORKS_OnLoadingScreen();
    }
}