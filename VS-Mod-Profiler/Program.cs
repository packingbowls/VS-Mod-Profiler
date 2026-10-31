using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* 
 * Yes, I used AI to help with the project architecture.
 * This is because it is a very useful tool and also because I
 * haven't touched C# in a hot minute. TY for understanding.
 */

namespace VSModProfiler
{
    internal class Program
    {
        private static ConfigManager _configMgr = new();
        private static ModManager _modMgr = null!;

        static void Main(string[] args)
        {
            Console.Title = "Vintage Story Mod Profiler";
            _configMgr.Initialize();
            _modMgr = new ModManager(_configMgr);

            // checks for existing mods
            _modMgr.InspectAndMigrateLooseMods();

            RunHomeMenu();
        }

        // home page
        private static void RunHomeMenu()
        {
            while (true)
            {
                UIManager.DrawHeader(_configMgr.Config, "Home");
                List<string> versions = _modMgr.GetVersions();

                Console.WriteLine("Choose Options:");
                Console.WriteLine("0. Exit");

                for (int i = 0; i < versions.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. Version: {versions[i]}");
                }

                int initNewVerOpt = versions.Count + 1;
                int unloadOpt = versions.Count + 2;
                int openRootOpt = versions.Count + 3;

                Console.WriteLine($"{initNewVerOpt}. Create New Version");
                Console.WriteLine($"{unloadOpt}. Unload Active Profile");
                Console.WriteLine($"{openRootOpt}. Open /VS-Mod-Profiler/ in Explorer");

                int choice = UIManager.PromptMenuChoice(openRootOpt);

                if (choice == 0)
                {
                    break;
                }
                else if (choice >= 1 && choice <= versions.Count)
                {
                    RunVersionMenu(versions[choice - 1]);
                }
                else if (choice == initNewVerOpt)
                {
                    string newVer = UIManager.PromptNonEmptyString("Enter new Vintage Story version (use the latest game version present in your modpack if it contains mods from different versions)");
                    _modMgr.CreateVersion(newVer);
                    UIManager.ShowMessage($"Initialized version directory for {newVer}.", ConsoleColor.Green);
                }
                else if (choice == unloadOpt)
                {
                    UIManager.PlayLoadingAnimation("Unloading active profile");
                    _modMgr.UnloadActiveProfile();
                    UIManager.ShowMessage("Active profile unloaded, /Mods/ folder cleared.", ConsoleColor.Yellow);
                }
                else if (choice == openRootOpt)
                {
                    _modMgr.OpenInExplorer(_configMgr.ProfilerRootPath);
                }
            }
        }

        // version dash
        private static void RunVersionMenu(string version)
        {
            while (true)
            {
                UIManager.DrawHeader(_configMgr.Config, $"Version: {version}");
                Console.WriteLine("Choose Options:");
                Console.WriteLine("0. Cancel / Back");
                Console.WriteLine("1. Profiles");
                Console.WriteLine("2. Open Persisting Mods Folder in Explorer");
                Console.WriteLine("3. Rename / Modify Version");
                Console.WriteLine("4. Delete Version and Profiles");

                int choice = UIManager.PromptMenuChoice(4);

                switch (choice)
                {
                    case 0:
                        return;
                    case 1:
                        RunProfilesListMenu(version);
                        break;
                    case 2:
                        _modMgr.OpenInExplorer(_modMgr.GetPersistPath(version));
                        break;
                    case 3:
                        string newVer = UIManager.PromptNonEmptyString($"Enter new name for version '{version}'");
                        try
                        {
                            _modMgr.RenameVersion(version, newVer);
                            version = newVer;
                            UIManager.ShowMessage($"Version renamed to {newVer}.", ConsoleColor.Green);
                        }
                        catch (Exception ex)
                        {
                            UIManager.ShowMessage($"Error: {ex.Message}", ConsoleColor.Red);
                        }
                        break;
                    case 4:
                        Console.Write($"Are you sure you want to permanently delete version '{version}' and ALL its profiles? (TYPE 'Y' to confirm): ");
                        if (Console.ReadLine()?.Trim() == "Y")
                        {
                            _modMgr.DeleteVersion(version);
                            UIManager.ShowMessage($"Version '{version}' deleted.", ConsoleColor.Yellow);
                            return;
                        }
                        break;
                }
            }
        }

