using Brutal.ImGuiApi;
using Brutal.Numerics;
using HarmonyLib;
using KSA;

namespace Custom_Start_Page_Tools
{
    [HarmonyPatch]
    public class StartPagePatch
    {
        public static float _windowWidth = float.Clamp(float2.Unpack(Program.GetWindow().Size).X * (0.65f / GameSettings.GetInterfaceScale()), 400f, 800f);
        public static readonly float2 _buttonSize = new float2(150f, 30f);
        private static string _showWindow = "Menu";
        private static bool _showActiveMods = true;
        private static bool _newGame = true;
        private static HashSet<string> _expandedMods = new HashSet<string>();
        private static bool _startGame = false;
        public static bool _loadSave = false;
        private static ConfigOnStartPopup? _instance;
        private static Traverse? _traverse;
        private static SystemInfo? _selectedSystem;
        private static GameTypeObject _selectedGameType;
        private static List<GameTypeObject> _gameTypes = new List<GameTypeObject>();
        private static ConfigOnStartPopup.VehicleObject _selectedVehicle;
        private static List<ConfigOnStartPopup.VehicleObject> _vehicles = new List<ConfigOnStartPopup.VehicleObject>();
        private static LocationObject _selectedLocation;
        private static List<LocationObject> _locations = new List<LocationObject>();
        private static LookupCollection<GameSave> _gameSaves = new LookupCollection<GameSave>("temp");
        public static GameSave? _selectedSave;
        public static SaveData? _selectedSaveData;
        private static Dictionary<string, SaveData> _gameSavesData = new Dictionary<string, SaveData>();
        private static bool _saveLoadError = false;
        private static bool _noSaveData = false;
        private static List<string> _modErrors = new List<string>();

        public static GameSave? SelectedSave => _selectedSave;
        public static SaveData? SelectedSaveData => _selectedSaveData;
        public static bool StartGame => _startGame;
        public static bool LoadSave => _loadSave;
        public static bool NewGame => _newGame;

        [HarmonyPatch(typeof(ConfigOnStartPopup), "OnDrawUi")]
        [HarmonyPrefix]
        public static bool ConfigStartPopupPatch(ConfigOnStartPopup __instance)
        {
            if (_loadSave)
            {
                if (_selectedSaveData != null)
                {
                    _selectedSystem = SelectSystem.Systems.Find(system => system.Id == _selectedSaveData.SystemInfoName);
                    if (_selectedSystem != null)
                    {
                        SystemLibrary.Default = _selectedSystem;
                        GameSettings.Current.System.LastSystemId = _selectedSystem.Id;
                        _traverse!.Method("SetVehicles").GetValue();
                        _traverse!.Method("SetStartingVehicleLocation").GetValue();
                        GameSettings.Current.Save();
                    }
                    GameSettings.Current.System.StartGameType = _selectedSaveData.GameType;
                }
                __instance.Active = false;
                return false;
            }

            if (_startGame)
            {
                __instance.Active = false;
                return false;
            }

            if (_instance == null)
            {
                _instance = __instance;
                _traverse = Traverse.Create(__instance);
                _selectedSystem = _traverse.Field("_systemTemplate").GetValue<SystemInfo>();
                _selectedGameType = _traverse.Field("_selectedGameType").GetValue<GameTypeObject>();
                _gameTypes = _traverse.Field("_gameTypes").GetValue<List<GameTypeObject>>();
                _selectedVehicle = _traverse.Field("_startingVehicle").GetValue<ConfigOnStartPopup.VehicleObject>();
                _vehicles = _traverse.Field("_vehicles").GetValue<List<ConfigOnStartPopup.VehicleObject>>();
                _selectedLocation = _traverse.Field("_startingLocation").GetValue<LocationObject>();
                _locations = _traverse.Field("_locations").GetValue<List<LocationObject>>();
                _gameSaves = Traverse.CreateWithType("KSA.GameSaves").Property("Saves").GetValue<LookupCollection<GameSave>>();
                foreach (GameSave save in _gameSaves.AsSpan())
                {
                    _gameSavesData.Add(save.Id, SaveData.Load(((UncompressedSave)save).Directory.FullName)!);
                }
            }

            switch (_showWindow)
            {
                case "Menu":
                    break;
                case "Singleplayer":
                    DrawSingleplayerMenu();
                    return false;
                case "Delete Save":
                    DrawDeleteSaveWarning();
                    return false;
                case "Multiplayer":
                    // TODO: Implement multiplayer menu
                    return false;
                case "Settings":
                    DrawSettingsMenu();
                    return false;
                case "Mods":
                    DrawModsMenu();
                    return false;
            }

            ImGui.SetNextWindowSize(new float2(167f, -1f), ImGuiCond.Always);
            ImGui.OpenPopup("Custom Start Page Tools");
            ImGui.BeginPopup("Custom Start Page Tools", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoResize);
            ImGuiHelper.SetCurrentWindowToCenter();
            if (ImGui.Button("Singleplayer", _buttonSize))
            {
                _showWindow = "Singleplayer";
            }
            ImGui.BeginDisabled();
            if (ImGui.Button("Multiplayer", _buttonSize))
            {
                _showWindow = "Multiplayer";
            }
            ImGui.EndDisabled();
            if (ImGui.Button("Settings", _buttonSize))
            {
                _showWindow = "Settings";
            }
            if (ImGui.Button("Mods", _buttonSize))
            {
                _showWindow = "Mods";
            }
            if (ImGui.Button("Quit", _buttonSize))
            {
                Environment.Exit(0);
            }
            ImGui.EndPopup();
            return false;
        }

