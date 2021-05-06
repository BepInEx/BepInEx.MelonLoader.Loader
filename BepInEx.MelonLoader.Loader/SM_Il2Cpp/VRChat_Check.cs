using System;
using System.Linq;
using System.Reflection;

namespace MelonLoader.Support
{
    static class VRChat_Check
    {
        public static bool ShouldCheck { get; set; } = MelonUtils.IsVRChat;
        private static Assembly Assembly_CSharp = null;
        private static Type VRCUiManager = null;
        private static MethodInfo VRCUiManager_Instance = null;

        internal static void VRChat_CheckUiManager()
        {
            if (!ShouldCheck)
                return;
            if (Assembly_CSharp == null)
                Assembly_CSharp = Assembly.Load("Assembly-CSharp");
            if (Assembly_CSharp == null)
                return;
            if (VRCUiManager == null)
                VRCUiManager = Assembly_CSharp.GetType("VRCUiManager");
            if (VRCUiManager == null)
                return;
            if (VRCUiManager_Instance == null)
                VRCUiManager_Instance = VRCUiManager.GetMethods().First(x => (x.ReturnType == VRCUiManager));
            if (VRCUiManager_Instance == null)
                return;
            object returnval = VRCUiManager_Instance.Invoke(null, new object[0]);
            if (returnval == null)
                return;
            ShouldCheck = false;
            Main.Interface.VRChat_OnUiManagerInit();
        }
    }
}
