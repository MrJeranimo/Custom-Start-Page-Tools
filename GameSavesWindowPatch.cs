using Brutal.ImGuiApi;
using Brutal.Numerics;
using HarmonyLib;
using KSA;

namespace Custom_Start_Page_Tools
{
    [HarmonyPatch]
    public class GameSavesWindowPatch
    {
        [HarmonyPatch(typeof(GameSaves), "Toggle")]
        [HarmonyPrefix]
        public static bool GameSavesTogglePatch()
        {
            CustomStartPageMod.DrawSavesWindow = !CustomStartPageMod.DrawSavesWindow;
            return false;
        }

        [HarmonyPatch(typeof(GameSaves), "IsOpen", MethodType.Getter)]
        [HarmonyPrefix]
        public static bool GameSavesIsOpenPatch(ref bool __result)
        {
            __result = CustomStartPageMod.DrawSavesWindow;
            return false;
        }
    }
}
