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
        public static string ShowWindow = "Menu";
        private static bool _showActiveMods = true;
        private static bool _newGame = true;
        private static HashSet<string> _expandedMods = new HashSet<string>();
        private static bool _startGame = false;
        private static ConfigOnStartPopup? _instance;
        private static Traverse? _traverse;
        private static SystemInfo? _selectedSystem;
        private static GameTypeObject _selectedGameType;
        private static List<GameTypeObject> _gameTypes = new List<GameTypeObject>();
        private static ConfigOnStartPopup.VehicleObject _selectedVehicle;
        private static List<ConfigOnStartPopup.VehicleObject> _vehicles = new List<ConfigOnStartPopup.VehicleObject>();
        private static LocationObject _selectedLocation;
        private static List<LocationObject> _locations = new List<LocationObject>();
        public static SavesMenuUi SavesUi = new SavesMenuUi(ref ShowWindow);

        public static bool StartGame => _startGame;
        public static bool NewGame => _newGame;

        [HarmonyPatch(typeof(ConfigOnStartPopup), "OnDrawUi")]
        [HarmonyPrefix]
        public static bool ConfigStartPopupPatch(ConfigOnStartPopup __instance)
        {
            if (SavesUi.LoadSave)
            {
                if (SavesUi.SelectedSaveData != null)
                {
                    _selectedSystem = SelectSystem.Systems.Find(system => system.Id == SavesUi.SelectedSaveData.SystemInfoName);
                    if (_selectedSystem != null)
                    {
                        SystemLibrary.Default = _selectedSystem;
                        GameSettings.Current.System.LastSystemId = _selectedSystem.Id;
                        _traverse!.Method("SetVehicles").GetValue();
                        _traverse!.Method("SetStartingVehicleLocation").GetValue();
                        GameSettings.Current.Save();
                    }
                    GameSettings.Current.System.StartGameType = SavesUi.SelectedSaveData.GameType;
                }
                __instance.Active = false;
                SavesMenuUi.GameStarted = true;
                return false;
            }

            if (_startGame)
            {
                __instance.Active = false;
                SavesMenuUi.GameStarted = true;
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
                SavesMenuUi.GameSaves = Traverse.CreateWithType("KSA.GameSaves").Property("Saves").GetValue<LookupCollection<GameSave>>();
                foreach (GameSave save in SavesMenuUi.GameSaves.AsSpan())
                {
                    SavesMenuUi.GameSavesData.Add(save.Id, SaveData.Load(((UncompressedSave)save).Directory.FullName)!);
                }
            }

            switch (ShowWindow)
            {
                case "Menu":
                    break;
                case "Singleplayer":
                    DrawSingleplayerMenu();
                    return false;
                case "Delete Save":
                    SavesUi.DrawDeleteSaveWarning();
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
                ShowWindow = "Singleplayer";
            }
            ImGui.BeginDisabled();
            if (ImGui.Button("Multiplayer", _buttonSize))
            {
                ShowWindow = "Multiplayer";
            }
            ImGui.EndDisabled();
            if (ImGui.Button("Settings", _buttonSize))
            {
                ShowWindow = "Settings";
            }
            if (ImGui.Button("Mods", _buttonSize))
            {
                ShowWindow = "Mods";
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
            if (SavesUi.SaveLoadError)
            {
                SavesUi.DrawSaveErrorsPopup(SavesUi.ModErrors);
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
                    ShowWindow = "Menu";
                }
                ImGui.SameLine();
                if (ImGui.Button("Start Game", _buttonSize))
                {
                    _startGame = true;
                }
            }
            else
            {
                SavesUi.Draw();
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
                ShowWindow = "Menu";
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
                ShowWindow = "Menu";
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
