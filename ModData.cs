using KSA;
using Tomlet;
using Tomlet.Attributes;

namespace Custom_Start_Page_Tools
{
    [TomlDoNotInlineObject]
    public class StarMapData
    {
        [TomlField("ModDependencies")]
        public List<ModDependency> ModDependencies = new List<ModDependency>();
    }

    public class ModDependency
    {
        [TomlField("ModId")]
        public string ModId = string.Empty;

        [TomlField("Optional")]
        public bool Optional = false;

        [TomlField("ImportedAssemblies")]
        public List<string> ImportedAssemblies = new List<string>();
    }

    [TomlDoNotInlineObject]
    public class ModData
    {
        [TomlNonSerialized]
        public ModEntry mod;
        [TomlNonSerialized]
        public string? path;
        [TomlField("author")]
        public string? author;
        [TomlField("version")]
        public string? version;
        [TomlField("description")]
        public string? description;
        [TomlField("StarMap")]
        public StarMapData StarMap = new StarMapData();

        public ModData(ModEntry mod, string? path = null, string? author = null, string? version = null, string? description = null)
        {
            this.mod = mod;
            this.path = path;
            this.author = author;
            this.version = version;
            this.description = description;
        }

        public static ModData CreateModData(ModEntry mod)
        {
            if (mod.Core)
                return new ModData(mod, null, "RocketWerkz", VersionInfo.Current.VersionString, "This is the KSA base content.");
            string modPath = System.IO.Path.Combine(ModLibrary.LocalModsFolderPath, mod.Id, "mod.toml");
            if (!File.Exists(modPath))
                return new ModData(mod);
            string tomlContent = File.ReadAllText(modPath);
            ModData result = TomletMain.To<ModData>(new TomlParser().Parse(tomlContent));
            result.mod = mod;
            result.path = modPath;
            return result;
        }
    }
}
