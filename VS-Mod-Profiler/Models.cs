using System.Collections.Generic;

namespace VSModProfiler
{
    public class AppConfig
    {
        public string AppVersion { get; set; } = "1.0.0";
        public string VsDataPath { get; set; } = string.Empty;
        public string? ActiveVersion { get; set; } = null;
        public string? ActiveProfile { get; set; } = null;

        public Dictionary<string, ProfileMetadata> ProfileSettings { get; set; } = new();
    }

    public class ProfileMetadata
    {
        // tracks omitted mods
        public List<string> OmittedPersistMods { get; set; } = new();
    }
}