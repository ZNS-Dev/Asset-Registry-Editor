using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetRegistry
{
    internal sealed class ProfileInfo
    {
        public string Name { get; set; }
        public string OriginalAssetRegistryPath { get; set; }
        public DateTime CreatedUtc { get; set; }

        [JsonIgnore]
        public string DirectoryPath { get; set; }

        [JsonIgnore]
        public string AssetRegistryPath
        {
            get { return Path.Combine(DirectoryPath ?? string.Empty, "AssetRegistry.bin"); }
        }

        [JsonIgnore]
        public string InjectDirectory
        {
            get { return Path.Combine(DirectoryPath ?? string.Empty, "Inject"); }
        }

        [JsonIgnore]
        public string CustomAssetRegistryPath
        {
            get { return Path.Combine(DirectoryPath ?? string.Empty, "AssetRegistry_Custom.bin"); }
        }

        public override string ToString()
        {
            return Name ?? string.Empty;
        }
    }

    internal sealed class ProfileInjectionResult
    {
        public int JsonFileCount { get; set; }
        public int AssetCount { get; set; }
        public string OutputPath { get; set; }
    }

    internal static class ProfileManager
    {
        private const string ProfileMetadataFileName = "profile.json";
        private const string SourceAssetRegistryFileName = "AssetRegistry.bin";
        private const string InjectDirectoryName = "Inject";
        private const string CustomAssetRegistryFileName = "AssetRegistry_Custom.bin";

        public static string DataDirectory
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"); }
        }

        public static ProfileInfo CreateProfile(string profileName, string originalAssetRegistryPath)
        {
            string safeName = ValidateProfileName(profileName);
            if (string.IsNullOrWhiteSpace(originalAssetRegistryPath) || !File.Exists(originalAssetRegistryPath))
                throw new FileNotFoundException("The original game AssetRegistry file could not be found.", originalAssetRegistryPath);

            byte[] originalBytes = File.ReadAllBytes(originalAssetRegistryPath);
            AssetRegistryDocument.Read(originalBytes);

            Directory.CreateDirectory(DataDirectory);
            string profileDirectory = Path.Combine(DataDirectory, safeName);
            if (Directory.Exists(profileDirectory) || File.Exists(profileDirectory))
                throw new IOException("A profile with this name already exists: " + safeName);

            bool directoryCreated = false;
            try
            {
                Directory.CreateDirectory(profileDirectory);
                directoryCreated = true;

                string copiedAssetRegistry = Path.Combine(profileDirectory, SourceAssetRegistryFileName);
                File.Copy(originalAssetRegistryPath, copiedAssetRegistry, false);

                string injectDirectory = Path.Combine(profileDirectory, InjectDirectoryName);
                Directory.CreateDirectory(injectDirectory);

                ProfileInfo profile = new()
                {
                    Name = safeName,
                    OriginalAssetRegistryPath = Path.GetFullPath(originalAssetRegistryPath),
                    CreatedUtc = DateTime.UtcNow,
                    DirectoryPath = profileDirectory
                };

                string metadataPath = Path.Combine(profileDirectory, ProfileMetadataFileName);
                string metadataJson = JsonConvert.SerializeObject(new
                {
                    profile.Name,
                    profile.OriginalAssetRegistryPath,
                    profile.CreatedUtc
                }, Formatting.Indented);
                File.WriteAllText(metadataPath, metadataJson, new System.Text.UTF8Encoding(false));

                return profile;
            }
            catch
            {
                if (directoryCreated && Directory.Exists(profileDirectory))
                    Directory.Delete(profileDirectory, true);
                throw;
            }
        }

        public static List<ProfileInfo> LoadProfiles()
        {
            List<ProfileInfo> profiles = new();
            if (!Directory.Exists(DataDirectory))
                return profiles;

            string[] directories = Directory.GetDirectories(DataDirectory, "*", SearchOption.TopDirectoryOnly);
            Array.Sort(directories, StringComparer.OrdinalIgnoreCase);

            foreach (string directory in directories)
            {
                string metadataPath = Path.Combine(directory, ProfileMetadataFileName);
                if (!File.Exists(metadataPath))
                    continue;

                try
                {
                    ProfileInfo profile = JsonConvert.DeserializeObject<ProfileInfo>(File.ReadAllText(metadataPath));
                    if (profile == null)
                        continue;

                    profile.DirectoryPath = directory;
                    profile.Name = Path.GetFileName(directory);
                    if (!File.Exists(profile.AssetRegistryPath))
                        continue;

                    if (!Directory.Exists(profile.InjectDirectory))
                        Directory.CreateDirectory(profile.InjectDirectory);

                    profiles.Add(profile);
                }
                catch
                {
                }
            }

            return profiles;
        }

        public static ProfileInjectionResult InjectAll(ProfileInfo profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            if (string.IsNullOrWhiteSpace(profile.DirectoryPath) || !Directory.Exists(profile.DirectoryPath))
                throw new DirectoryNotFoundException("The selected profile folder could not be found.");
            if (!File.Exists(profile.AssetRegistryPath))
                throw new FileNotFoundException("The profile's original AssetRegistry copy could not be found.", profile.AssetRegistryPath);
            if (!Directory.Exists(profile.InjectDirectory))
                throw new DirectoryNotFoundException("The profile's Inject folder could not be found.");

            string[] jsonFiles = Directory.GetFiles(profile.InjectDirectory, "*.json", SearchOption.AllDirectories);
            Array.Sort(jsonFiles, StringComparer.OrdinalIgnoreCase);
            if (jsonFiles.Length == 0)
                throw new InvalidDataException("No JSON files were found in this profile's Inject folder.");

            // Always start from the copied original registry so repeated injections do not append duplicates.
            AssetRegistryDocument document = AssetRegistryDocument.Read(File.ReadAllBytes(profile.AssetRegistryPath));
            document.InjectJsonFolder(profile.InjectDirectory);
            byte[] outputBytes = document.Make();

            string outputPath = profile.CustomAssetRegistryPath;
            string temporaryPath = outputPath + ".tmp";
            try
            {
                File.WriteAllBytes(temporaryPath, outputBytes);
                if (File.Exists(outputPath))
                {
                    string backupPath = outputPath + ".bak";
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Replace(temporaryPath, outputPath, backupPath, true);
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                }
                else
                {
                    File.Move(temporaryPath, outputPath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }

            return new ProfileInjectionResult
            {
                JsonFileCount = jsonFiles.Length,
                AssetCount = document.AssetCount,
                OutputPath = outputPath
            };
        }

        private static string ValidateProfileName(string value)
        {
            string name = (value ?? string.Empty).Trim();
            if (name.Length == 0)
                throw new ArgumentException("Enter a profile name.", nameof(value));
            if (name.Length > 64)
                throw new ArgumentException("Profile names cannot exceed 64 characters.", nameof(value));
            if (name == "." || name == ".." || name.EndsWith(".", StringComparison.Ordinal) || name.EndsWith(" ", StringComparison.Ordinal))
                throw new ArgumentException("The profile name is not a valid folder name.", nameof(value));

            char[] invalidCharacters = Path.GetInvalidFileNameChars()
                .Concat(new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' })
                .Distinct()
                .ToArray();
            if (name.IndexOfAny(invalidCharacters) >= 0)
                throw new ArgumentException("The profile name contains characters that are not allowed in Windows folder names.", nameof(value));

            string baseName = name.Split('.')[0];
            string[] reservedNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
            if (reservedNames.Contains(baseName, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("This profile name is reserved by Windows.", nameof(value));

            return name;
        }
    }
}
