using KSA;
using Brutal.ImGuiApi;
using Brutal.Numerics;

namespace Custom_Start_Page_Tools
{
    public class SavesMenuUi
    {
        public static float2 _buttonSize = new float2(150f, 30f);
        public static float _windowWidth = float.Clamp(float2.Unpack(Program.GetWindow().Size).X * (0.65f / GameSettings.GetInterfaceScale()), 400f, 800f);
        public GameSave? SelectedSave;
        public static LookupCollection<GameSave> GameSaves = new LookupCollection<GameSave>("temp");
        public SaveData? SelectedSaveData;
        public static Dictionary<string, SaveData> GameSavesData = new Dictionary<string, SaveData>();
        private ImInputString _saveName = new ImInputString(255, "");
        public bool SaveLoadError = false;
        public bool NoSaveData = false;
        public List<string> ModErrors = new List<string>();
        public bool LoadSave = false;
        public bool NewSave = false;
        public bool Initalized = false;
        public bool DeleteSave = false;
        public string ShowWindow = string.Empty;
        public static bool GameStarted = false;

        public SavesMenuUi(ref string showWindow)
        {
            ShowWindow = showWindow;
        }

        public SavesMenuUi(ref bool initalized)
        {
            Initalized = initalized;
        }

        public SavesMenuUi(ref string showWindow, ref bool initalized)
        {
            ShowWindow = showWindow;
            Initalized = initalized;
        }

