using System;
using System.Collections;
using System.IO;
using System.Reflection;
using MelonLoader.Support;

namespace MelonLoader
{
    public interface ISupportModule
    {
        int GetActiveSceneIndex();
        object StartCoroutine(IEnumerator coroutine);
        void StopCoroutine(object coroutineToken);
        void UnityDebugLog(string msg);
        void Destroy();
    }

    internal static class SupportModule
    {
        private static Assembly assembly = null;
        private static Type type = null;
        internal static ISupportModule supportModule = null;

        internal static void Initialize()
        {
	        supportModule = IL2CPPMain.Initialize();
        }

        internal static bool IsOldUnity()
        {
            try
            {
                Assembly unityengine = Assembly.Load("UnityEngine");
                if (unityengine != null)
                {
                    Type scenemanager = unityengine.GetType("UnityEngine.SceneManagement.SceneManager");
                    if (scenemanager != null)
                    {
                        EventInfo sceneLoaded = scenemanager.GetEvent("sceneLoaded");
                        if (sceneLoaded != null)
                            return false;
                    }
                }
            }
            catch (Exception e) { }
            return true;
        }

        internal static int GetActiveSceneIndex() => supportModule?.GetActiveSceneIndex() ?? -9;
        internal static void UnityDebugLog(string msg) => supportModule?.UnityDebugLog(msg);
        internal static void Destroy() { supportModule?.Destroy(); supportModule = null; }
    }
}
