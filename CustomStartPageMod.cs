using Brutal.ImGuiApi;
using Brutal.Numerics;
using HarmonyLib;
using KSA;
using StarMap.API;

namespace Custom_Start_Page_Tools
{
    [StarMapMod]
    public class CustomStartPageMod
    {
        private static readonly Harmony _harmony = new Harmony("Custom Start Page Tools");
        public static List<ModData> ActiveMods = new List<ModData>();
        public static List<ModData> InactiveMods = new List<ModData>();
        public static bool DrawSavesWindow = false;
        private static bool _initalized = false;
        public static SavesMenuUi SavesUi = new SavesMenuUi(ref _initalized);

        [StarMapImmediateLoad]
        public void OnImediateLoad(Mod mod)
        {
            _harmony.PatchAll(typeof(CustomStartPageMod).Assembly);

            foreach (ModEntry modEntry in ModLibrary.Manifest.Mods)
            {
                if (modEntry.Enabled)
                {
                    ActiveMods.Add(ModData.CreateModData(modEntry));
                }
                else
                {
                    InactiveMods.Add(ModData.CreateModData(modEntry));
                }
            }
        }

        [StarMapBeforeGui]
        public void OnGui(double dt)
        {
            if (!DrawSavesWindow)
            {
                SavesUi.SelectedSave = null;
                SavesUi.SelectedSaveData = null;
                return;
            }

            if (!_initalized)
            {
                SavesMenuUi.GameSavesData.Clear();
                SavesMenuUi.GameSaves = Traverse.CreateWithType("KSA.GameSaves").Property("Saves").GetValue<LookupCollection<GameSave>>();
                foreach (GameSave save in SavesMenuUi.GameSaves.AsSpan())
                {
                    SavesMenuUi.GameSavesData.Add(save.Id, SaveData.Load(((UncompressedSave)save).Directory.FullName)!);
                }
                _initalized = true;
            }

            if (SavesUi.NewSave)
            {
                SavesUi.DrawNewSaveWindow();
            }

            if (SavesUi.SaveLoadError)
            {
                SavesUi.DrawSaveErrorsPopup(SavesUi.ModErrors);
            }

            if (SavesUi.DeleteSave)
            {
                SavesUi.DrawDeleteSaveWarning();
            }

            ImGui.SetNextWindowSize(new float2(float.Clamp(float2.Unpack(Program.GetWindow().Size).X * (0.65f / GameSettings.GetInterfaceScale()), 400f, 800f), -1f), ImGuiCond.Appearing);
            if(ImGui.Begin("Game Saves", ref DrawSavesWindow, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            {
                SavesUi.Draw();
            }
            ImGui.End();
        }

        [StarMapUnload]
        public void OnUnload()
        {
            _harmony.UnpatchAll(nameof(CustomStartPageMod));
        }
    }
}
