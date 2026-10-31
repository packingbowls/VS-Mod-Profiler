using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace VSModProfiler
{
    public class ModManager
    {
        private readonly ConfigManager _configMgr;

        public ModManager(ConfigManager configMgr)
        {
            _configMgr = configMgr;
        }

        // Checks if existing real (non-symlink) mods are sitting in /Mods/ on startup
        public void InspectAndMigrateLooseMods()
        {
            var looseMods = GetRealModFilesInModsFolder();
            if (looseMods.Count == 0) return;

            // If a profile is already active, absorb newly dropped real files into that profile and convert to symlinks
            if (!string.IsNullOrEmpty(_configMgr.Config.ActiveVersion) &&
                !string.IsNullOrEmpty(_configMgr.Config.ActiveProfile))
            {
                string activeProfDir = GetProfilePath(_configMgr.Config.ActiveVersion, _configMgr.Config.ActiveProfile);
                if (Directory.Exists(activeProfDir))
                {
                    foreach (var file in looseMods)
                    {
                        string dest = Path.Combine(activeProfDir, Path.GetFileName(file));
                        File.Move(file, dest, true);
                        File.CreateSymbolicLink(file, dest);
                    }
                    return;
                }
            }

            // Otherwise, we have unorganized mods from before VS-Mod-Profiler was installed
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Found {looseMods.Count} existing unorganized mod(s) in your /Mods/ folder.");
            Console.ResetColor();
            Console.WriteLine("Let's migrate them into an initial Game Version and Profile so nothing is lost.\n");

            string version = UIManager.PromptNonEmptyString("Enter Game Version for these mods (e.g., 1.22.7)");
            string profile = UIManager.PromptNonEmptyString("Enter Profile Name for these mods (e.g., Default)");

            CreateVersion(version);
            CreateProfile(version, profile);

            string targetProfileDir = GetProfilePath(version, profile);
            foreach (string modPath in looseMods)
            {
                string fileName = Path.GetFileName(modPath);
                string destPath = Path.Combine(targetProfileDir, fileName);
                File.Move(modPath, destPath, true);
            }

            LoadProfile(version, profile, out _);
            UIManager.ShowMessage($"Migrated {looseMods.Count} mod(s) into {version} -> {profile} and linked them active.", ConsoleColor.Green);
        }

        public List<string> GetRealModFilesInModsFolder()
        {
            if (!Directory.Exists(_configMgr.ModsPath)) return new List<string>();

            var result = new List<string>();
            foreach (var file in Directory.GetFiles(_configMgr.ModsPath))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext != ".zip" && ext != ".cs" && ext != ".dll") continue;

                FileInfo fi = new(file);
                bool isSymlink = fi.LinkTarget != null || fi.Attributes.HasFlag(FileAttributes.ReparsePoint);
                if (!isSymlink)
                {
                    result.Add(file);
                }
            }
            return result;
        }

        public List<string> GetVersions()
        {
            if (!Directory.Exists(_configMgr.VersionsRootPath)) return new List<string>();
            return Directory.GetDirectories(_configMgr.VersionsRootPath)
                            .Select(Path.GetFileName)
                            .Where(n => !string.IsNullOrEmpty(n))
                            .Cast<string>()
                            .OrderBy(n => n)
                            .ToList();
        }

        public List<string> GetProfiles(string version)
        {
            string profilesRoot = Path.Combine(_configMgr.VersionsRootPath, version, "Profiles");
            if (!Directory.Exists(profilesRoot)) return new List<string>();
            return Directory.GetDirectories(profilesRoot)
                            .Select(Path.GetFileName)
                            .Where(n => !string.IsNullOrEmpty(n))
                            .Cast<string>()
                            .OrderBy(n => n)
                            .ToList();
        }

        public string GetVersionPath(string version) => Path.Combine(_configMgr.VersionsRootPath, version);
        public string GetPersistPath(string version) => Path.Combine(GetVersionPath(version), "Persist");
        public string GetProfilePath(string version, string profile) => Path.Combine(GetVersionPath(version), "Profiles", profile);

        public void CreateVersion(string version)
        {
            Directory.CreateDirectory(GetPersistPath(version));
            Directory.CreateDirectory(Path.Combine(GetVersionPath(version), "Profiles"));
        }

        public void RenameVersion(string oldVersion, string newVersion)
        {
            string oldPath = GetVersionPath(oldVersion);
            string newPath = GetVersionPath(newVersion);
            if (Directory.Exists(newPath)) throw new IOException("A version with that name already exists.");

            bool wasActive = _configMgr.Config.ActiveVersion == oldVersion;
            string? activeProf = _configMgr.Config.ActiveProfile;

            if (wasActive) UnloadActiveProfile();

            Directory.Move(oldPath, newPath);

            // Update metadata keys
            var keysToUpdate = _configMgr.Config.ProfileSettings.Keys
                .Where(k => k.StartsWith(oldVersion + "/"))
                .ToList();

            foreach (var oldKey in keysToUpdate)
            {
                var meta = _configMgr.Config.ProfileSettings[oldKey];
                _configMgr.Config.ProfileSettings.Remove(oldKey);
                string newKey = newVersion + oldKey.Substring(oldVersion.Length);
                _configMgr.Config.ProfileSettings[newKey] = meta;
            }

            if (wasActive && activeProf != null)
            {
                LoadProfile(newVersion, activeProf, out _);
            }
            _configMgr.Save();
        }

        public void DeleteVersion(string version)
        {
            if (_configMgr.Config.ActiveVersion == version)
            {
                UnloadActiveProfile();
            }

            string path = GetVersionPath(version);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            var keysToRemove = _configMgr.Config.ProfileSettings.Keys
                .Where(k => k.StartsWith(version + "/"))
                .ToList();
            foreach (var k in keysToRemove) _configMgr.Config.ProfileSettings.Remove(k);

            _configMgr.Save();
        }

        public void CreateProfile(string version, string profile)
        {
            Directory.CreateDirectory(GetProfilePath(version, profile));
            _configMgr.GetProfileMeta(version, profile);
            _configMgr.Save();
        }

        public void RenameProfile(string version, string oldProfile, string newProfile)
        {
            string oldPath = GetProfilePath(version, oldProfile);
            string newPath = GetProfilePath(version, newProfile);
            if (Directory.Exists(newPath)) throw new IOException("A profile with that name already exists.");

            bool wasActive = _configMgr.Config.ActiveVersion == version && _configMgr.Config.ActiveProfile == oldProfile;
            if (wasActive) UnloadActiveProfile();

            Directory.Move(oldPath, newPath);

            string oldKey = $"{version}/{oldProfile}";
            string newKey = $"{version}/{newProfile}";
            if (_configMgr.Config.ProfileSettings.TryGetValue(oldKey, out var meta))
            {
                _configMgr.Config.ProfileSettings.Remove(oldKey);
                _configMgr.Config.ProfileSettings[newKey] = meta;
            }

            if (wasActive)
            {
                LoadProfile(version, newProfile, out _);
            }
            _configMgr.Save();
        }

        public void DeleteProfile(string version, string profile)
        {
            if (_configMgr.Config.ActiveVersion == version && _configMgr.Config.ActiveProfile == profile)
            {
                UnloadActiveProfile();
            }

            string path = GetProfilePath(version, profile);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            _configMgr.Config.ProfileSettings.Remove($"{version}/{profile}");
            _configMgr.Save();
        }

        public bool LoadProfile(string version, string profile, out string statusMessage)
        {
            try
            {
                // safeguard any real files dropped into /Mods/ while the profile was active
                InspectAndMigrateLooseMods();

                ClearModsSymlinks();

                string profileDir = GetProfilePath(version, profile);
                string persistDir = GetPersistPath(version);
                var meta = _configMgr.GetProfileMeta(version, profile);

                int profileModCount = 0;
                int persistModCount = 0;

                // symlink Profile Mods
                if (Directory.Exists(profileDir))
                {
                    foreach (var file in Directory.GetFiles(profileDir))
                    {
                        string linkPath = Path.Combine(_configMgr.ModsPath, Path.GetFileName(file));
                        File.CreateSymbolicLink(linkPath, file);
                        profileModCount++;
                    }
                }

                // symlink persisting mods skipping individually omitted mods
                if (Directory.Exists(persistDir))
                {
                    foreach (var file in Directory.GetFiles(persistDir))
                    {
                        string fileName = Path.GetFileName(file);

                        // check if mod is omitted for this profile
                        bool isOmitted = meta.OmittedPersistMods.Exists(m => m.Equals(fileName, StringComparison.OrdinalIgnoreCase));
                        if (isOmitted) continue;

                        string linkPath = Path.Combine(_configMgr.ModsPath, fileName);
                        // profile-specific mod takes precedence if duplicate exists
                        if (!File.Exists(linkPath))
                        {
                            File.CreateSymbolicLink(linkPath, file);
                            persistModCount++;
                        }
                    }
                }

                _configMgr.Config.ActiveVersion = version;
                _configMgr.Config.ActiveProfile = profile;
                _configMgr.Save();

                statusMessage = $"Loaded '{profile}' ({profileModCount} profile mods, {persistModCount} persist mods).\n" +
                                "Reminder: Hit 'Reload Mods' in the Vintage Story main menu (do not reload while in an active world/server).";
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                statusMessage = "ERROR: Windows blocked symlink creation. Enable 'Developer Mode' in Windows Settings (System -> For Developers) or run this app as Administrator.";
                return false;
            }
            catch (IOException ex) when (ex.HResult == -2147023582) // ERROR_PRIVILEGE_NOT_HELD (1314)
            {
                statusMessage = "ERROR: Privilege not held for NTFS symlinks. Enable 'Developer Mode' in Windows Settings (System -> For Developers) or run this app as Administrator.";
                return false;
            }
            catch (Exception ex)
            {
                statusMessage = $"ERROR loading profile: {ex.Message}";
                return false;
            }
        }

        public void UnloadActiveProfile()
        {
            InspectAndMigrateLooseMods();
            ClearModsSymlinks();
            _configMgr.Config.ActiveVersion = null;
            _configMgr.Config.ActiveProfile = null;
            _configMgr.Save();
        }

        private void ClearModsSymlinks()
        {
            if (!Directory.Exists(_configMgr.ModsPath)) return;

            foreach (var file in Directory.GetFiles(_configMgr.ModsPath))
            {
                FileInfo fi = new(file);
                bool isSymlink = fi.LinkTarget != null || fi.Attributes.HasFlag(FileAttributes.ReparsePoint);
                if (isSymlink)
                {
                    File.Delete(file);
                }
            }
        }

        public void OpenInExplorer(string path)
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }
}