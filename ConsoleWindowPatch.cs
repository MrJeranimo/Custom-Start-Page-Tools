using Brutal.ImGuiApi.Abstractions;
using HarmonyLib;
using KSA;

namespace Custom_Start_Page_Tools
{
    [HarmonyPatch]
    public class ConsoleWindowPatch
    {
        [HarmonyPatch(typeof(ConsoleWindow), "EnableMessageFading")]
        [HarmonyPostfix]
        public static void LoadSaveOnLaunch()
        {
            if (StartPagePatch._loadSave)
            {
                GameSaves.LoadSaveGame(StartPagePatch._selectedSave!.Id);
            }
        }
    }
}