        // profile lists
        private static void RunProfilesListMenu(string version)
        {
            while (true)
            {
                UIManager.DrawHeader(_configMgr.Config, $"Version: {version} -> Profiles");
                List<string> profiles = _modMgr.GetProfiles(version);

                Console.WriteLine("Choose Options:");
                Console.WriteLine("0. Cancel / Back");

                for (int i = 0; i < profiles.Count; i++)
                {
                    bool isActive = _configMgr.Config.ActiveVersion == version &&
                                    _configMgr.Config.ActiveProfile == profiles[i];
                    var meta = _configMgr.GetProfileMeta(version, profiles[i]);

                    string activeTag = isActive ? " [ACTIVE]" : "";
                    string omitTag = meta.OmittedPersistMods.Count > 0 ? $" [{meta.OmittedPersistMods.Count} Persist Omitted]" : "";

                    Console.WriteLine($"{i + 1}. {profiles[i]}{activeTag}{omitTag}");
                }

                int createNewOpt = profiles.Count + 1;
                Console.WriteLine($"{createNewOpt}. Create New Profile");

                int choice = UIManager.PromptMenuChoice(createNewOpt);

                if (choice == 0) return;
                else if (choice >= 1 && choice <= profiles.Count) RunSingleProfileMenu(version, profiles[choice - 1]);
                else if (choice == createNewOpt)
                {
                    string newProf = UIManager.PromptNonEmptyString("Enter new profile name");
                    _modMgr.CreateProfile(version, newProf);
                    UIManager.ShowMessage($"Created profile '{newProf}'.", ConsoleColor.Green);
                }
            }
        }

        // profile menu
        private static void RunSingleProfileMenu(string version, string profile)
        {
            while (true)
            {
                bool isActive = _configMgr.Config.ActiveVersion == version &&
                                _configMgr.Config.ActiveProfile == profile;
                var meta = _configMgr.GetProfileMeta(version, profile);

                UIManager.DrawHeader(_configMgr.Config, $"Profile: {profile} ({version})");
                Console.WriteLine($"Individually Omitted Persist Mods: {meta.OmittedPersistMods.Count}\n");

                Console.WriteLine("Choose Options:");
                Console.WriteLine("0. Cancel / Back");
                Console.WriteLine("1. Open Instance in Explorer");
                Console.WriteLine(isActive ? "2. Unload Profile (Currently Active)" : "2. Load Profile");
                Console.WriteLine("3. Modify Profile");

                int choice = UIManager.PromptMenuChoice(3);

                switch (choice)
                {
                    case 0: return;
                    case 1: _modMgr.OpenInExplorer(_modMgr.GetProfilePath(version, profile)); break;
                    case 2:
                        if (isActive)
                        {
                            UIManager.PlayLoadingAnimation($"Unloading {profile}");
                            _modMgr.UnloadActiveProfile();
                            UIManager.ShowMessage($"Unloaded '{profile}'. Don't forget to reload mods in the VS Mod Menu.", ConsoleColor.Yellow);
                        }
                        else
                        {
                            UIManager.PlayLoadingAnimation($"Loading {profile}");
                            bool ok = _modMgr.LoadProfile(version, profile, out string msg);
                            UIManager.ShowMessage(msg, ok ? ConsoleColor.Green : ConsoleColor.Red);
                        }
                        break;
                    case 3:
                        bool deletedOrRenamed = RunModifyProfileMenu(version, ref profile);
                        if (deletedOrRenamed) return;
                        break;
                }
            }
        }

