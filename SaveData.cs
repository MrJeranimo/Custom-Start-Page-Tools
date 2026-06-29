using Brutal.Logging;
using KSA;
using Tomlet;
using Tomlet.Attributes;

namespace Custom_Start_Page_Tools
{
    public struct SaveModData
    {
        [TomlField("name")]
        public string Name;
        [TomlField("version")]
        public string? Version;

        public SaveModData(string name, string? version = null)
        {
            this.Name = name;
            this.Version = version;
        }

        public bool Equals(SaveModData other)
        {
            if (other.Name != null && other.Version != null && this.Version != null &&  other.Name == this.Name && other.Version == this.Version)
                return true;
            return false;
        }

        public bool NameCheck(SaveModData other)
        {
            if (other.Name != null && other.Name == this.Name)
                return true;
            return false;
        }

        public bool VersionCheck(SaveModData other)
        {
            if (other.Version != null && this.Version != null && other.Version == this.Version)
                return true;
            return false;
        }
    }

    public class SaveData
    {
        [TomlNonSerialized]
        public static string fileName { get; } = "CSPTData.toml";
        [TomlField("GameSaveName")]
        public string GameSaveName = String.Empty;
        [TomlField("SystemInfoName")]
        public string SystemInfoName = String.Empty;
        [TomlField("GameType")]
        public GameSettings.GameType GameType;
        [TomlField("SimTime")]
        public double SimTime;
        [TomlField("VehicleCount")]
        public int VehicleCount = 0;
        [TomlField("LastControlledVehicle")]
        public string? LastControlledVehicle;
        [TomlField("LastControlledVehicleParent")]
        public string? LastControlledVehicleParent;
        [TomlField("Mods")]
        public List<SaveModData> Mods = new List<SaveModData>();

        public SaveData() { }

        public SaveData(string saveName)
        {
            GameSaveName = saveName;
            SystemInfoName = Universe.CurrentSystem!.Id;
            GameType = GameSettings.Current.System.StartGameType;
            SimTime = Universe.GetElapsedSimTime().Seconds();
            foreach (Vehicle vehicle in Universe.CurrentSystem.All.OfType<Vehicle>())
            {
                VehicleCount++;
            }
            LastControlledVehicle = Program.ControlledVehicle?.Id;
            LastControlledVehicleParent = Program.ControlledVehicle?.Parent.Id;
            foreach (ModData mod in CustomStartPageMod.ActiveMods)
            {
                Mods.Add(new SaveModData(mod.mod.Id, mod?.version));
            }
        }

        public void Save(string path)
        {
            string filePath = Path.Combine(path, fileName);
            File.WriteAllText(filePath, TomletMain.TomlStringFrom<SaveData>(this));
        }

        public static SaveData? Load(string? path)
        {
            if (path == null)
            {
                DefaultCategory.Log.Error("Path is null");
                return null;
            }
            string filePath = Path.Combine(path, fileName);
            if (!File.Exists(filePath))
            {
                return null;
            }
            else
            {
                string tomlContent = File.ReadAllText(filePath);
                SaveData result = TomletMain.To<SaveData>(new TomlParser().Parse(tomlContent));
                return result;
            }
        }

        public List<string> CheckMods()
        {
            List<string> found = new List<string>();
            List<string> errors = new List<string>();
            foreach (SaveModData saveModData in this.Mods)
            {
                if (saveModData.Name == "Core")
                {
                    found.Add(saveModData.Name);
                    continue;
                }
                string error = string.Empty;
                ModData? mod = CustomStartPageMod.ActiveMods.Find(a => a.mod.Id == saveModData.Name);
                if (mod == null)
                {
                    error = $"Mod {saveModData.Name} was not found";
                    errors.Add(error);
                    continue;
                }
                else
                {
                    found.Add(mod.mod.Id);
                }
                if (!saveModData.VersionCheck(new SaveModData(mod.mod.Id, mod.version)))
                {
                    error = $"Mod {saveModData.Name} expected {saveModData.Version}; {saveModData.Name}'s currently loaded version is {mod.version}";
                    errors.Add(error);
                }
            }
            foreach (ModData mod in CustomStartPageMod.ActiveMods)
            {
                string error = string.Empty;
                if (!found.Contains(mod.mod.Id))
                {
                    error = $"Mod {mod.mod.Id} is not listed in the save's meta data.";
                    errors.Add(error);
                }
            }
            return errors;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;
            if (obj.GetType() != typeof(SaveData))
                return false;
            SaveData otherSave = (SaveData)obj;
            if (otherSave.GameSaveName == this.GameSaveName && otherSave.GameType == this.GameType && otherSave.SystemInfoName == this.SystemInfoName && otherSave.Mods == this.Mods && otherSave.SimTime == this.SimTime)
            {
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
