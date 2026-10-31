using System;
using System.IO;
using System.Text.Json;

namespace VSModProfiler
{
    public class ConfigManager
    {
        public AppConfig Config { get; private set; } = new();

        public string VsDataPath => Config.VsDataPath;
        public string ModsPath => Path.Combine(VsDataPath, "Mods");
        public string ProfilerRootPath => Path.Combine(VsDataPath, "VS-Mod-Profiler");
        public string SettingsDirPath => Path.Combine(ProfilerRootPath, "Settings");
        public string ConfigFilePath => Path.Combine(SettingsDirPath, "config.json");
        public string VersionsRootPath => Path.Combine(ProfilerRootPath, "Versions");

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public void Initialize()
        {
            string defaultVsData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VintagestoryData"
            );

            string defaultConfigFile = Path.Combine(defaultVsData, "VS-Mod-Profiler", "Settings", "config.json");

            // check if an existing config already lives in the default install path
            if (File.Exists(defaultConfigFile))
            {
                LoadFromPath(defaultConfigFile);
                EnsureDirectories();
                return;
            }

            // check if a local pointer file exists next to the .exe when user chose a custom dir
            string localPointer = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vs_path.txt");
            if (File.Exists(localPointer))
            {
                string customDataPath = File.ReadAllText(localPointer).Trim();
                string customConfigFile = Path.Combine(customDataPath, "VS-Mod-Profiler", "Settings", "config.json");
                if (File.Exists(customConfigFile))
                {
                    LoadFromPath(customConfigFile);
                    EnsureDirectories();
                    return;
                }
            }

            // first-time run wizard
            RunFirstTimeSetup(defaultVsData, localPointer);
        }

        private void RunFirstTimeSetup(string defaultVsData, string localPointerPath)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("======= VS Mod Profiler Wizard =======");
            Console.ResetColor();

            if (Directory.Exists(defaultVsData))
            {
                Console.WriteLine($"Detected Vintage Story data folder at:\n  {defaultVsData}");
                Console.Write("\nUse this directory? (Y/N): ");
                string? resp = Console.ReadLine()?.Trim().ToUpperInvariant();
                if (resp == "Y" || resp == "")
                {
                    Config.VsDataPath = defaultVsData;
                }
            }

            while (string.IsNullOrEmpty(Config.VsDataPath))
            {
                Console.WriteLine("\nPaste the path to your VintagestoryData (or VintagestoryData\\Mods) folder:");
                string? inputPath = Console.ReadLine()?.Trim().Trim('"');

                if (string.IsNullOrWhiteSpace(inputPath)) continue;

                // normalize if the user pasted the /Mods subdirectory directly
                if (inputPath.EndsWith("Mods", StringComparison.OrdinalIgnoreCase) ||
                    inputPath.EndsWith("Mods\\", StringComparison.OrdinalIgnoreCase) ||
                    inputPath.EndsWith("Mods/", StringComparison.OrdinalIgnoreCase))
                {
                    inputPath = Path.GetDirectoryName(inputPath.TrimEnd('\\', '/')) ?? inputPath;
                }

                if (Directory.Exists(inputPath))
                {
                    Config.VsDataPath = inputPath;
                }
                else
                {
                    Console.Write($"Directory '{inputPath}' does not exist. Create it now? (Y/N): ");
                    if (Console.ReadLine()?.Trim().ToUpperInvariant() == "Y")
                    {
                        Directory.CreateDirectory(inputPath);
                        Config.VsDataPath = inputPath;
                    }
                }
            }

            if (!string.Equals(Config.VsDataPath, defaultVsData, StringComparison.OrdinalIgnoreCase))
            {
                File.WriteAllText(localPointerPath, Config.VsDataPath);
            }

            EnsureDirectories();
            Save();
        }

        private void LoadFromPath(string configFile)
        {
            string json = File.ReadAllText(configFile);
            Config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }

        public void EnsureDirectories()
        {
            Directory.CreateDirectory(ModsPath);
            Directory.CreateDirectory(SettingsDirPath);
            Directory.CreateDirectory(VersionsRootPath);
        }

        public void Save()
        {
            EnsureDirectories();
            string json = JsonSerializer.Serialize(Config, JsonOptions);
            File.WriteAllText(ConfigFilePath, json);
        }

        public ProfileMetadata GetProfileMeta(string version, string profile)
        {
            string key = $"{version}/{profile}";
            if (!Config.ProfileSettings.TryGetValue(key, out var meta))
            {
                meta = new ProfileMetadata();
                Config.ProfileSettings[key] = meta;
            }
            return meta;
        }
    }
}