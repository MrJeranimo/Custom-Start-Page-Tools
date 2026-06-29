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
        private static float2 _buttonSize = new float2(150f, 30f);
        private static LookupCollection<GameSave> _gameSaves = new LookupCollection<GameSave>("temp");
        private static GameSave? _selectedSave;
        private static SaveData? _selectedSaveData;
        private static Dictionary<string, SaveData> _gameSavesData = new Dictionary<string, SaveData>();
        private static bool _newSave = false;
        private static bool _saveLoadError = false;
        private static bool _deleteSave = false;
        private static bool _noSaveData = false;
        private static List<string> _modErrors = new List<string>();
        private static ImInputString _saveName = new ImInputString(255, "");

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
                _selectedSave = null;
                _selectedSaveData = null;
                return;
            }

            if (!_initalized)
            {
                _gameSavesData.Clear();
                _gameSaves = Traverse.CreateWithType("KSA.GameSaves").Property("Saves").GetValue<LookupCollection<GameSave>>();
                foreach (GameSave save in _gameSaves.AsSpan())
                {
                    _gameSavesData.Add(save.Id, SaveData.Load(((UncompressedSave)save).Directory.FullName)!);
                }
                _initalized = true;
            }

            if (_newSave)
            {
                DrawNewSaveWindow();
            }

            if (_saveLoadError)
            {
                DrawSaveErrorWindow(_modErrors);
            }

            if (_deleteSave)
            {
                DrawDeleteSaveWindow();
            }

            ImGui.SetNextWindowSize(new float2(float.Clamp(float2.Unpack(Program.GetWindow().Size).X * (0.65f / GameSettings.GetInterfaceScale()), 400f, 800f), -1f), ImGuiCond.Appearing);
            if(ImGui.Begin("Game Saves", ref DrawSavesWindow, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            {
                if (_selectedSave != null)
                {
                    ImGui.Columns(2, "Save Cloumns", false);
                    float windowWidth = ImGui.GetWindowWidth();
                    ImGui.SetColumnWidth(0, windowWidth * 0.4f);
                }
                StartPagePatch.BeginBox("Select Save", true, false, regionHeight: 500f);
                foreach (GameSave save in _gameSaves.AsSpan())
                {
                    DrawSaveBox(save, _gameSavesData.GetValueSafe(save.Id));
                }
                StartPagePatch.EndBox();
                if (_selectedSave != null)
                {
                    ImGui.NextColumn();
                    DrawSaveInfoBox();
                    ImGui.Columns();
                }
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - (_buttonSize.X * 4)) / 2f);
                if (ImGui.Button("New Save", _buttonSize))
                {
                    _newSave = true;
                }
                ImGui.SameLine();
                if (_selectedSave == null)
                    ImGui.BeginDisabled();
                if (ImGui.Button("Delete Save", _buttonSize))
                {
                    _deleteSave = true;
                }
                ImGui.SameLine();
                if (ImGui.Button("Load Save ##Button", _buttonSize))
                {
                    if (_selectedSaveData != null)
                    {
                        _noSaveData = false;
                        _modErrors = _selectedSaveData.CheckMods();
                        if (_modErrors.Count > 0 || _selectedSave!.Version != VersionInfo.Current.VersionString || _selectedSaveData.SystemInfoName != Universe.CurrentSystem!.Id || _selectedSaveData.GameType != GameSettings.Current.System.StartGameType)
                        {
                            _saveLoadError = true;
                        }
                        else
                        {
                            GameSaves.LoadSaveGame(_selectedSave.Id);
                            DrawSavesWindow = false;
                        }
                    }
                    else
                    {
                        _noSaveData = true;
                        _saveLoadError = true;
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Overwrite Save", _buttonSize))
                {
                    GameSaves.MakeSave(_selectedSave!.Id);
                    _initalized = false;
                }
                if (_selectedSave == null)
                    ImGui.EndDisabled();
            }
            ImGui.End();
        }

        [StarMapUnload]
        public void OnUnload()
        {
            _harmony.UnpatchAll(nameof(CustomStartPageMod));
        }

        public static void DrawNewSaveWindow()
        {
            ImGui.SetNextWindowSize(new float2(float.Clamp(float2.Unpack(Program.GetWindow().Size).X * (0.65f / GameSettings.GetInterfaceScale()), 400f, 600f), -1f), ImGuiCond.Appearing);
            if (ImGui.Begin("Delete Save", ref _newSave, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            {
                ImGui.PushFont(default(ImFontPtr), 40);
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("New Save").X) / 2f);
                ImGui.Text("New Save");
                ImGui.PopFont();
                ImGui.Text("Save Name:");
                ImGui.SameLine();
                ImGui.InputText("", _saveName);
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - (_buttonSize.X * 2)) / 2f);
                if (ImGui.Button("Cancel", _buttonSize))
                {
                    _newSave = false;
                }
                ImGui.SameLine();
                if (ImGui.Button("Save", _buttonSize))
                {
                    GameSaves.MakeSave(_saveName.Value.ToString());
                    _newSave = false;
                    _initalized = false;
                }
            }
            ImGui.End();
        }

        public static void DrawDeleteSaveWindow()
        {
            ImGui.SetNextWindowSize(new float2(float.Clamp(float2.Unpack(Program.GetWindow().Size).X * (0.65f / GameSettings.GetInterfaceScale()), 400f, 800f), 285f), ImGuiCond.Appearing);
            if (ImGui.Begin("Delete Save", ref _deleteSave, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            {
                ImGui.PushFont(default(ImFontPtr), 40);
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("Warning").X) / 2f);
                ImGui.Text("Warning");
                ImGui.PopFont();
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.TextWrapped("Deleting this save cannot be undone!");
                ImGui.Spacing();
                ImGui.Spacing();
                DrawSaveBox(_selectedSave!, _selectedSaveData);
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - (_buttonSize.X * 2)) / 2f);
                if (ImGui.Button("Cancel", _buttonSize))
                {
                    _deleteSave = false;
                }
                ImGui.SameLine();
                if (ImGui.Button("Delete", _buttonSize))
                {
                    _selectedSave!.Delete();
                    _selectedSave = null;
                    _selectedSaveData = null;
                    _deleteSave = false;
                }
            }
            ImGui.End();
        }

        public static void DrawSaveErrorWindow(List<string> modErrors)
        {
            ImGui.SetNextWindowSize(new float2(float.Clamp(float2.Unpack(Program.GetWindow().Size).X * (0.65f / GameSettings.GetInterfaceScale()), 400f, 800f), -1f), ImGuiCond.Appearing);
            if (ImGui.Begin("Load Error", ref _saveLoadError, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            {
                ImGui.PushFont(default(ImFontPtr), 40);
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("Warning").X) / 2f);
                ImGui.Text("Warning");
                ImGui.PopFont();
                ImGui.TextWrapped("The game version and/or active mods may not match the selected save's game version and/or active mods.");
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                if (_selectedSave!.Version != VersionInfo.Current.VersionString)
                {
                    ImGui.Text("KSA Game Versions do not match:");
                    ImGui.TextDisabled($"Current Version: {VersionInfo.Current.VersionString}\nSave's Version: {_selectedSave.Version}");
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();
                }
                if (_selectedSaveData != null)
                {
                    if (_selectedSaveData.SystemInfoName != Universe.CurrentSystem!.Id)
                    {
                        ImGui.Text("Celestial Systems do not match:");
                        ImGui.TextDisabled($"Current System: {Universe.CurrentSystem!.Id}\nSave's System: {_selectedSaveData.SystemInfoName}");
                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                    }
                    if (_selectedSaveData.GameType != GameSettings.Current.System.StartGameType)
                    {
                        ImGui.Text("Game Types do not match:");
                        ImGui.TextDisabled($"Current Game Type: {GameSettings.Current.System.StartGameType}\nSave's Game Type: {_selectedSaveData.GameType}");
                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                    }
                }
                if (_noSaveData)
                {
                    ImGui.Text("There is no meta data about the mods for this save.");
                }
                foreach (string error in modErrors)
                {
                    ImGui.TextWrapped(error);
                }
                if (modErrors.Count > 0)
                {
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();
                }
                ImGui.TextWrapped("You can attempt to load the save with the error/warnings, but it is not recommended.");
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - (_buttonSize.X * 2)) / 2f);
                if (ImGui.Button("Cancel", _buttonSize))
                {
                    _saveLoadError = false;
                }
                ImGui.SameLine();
                if (ImGui.Button("Load Anyway", _buttonSize))
                {
                    GameSaves.LoadSaveGame(_selectedSave.Id);
                    DrawSavesWindow = false;
                    _saveLoadError = false;
                }
            }
            ImGui.End();
        }

        public static void DrawSaveBox(GameSave save, SaveData? saveData)
        {
            StartPagePatch.BeginBox($"##{save.Id}'s Box", true, regionHeight: -1f);
            ImGui.TextWrapped(save.Id);
            if (saveData != null)
                ImGui.TextColored(new ImColor8(144, 238, 144, 255).AsFloat4(), saveData.GameType.ToString()!);
            ImGui.TextDisabled(save.Version.VersionString);
            if (saveData != null && saveData.Mods.Count > 1)
            {
                ImGui.TextDisabled("Modded");
            }
            if (save == _selectedSave)
            {
                ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), new ImColor8(211, 211, 211, 20));
            }
            StartPagePatch.EndBox();

            if (ImGui.IsItemClicked())
            {
                _selectedSave = save;
                _selectedSaveData = saveData;
            }
        }

        public static void DrawSaveInfoBox()
        {
            StartPagePatch.BeginBox("Save Info", true, false, regionHeight: 500f);
            ImGui.PushFont(default(ImFontPtr), 32);
            ImGui.TextWrapped(_selectedSave!.Id);
            ImGui.PopFont();
            if (_selectedSaveData != null)
                ImGui.TextColored(new ImColor8(144, 238, 144, 255).AsFloat4(), _selectedSaveData.GameType.ToString()!);
            ImGui.BeginDisabled();
            ImGui.TextWrapped($"KSA Version: {_selectedSave.Version}");
            ImGui.EndDisabled();
            if (_selectedSaveData != null && _selectedSaveData.Mods.Count > 1)
            {
                ImGui.TextDisabled("Modded");
            }
            if (_selectedSaveData != null)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                SimTime simTime = new SimTime(_selectedSaveData.SimTime);
                string timeString = $"{simTime.ValueIn(TimeUnit.Years):F0} Years, {simTime.ValueIn(TimeUnit.Days) % 365:F0} Days, {simTime.ValueIn(TimeUnit.Hours) % 24:F0} Hours";
                ImGui.TextWrapped(timeString);
                ImGui.TextWrapped("Number of Vehicles:");
                ImGui.SameLine();
                ImGui.BeginDisabled();
                ImGui.TextWrapped($"{_selectedSaveData.VehicleCount}");
                ImGui.EndDisabled();
                if (_selectedSaveData.LastControlledVehicle != null)
                {
                    ImGui.TextWrapped("Last Controlled Vehicle:");
                    ImGui.SameLine();
                    ImGui.BeginDisabled();
                    ImGui.TextWrapped($"{_selectedSaveData.LastControlledVehicle}");
                    ImGui.EndDisabled();
                    ImGui.TextWrapped("Parent Body:");
                    ImGui.SameLine();
                    ImGui.BeginDisabled();
                    ImGui.TextWrapped($"{_selectedSaveData.LastControlledVehicleParent!}");
                    ImGui.EndDisabled();
                }
                StartPagePatch.DrawModHook();
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.PushFont(default(ImFontPtr), 24);
                ImGui.Text("Mods:");
                ImGui.PopFont();
                foreach (SaveModData mod in _selectedSaveData.Mods)
                {
                    string modString = $"{mod.Name} - {(mod.Version ?? "")}";
                    ImGui.TextWrapped(modString);
                }
            }
            StartPagePatch.EndBox();
        }
    }
}