        public void Draw()
        {
            if (SelectedSave != null)
            {
                ImGui.Columns(2, "Save Cloumns", false);
                float windowWidth = ImGui.GetWindowWidth();
                ImGui.SetColumnWidth(0, windowWidth * 0.4f);
            }
            BeginBox("Select Save", true, false, regionHeight: 500f);
            foreach (GameSave save in GameSaves.AsSpan())
            {
                DrawSaveBox(save, GameSavesData[save.Id]);
            }
            EndBox();
            if (SelectedSave != null)
            {
                ImGui.NextColumn();
                DrawSaveInfoBox();
                ImGui.Columns();
            }
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - (_buttonSize.X * (GameStarted ? 4 : 3))) / 2f);
            if (!GameStarted)
            {
                if (ImGui.Button("Back", _buttonSize))
                {
                    ShowWindow = "Menu";
                    SelectedSave = null;
                    SelectedSaveData = null;
                }
                ImGui.SameLine();
            }
            if (GameStarted)
            {
                if (ImGui.Button("New Save", _buttonSize))
                {
                    NewSave = true;
                }
                ImGui.SameLine();
            }
            if (SelectedSave == null)
                ImGui.BeginDisabled();
            if (ImGui.Button("Delete Save", _buttonSize))
            {
                DeleteSave = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("Load Save ##Button", _buttonSize))
            {
                if (SelectedSaveData != null)
                {
                    NoSaveData = false;
                    ModErrors = SelectedSaveData.CheckMods();
                    if (ModErrors.Count > 0 || SelectedSave!.Version != VersionInfo.Current.VersionString)
                    {
                        SaveLoadError = true;
                    }
                    else
                    {
                        LoadSave = true;
                    }
                }
                else
                {
                    NoSaveData = true;
                    SaveLoadError = true;
                }
            }
            if (GameStarted)
            {
                ImGui.SameLine();
                if (ImGui.Button("Overwrite Save", _buttonSize))
                {
                    KSA.GameSaves.MakeSave(SelectedSave!.Id);
                }
                ImGui.SameLine();
            }
            if (SelectedSave == null)
                ImGui.EndDisabled();
        }

        public void DrawSaveBox(GameSave save, SaveData? saveData)
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
            if (save == SelectedSave)
            {
                ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), new ImColor8(211, 211, 211, 20));
            }
            EndBox();

            if (ImGui.IsItemClicked())
            {
                SelectedSave = save;
                SelectedSaveData = saveData;
            }
        }

        public void DrawSaveInfoBox()
        {
            BeginBox("Save Info", true, false, regionHeight: 500f);
            ImGui.PushFont(default(ImFontPtr), 32);
            ImGui.TextWrapped(SelectedSave!.Id);
            ImGui.PopFont();
            if (SelectedSaveData != null)
                ImGui.TextColored(new ImColor8(144, 238, 144, 255).AsFloat4(), SelectedSaveData.GameType.ToString()!);
            ImGui.BeginDisabled();
            ImGui.TextWrapped($"KSA Version: {SelectedSave.Version}");
            ImGui.EndDisabled();
            if (SelectedSaveData != null && SelectedSaveData.Mods.Count > 1)
            {
                ImGui.TextDisabled("Modded");
            }
            if (SelectedSaveData != null)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                SimTime simTime = new SimTime(SelectedSaveData.SimTime);
                string timeString = $"{simTime.ValueIn(TimeUnit.Years):F0} Years, {simTime.ValueIn(TimeUnit.Days) % 365:F0} Days, {simTime.ValueIn(TimeUnit.Hours) % 24:F0} Hours";
                ImGui.TextWrapped(timeString);
                ImGui.TextWrapped("Number of Vehicles:");
                ImGui.SameLine();
                ImGui.BeginDisabled();
                ImGui.TextWrapped($"{SelectedSaveData.VehicleCount}");
                ImGui.EndDisabled();
                if (SelectedSaveData.LastControlledVehicle != null)
                {
                    ImGui.TextWrapped("Last Controlled Vehicle:");
                    ImGui.SameLine();
                    ImGui.BeginDisabled();
                    ImGui.TextWrapped($"{SelectedSaveData.LastControlledVehicle}");
                    ImGui.EndDisabled();
                    ImGui.TextWrapped("Parent Body:");
                    ImGui.SameLine();
                    ImGui.BeginDisabled();
                    ImGui.TextWrapped($"{SelectedSaveData.LastControlledVehicleParent!}");
                    ImGui.EndDisabled();
                }
                DrawModHook();
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.PushFont(default(ImFontPtr), 24);
                ImGui.Text("Mods:");
                ImGui.PopFont();
                foreach (SaveModData mod in SelectedSaveData.Mods)
                {
                    string modString = $"{mod.Name} - {(mod.Version ?? "")}";
                    ImGui.TextWrapped(modString);
                }
            }
            EndBox();
        }

        public void DrawModHook() { }

        public void DrawDeleteSaveWarning()
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
            DrawSaveBox(SelectedSave!, SelectedSaveData);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - (_buttonSize.X * 2)) / 2f);
            if (ImGui.Button("Cancel", _buttonSize))
            {
                ShowWindow = "Singleplayer";
            }
            ImGui.SameLine();
            if (ImGui.Button("Delete", _buttonSize))
            {
                SelectedSave!.Delete();
                SelectedSave = null;
                SelectedSaveData = null;
                ShowWindow = "Singleplayer";
            }
            ImGui.EndPopup();
        }

        public void DrawSaveErrorsPopup(List<string> modErrors)
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
            if (SelectedSave!.Version != VersionInfo.Current.VersionString)
            {
                ImGui.Text("KSA Game Versions do not match:");
                ImGui.TextDisabled($"Current Version: {VersionInfo.Current.VersionString}\nSave's Version: {SelectedSave.Version}");
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
            }
            if (NoSaveData)
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
                SaveLoadError = false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Load Anyway", _buttonSize))
            {
                LoadSave = true;
            }
            ImGui.EndPopup();
        }

        public void DrawNewSaveWindow()
        {
            ImGui.SetNextWindowSize(new float2(float.Clamp(float2.Unpack(Program.GetWindow().Size).X * (0.65f / GameSettings.GetInterfaceScale()), 400f, 600f), -1f), ImGuiCond.Appearing);
            if (ImGui.Begin("New Save", ref NewSave, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
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
                    NewSave = false;
                }
                ImGui.SameLine();
                if (ImGui.Button("Save", _buttonSize))
                {
                    KSA.GameSaves.MakeSave(_saveName.Value.ToString());
                    NewSave = false;
                    Initalized = false;
                }
            }
            ImGui.End();
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