        private static void DrawSingleplayerMenu()
        {
            if (_saveLoadError)
            {
                DrawSaveErrorsPopup(_modErrors);
                return;
            }

            ImGui.SetNextWindowSize(new float2(_windowWidth, -1f), ImGuiCond.Always);
            ImGui.OpenPopup("Custom Start Page Tools - Singleplayer");
            ImGui.BeginPopup("Custom Start Page Tools - Singleplayer", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoResize);
            ImGuiHelper.SetCurrentWindowToCenter();
            ImGui.PushFont(default(ImFontPtr), 40);
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("Singleplayer").X) / 2f);
            ImGui.Text("Singleplayer");
            ImGui.PopFont();
            float availableWidth = ImGui.GetContentRegionAvail().X;
            float buttonWidth = availableWidth / 2f;
            float buttonHeight = 30f;
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f);

            if (_newGame)
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
            else
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.Button]);

            if (ImGui.Button("New Game", new float2(buttonWidth, buttonHeight)))
            {
                _newGame = true;
                _selectedSave = null;
                _selectedSaveData = null;
            }

            ImGui.PopStyleColor();
            ImGui.SameLine(0f, 0f);

            if (!_newGame)
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
            else
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.Button]);

            if (ImGui.Button("Load A Save", new float2(buttonWidth, buttonHeight)))
            {
                _newGame = false;
            }

            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            if (_newGame) 
            {
                bool changed = false;
                BeginBox("System Selection", true, false);
                changed |= ImGuiHelper.DrawCombo<SystemInfo>("Systems", ref _selectedSystem!, SelectSystem.Systems);
                if (changed)
                {
                    SystemLibrary.Default = _selectedSystem;
                    GameSettings.Current.System.LastSystemId = _selectedSystem.Id;
                    _traverse!.Field("_systemTemplate").SetValue(_selectedSystem);
                    _traverse!.Method("SetVehicles").GetValue();
                    _traverse!.Method("SetStartingVehicleLocation").GetValue();
                    GameSettings.Current.Save();
                    _vehicles = _traverse.Field("_vehicles").GetValue<List<ConfigOnStartPopup.VehicleObject>>();
                    _selectedVehicle = _traverse.Field("_startingVehicle").GetValue<ConfigOnStartPopup.VehicleObject>();
                    _locations = _traverse.Field("_locations").GetValue<List<LocationObject>>();
                    _selectedLocation = _traverse.Field("_startingLocation").GetValue<LocationObject>();
                }
                changed |= ImGuiHelper.DrawCombo<GameTypeObject>("Game Type", ref _selectedGameType, _gameTypes);
                if (changed)
                {
                    GameSettings.Current.System.StartGameType = _selectedGameType.GameType;
                }
                bool testing = false;
                if (GameSettings.Current.System.StartGameType != GameSettings.GameType.Testing)
                {
                    testing = true;
                    ImGui.BeginDisabled();
                }
                if (_selectedVehicle.IsEnabled)
                {
                    changed |= ImGuiHelper.DrawCombo<ConfigOnStartPopup.VehicleObject>("Starting Vehicle", ref _selectedVehicle, _vehicles);
                    if (changed)
                    {
                        GameSettings.Current.System.StartVehicle = _selectedVehicle.Vehicle.Id;
                        _traverse!.Method("SetStartingVehicleLocation").GetValue();
                        _selectedLocation = _traverse.Field("_startingLocation").GetValue<LocationObject>();
                    }
                }
                if (_selectedLocation.IsEnabled)
                {
                    changed |= ImGuiHelper.DrawCombo<LocationObject>("Starting Location", ref _selectedLocation, _locations);
                    if (changed)
                    {
                        GameSettings.Current.System.StartSituation = _selectedLocation.Situation.Id;
                        GameSettings.Current.System.StartParent = _selectedLocation.Situation.CelestialId;
                    }
                }
                if (testing)
                {
                    ImGui.EndDisabled();
                }
                EndBox();
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - (_buttonSize.X * 2)) / 2f);
                if (ImGui.Button("Back", _buttonSize))
                {
                    _showWindow = "Menu";
                }
                ImGui.SameLine();
                if (ImGui.Button("Start Game", _buttonSize))
                {
                    _startGame = true;
                }
            }
            else
            {
                if (_selectedSave != null)
                {
                    ImGui.Columns(2, "Save Cloumns", false);
                    float windowWidth = ImGui.GetWindowWidth();
                    ImGui.SetColumnWidth(0, windowWidth * 0.4f);
                }
                BeginBox("Select Save", true, false, regionHeight: 500f);
                foreach (GameSave save in _gameSaves.AsSpan())
                {
                    DrawSaveBox(save, _gameSavesData[save.Id]);
                }
                EndBox();
                if (_selectedSave != null)
                {
                    ImGui.NextColumn();
                    DrawSaveInfoBox();
                    ImGui.Columns();
                }
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - (_buttonSize.X * 3)) / 2f);
                if (ImGui.Button("Back", _buttonSize))
                {
                    _showWindow = "Menu";
                    _selectedSave = null;
                    _selectedSaveData = null;
                }
                ImGui.SameLine();
                if (_selectedSave == null)
                    ImGui.BeginDisabled();
                if (ImGui.Button("Delete Save", _buttonSize))
                {
                    _showWindow = "Delete Save";
                }
                ImGui.SameLine();
                if (ImGui.Button("Load Save ##Button", _buttonSize))
                {
                    if (_selectedSaveData != null)
                    {
                        _noSaveData = false;
                        _modErrors = _selectedSaveData.CheckMods();
                        if (_modErrors.Count > 0 || _selectedSave!.Version != VersionInfo.Current.VersionString)
                        {
                            _saveLoadError = true;
                        }
                        else
                        {
                            _loadSave = true;
                        }
                    }
                    else
                    {
                        _noSaveData = true;
                        _saveLoadError = true;
                    }
                }
                if (_selectedSave == null)
                    ImGui.EndDisabled();
            }
            ImGui.EndPopup();
        }

        public static void DrawSaveErrorsPopup(List<string> modErrors)
        {
            ImGui.SetNextWindowSize(new float2(_windowWidth, -1f), ImGuiCond.Always);
            ImGui.OpenPopup("Custom Start Page Tools - Save Load Error");
            ImGui.BeginPopup("Custom Start Page Tools - Save Load Error", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoResize);
            ImGuiHelper.SetCurrentWindowToCenter();
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
                _loadSave = true;
            }
            ImGui.EndPopup();
        }

        public static void DrawSaveBox(GameSave save, SaveData? saveData)
        {
            BeginBox($"##{save.Id}'s Box", true, regionHeight: -1f);
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
            EndBox();

            if (ImGui.IsItemClicked())
            {
                _selectedSave = save;
                _selectedSaveData = saveData;
            }
        }

        public static void DrawSaveInfoBox()
        {
            BeginBox("Save Info", true, false, regionHeight: 500f);
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
                DrawModHook();
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
            EndBox();
        }

        public static void DrawModHook() { }

        public static void DrawDeleteSaveWarning()
        {
            ImGui.SetNextWindowSize(new float2(_windowWidth, -1f), ImGuiCond.Always);
            ImGui.OpenPopup("Custom Start Page Tools - Delete Save Warning");
            ImGui.BeginPopup("Custom Start Page Tools - Delete Save Warning", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoResize);
            ImGuiHelper.SetCurrentWindowToCenter();
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
                _showWindow = "Singleplayer";
            }
            ImGui.SameLine();
            if (ImGui.Button("Delete", _buttonSize))
            {
                _selectedSave!.Delete();
                _selectedSave = null;
                _selectedSaveData = null;
                _showWindow = "Singleplayer";
            }
            ImGui.EndPopup();
        }

        private static void DrawSettingsMenu()
        {
            ImGui.SetNextWindowSize(new float2(_windowWidth, -1f), ImGuiCond.Always);
            ImGui.OpenPopup("Custom Start Page Tools - Settings");
            ImGui.BeginPopup("Custom Start Page Tools - Settings", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoResize);
            ImGuiHelper.SetCurrentWindowToCenter();
            ImGui.PushFont(default(ImFontPtr), 40);
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("Settings").X) / 2f);
            ImGui.Text("Settings");
            ImGui.PopFont();
            ImGui.Text("Work in Progress");
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - (_buttonSize.X * 3)) / 2f);
            if (ImGui.Button("Back", _buttonSize))
            {
                _showWindow = "Menu";
            }
            ImGui.SameLine();
            if (ImGui.Button("Save", _buttonSize))
            {
                GameSettings.SaveChanges();
            }
            ImGui.SameLine();
            if (ImGui.Button("Save & Restart", _buttonSize))
            {
                GameSettings.SaveChanges();
                StarMapRestarter.RestartStarMap();
            }
            ImGui.EndPopup();
        }

        private static void DrawModsMenu()
        {
            ImGui.SetNextWindowSize(new float2(_windowWidth, -1f), ImGuiCond.Always);
            ImGui.OpenPopup("Custom Start Page Tools - Mods");
            ImGui.BeginPopup("Custom Start Page Tools - Mods", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.Popup | ImGuiWindowFlags.NoResize);
            ImGuiHelper.SetCurrentWindowToCenter();
            ImGui.PushFont(default(ImFontPtr), 40);
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("Mod Manifest").X) / 2f);
            ImGui.Text("Mod Manifest");
            ImGui.PopFont();
            float availableWidth = ImGui.GetContentRegionAvail().X;
            float buttonWidth = availableWidth / 2f;
            float buttonHeight = 30f;
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f);

            if (_showActiveMods)
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
            else
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.Button]);

            if (ImGui.Button("Active Mods", new float2(buttonWidth, buttonHeight)))
                _showActiveMods = true;

            ImGui.PopStyleColor();
            ImGui.SameLine(0f, 0f);

            if (!_showActiveMods)
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
            else
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.Button]);

            if (ImGui.Button("Inactive Mods", new float2(buttonWidth, buttonHeight)))
                _showActiveMods = false;

            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            if (_showActiveMods)
            {
                foreach (ModData modData in CustomStartPageMod.ActiveMods)
                {
                    BeginBox("Active Mods", true, false, regionHeight: 500f);
                    DrawModBox(modData);
                    EndBox();
                }
            }
            else
            {
                foreach (ModData modData in CustomStartPageMod.InactiveMods)
                {
                    BeginBox("Inactive Mods", true, false, regionHeight: 500f);
                    DrawModBox(modData);
                    EndBox();
                }
            }
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - (_buttonSize.X * 3)) / 2f);
            if (ImGui.Button("Back", _buttonSize))
            {
                _showWindow = "Menu";
            }
            ImGui.SameLine();
            if (ImGui.Button("Save", _buttonSize))
            {
                ModLibrary.Manifest.Save();
            }
            ImGui.SameLine();
            if (ImGui.Button("Save & Reload", _buttonSize))
            {
                ModLibrary.Manifest.Save();
                StarMapRestarter.RestartStarMap();
            }
            ImGui.EndPopup();
        }

        public static void DrawModBox(ModData modData, ImFontPtr font = default(ImFontPtr))
        {
            bool isExpanded = _expandedMods.Contains(modData.mod.Id);

            BeginBox($"{modData.mod.Id}'s region", true, regionHeight: -1f);
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);
            ImGui.PushFont(font, 24);
            ImGui.TextWrapped(modData.mod.Id);
            ImGui.PopFont();
            if (!modData.mod.Core)
            {
                ImGui.SameLine();
                float checkboxSize = ImGui.GetFrameHeight();
                float textWidth = ImGui.CalcTextSize("Enabled").X;
                float rightOffset = ImGui.GetWindowWidth() - textWidth - checkboxSize - ImGui.GetStyle().ItemSpacing.X * 2 - ImGui.GetStyle().WindowPadding.X;
                ImGui.SetCursorPosX(rightOffset);
                ImGui.Text("Enabled");
                ImGui.SameLine();
                ImGui.Checkbox($"##{modData.mod.Id}", ref modData.mod.Enabled);
            }
            if (modData.author != null)
            {
                ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);
                ImGui.TextDisabled(modData.author);
            }
            if (modData.version != null)
            {
                ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);
                ImGui.TextDisabled(modData.version);
            }
            if (isExpanded)
            {
                if (modData.description != null)
                {
                    ImGui.Separator();
                    ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);
                    ImGui.TextWrapped(modData.description);
                }
                if(modData.StarMap.ModDependencies.Count > 0)
                {
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();
                    ImGui.SetCursorPosX(ImGui.GetStyle().WindowPadding.X);
                    ImGui.PushFont(font, 20f);
                    ImGui.Text($"Dependencies: {modData.StarMap.ModDependencies.Count}");
                    ImGui.PopFont();
                    ImGui.Spacing();
                    foreach (ModDependency dependency in modData.StarMap.ModDependencies)
                    {
                        ImGui.Text(dependency.ModId);
                        ImGui.TextDisabled(dependency.Optional ? "Optional: true" : "Optional: false");
                        ImGui.Spacing();
                    }
                }
                ImGui.GetWindowDrawList().AddRectFilled(
                    ImGui.GetWindowPos(),
                    ImGui.GetWindowPos() + ImGui.GetWindowSize(),
                    new ImColor8(211, 211, 211, 20)
                );
            }
            EndBox();

            if (ImGui.IsItemClicked())
            {
                if (isExpanded)
                    _expandedMods.Remove(modData.mod.Id);
                else
                    _expandedMods.Add(modData.mod.Id);
            }
        }

        public static void BeginBox(ImString regionName, bool border, bool highlight = true, float regionWidth = float.NaN, float regionHeight = -1f)
        {
            if (float.IsNaN(regionWidth))
            {
                regionWidth = ImGui.GetContentRegionAvail().X;
            }

            ImGuiChildFlags imGuiChildFlags = ImGuiChildFlags.AutoResizeY;
            if (border)
            {
                imGuiChildFlags |= ImGuiChildFlags.Borders;
            }

            ImGuiChildFlags childFlags = imGuiChildFlags;
            ImGui.SetNextWindowSize(new float2(regionWidth, regionHeight), ImGuiCond.Always);
            ImGui.BeginChild(regionName, (float2?)null, childFlags);

            if (highlight)
            {
                bool isHovered = ImGui.IsWindowHovered();
                bool isActive = isHovered && ImGui.IsMouseDown(ImGuiMouseButton.Left);

                if (isActive)
                    ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), new ImColor8(211, 211, 211, 70));
                else if (isHovered)
                    ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), new ImColor8(211, 211, 211, 40));
            }
        }

        public static void EndBox()
        {
            ImGui.EndChild();
        }
    }
}
