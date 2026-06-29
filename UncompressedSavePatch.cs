using HarmonyLib;
using KSA;

namespace Custom_Start_Page_Tools
{
    [HarmonyPatch]
    public class UncompressedSavePatch
    {

        [HarmonyPatch(typeof(UncompressedSave), "Write")]
        [HarmonyPostfix]
        public static void WriteSaveData(UncompressedSave __instance)
        {
            new SaveData(__instance.MetaData.Name).Save(__instance.Directory.FullName);
        }

        [HarmonyPatch(typeof(UncompressedSave), "Load")]
        [HarmonyPrefix]
        public static bool CheckSaveData(UncompressedSave __instance)
        {
            SaveData? save = SaveData.Load(__instance.Directory.FullName);
            if (save != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