        // modify profile menu
        private static bool RunModifyProfileMenu(string version, ref string profile)
        {
            while (true)
            {
                var meta = _configMgr.GetProfileMeta(version, profile);
                UIManager.DrawHeader(_configMgr.Config, $"Modify Profile: {profile}");

                Console.WriteLine("Choose Options:");
                Console.WriteLine("0. Cancel / Back");
                Console.WriteLine("1. Rename Profile");
                Console.WriteLine($"2. Manage Persist Mods ({meta.OmittedPersistMods.Count} currently omitted)");
                Console.WriteLine("3. Delete Profile");

                int choice = UIManager.PromptMenuChoice(3);

                switch (choice)
                {
                    case 0: return false;
                    case 1:
                        string newName = UIManager.PromptNonEmptyString($"Enter new name for '{profile}'");
                        try
                        {
                            _modMgr.RenameProfile(version, profile, newName);
                            profile = newName;
                            UIManager.ShowMessage($"Profile renamed to '{newName}'.", ConsoleColor.Green);
                        }
                        catch (Exception ex)
                        {
                            UIManager.ShowMessage($"Error: {ex.Message}", ConsoleColor.Red);
                        }
                        break;
                    case 2:
                        RunManagePersistModsMenu(version, profile);
                        break;
                    case 3:
                        Console.Write($"Delete profile '{profile}' and all mods inside it? (Y/N): ");
                        if (Console.ReadLine()?.Trim().ToUpperInvariant() == "Y")
                        {
                            _modMgr.DeleteProfile(version, profile);
                            UIManager.ShowMessage($"Deleted profile '{profile}'.", ConsoleColor.Yellow);
                            return true;
                        }
                        break;
                }
            }
        }

        // persist mod toggle
        private static void RunManagePersistModsMenu(string version, string profile)
        {
            while (true)
            {
                var meta = _configMgr.GetProfileMeta(version, profile);
                UIManager.DrawHeader(_configMgr.Config, $"Manage Persist Mods: {profile}");

                string persistDir = _modMgr.GetPersistPath(version);
                List<string> persistMods = new List<string>();

                if (System.IO.Directory.Exists(persistDir))
                {
                    persistMods = System.IO.Directory.GetFiles(persistDir)
                        .Select(System.IO.Path.GetFileName)
                        .Where(n => !string.IsNullOrEmpty(n))
                        .Cast<string>()
                        .OrderBy(n => n)
                        .ToList();
                }

                if (persistMods.Count == 0)
                {
                    Console.WriteLine("No mods found in this version's /Persist/ folder.");
                    Console.WriteLine("0. Back");
                    UIManager.PromptMenuChoice(0);
                    return;
                }

                Console.WriteLine("Select a mod to toggle its inclusion, or 0 to go back:\n");
                Console.WriteLine("0. Back");

                for (int i = 0; i < persistMods.Count; i++)
                {
                    string modName = persistMods[i];
                    bool isOmitted = meta.OmittedPersistMods.Exists(m => m.Equals(modName, StringComparison.OrdinalIgnoreCase));

                    Console.Write($"{i + 1}. ");
                    if (isOmitted)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("[OMITTED]  ");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("[INCLUDED] ");
                    }
                    Console.ResetColor();
                    Console.WriteLine(modName);
                }

                int choice = UIManager.PromptMenuChoice(persistMods.Count);

                if (choice == 0)
                {
                    // hotswap symlinks if profile is currently active so changes take effect immediately
                    if (_configMgr.Config.ActiveVersion == version && _configMgr.Config.ActiveProfile == profile)
                    {
                        UIManager.PlayLoadingAnimation("Refreshing active symlinks");
                        _modMgr.LoadProfile(version, profile, out _);
                    }
                    return;
                }

                string selectedMod = persistMods[choice - 1];
                bool currentlyOmitted = meta.OmittedPersistMods.Exists(m => m.Equals(selectedMod, StringComparison.OrdinalIgnoreCase));

                if (currentlyOmitted)
                {
                    meta.OmittedPersistMods.RemoveAll(m => m.Equals(selectedMod, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    meta.OmittedPersistMods.Add(selectedMod);
                }

                _configMgr.Save();
            }
        }
    }
}