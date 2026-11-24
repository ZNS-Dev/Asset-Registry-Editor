using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using UE427AssetData = AssetRegistry.FAssetData;
using UE427AssetRegistry = AssetRegistry.AssetRegistry;

namespace AssetRegistry
{
    internal enum AssetRegistryBackend
    {
        UE427,
        UE4Legacy
    }

    internal sealed class AssetRegistryDocument
    {
        private readonly UE427AssetRegistry _ue427;
        private readonly AssetRegistryUE4Legacy _legacy;

        private AssetRegistryDocument(UE427AssetRegistry registry)
        {
            _ue427 = registry ?? throw new ArgumentNullException(nameof(registry));
            Backend = AssetRegistryBackend.UE427;
        }

        private AssetRegistryDocument(AssetRegistryUE4Legacy registry)
        {
            _legacy = registry ?? throw new ArgumentNullException(nameof(registry));
            Backend = AssetRegistryBackend.UE4Legacy;
        }

        public AssetRegistryBackend Backend { get; private set; }

        public int AssetCount
        {
            get
            {
                return Backend == AssetRegistryBackend.UE427
                    ? (_ue427.FAssetDatas == null ? 0 : _ue427.FAssetDatas.Count)
                    : _legacy.AssetCount;
            }
        }

        public static AssetRegistryDocument Read(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            int version = AssetRegistryUE4Legacy.PeekVersion(data);
            if (version == UE427AssetRegistry.SupportedVersion)
            {
                UE427AssetRegistry registry = new();
                registry.Read(data);
                return new AssetRegistryDocument(registry);
            }

            if (version >= 3 && version <= 7)
            {
                return new AssetRegistryDocument(AssetRegistryUE4Legacy.Read(data));
            }

            if (version < 3)
            {
                throw new NotSupportedException(
                    "AssetRegistry version " + version + " uses the FString format from before ChangedAssetData. " +
                    "This generation requires the UE4 engine version (FileVersionUE4) to interpret some fields. " +
                    "Automatically supported formats are versions 3 through 8.");
            }

            throw new NotSupportedException(
                "Unsupported AssetRegistry version: " + version + ". " +
                "This project supports UE4 Legacy versions 3 through 7 and FixedTags version 8.");
        }

        public string ToJson()
        {
            return Backend == AssetRegistryBackend.UE427
                ? JsonConvert.SerializeObject(_ue427, Formatting.Indented)
                : _legacy.ToJson();
        }

        public byte[] Make()
        {
            return Backend == AssetRegistryBackend.UE427 ? _ue427.Make() : _legacy.Make();
        }

        public void InjectJsonFolder(string folderPath)
        {
            if (Backend == AssetRegistryBackend.UE4Legacy)
            {
                _legacy.InjectJsonFolder(folderPath);
                return;
            }

            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("The JSON folder has not been specified.", nameof(folderPath));
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException("JSON folder not found: " + folderPath);

            string[] files = Directory.GetFiles(folderPath, "*.json", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            if (files.Length == 0)
                throw new InvalidDataException("No JSON files were found in: " + folderPath);

            for (int i = 0; i < files.Length; i++)
            {
                UE427AssetData asset = JsonConvert.DeserializeObject<UE427AssetData>(File.ReadAllText(files[i])) ?? throw new InvalidDataException("The JSON file does not contain a valid UE4.27 FAssetData object: " + files[i]);
                _ue427.FAssetDatas.Add(asset);
            }
        }

        public void ExportJsonFolder(string folderPath)
        {
            if (Backend == AssetRegistryBackend.UE4Legacy)
            {
                _legacy.ExportJsonFolder(folderPath);
                return;
            }

            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("The destination folder has not been specified.", nameof(folderPath));

            Directory.CreateDirectory(folderPath);
            for (int i = 0; i < _ue427.FAssetDatas.Count; i++)
            {
                UE427AssetData asset = _ue427.FAssetDatas[i];
                if (asset == null) continue;

                string json = JsonConvert.SerializeObject(asset, Formatting.Indented);
                string safeName = MakeSafeFileName(asset.ToString());
                string fileName = i.ToString("D5") + "_" + safeName + ".json";
                File.WriteAllText(Path.Combine(folderPath, fileName), json, new System.Text.UTF8Encoding(false));
            }
        }

        public byte[] MakeFromJson(string json)
        {
            if (Backend == AssetRegistryBackend.UE4Legacy)
                return _legacy.MakeFromJson(json);

            UE427AssetRegistry registry = JsonConvert.DeserializeObject<UE427AssetRegistry>(json);
            return registry == null ? throw new InvalidDataException("The JSON does not contain a valid UE4.27 registry.") : registry.Make();
        }

        public static byte[] MakeFromJsonAuto(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("The registry JSON is empty.");

            JObject root;
            try { root = JObject.Parse(json); }
            catch (JsonException ex) { throw new InvalidDataException("The AssetRegistry JSON is invalid.", ex); }

            int version = root.Value<int?>("Version") ?? 8;
            if (version == UE427AssetRegistry.SupportedVersion)
            {
                UE427AssetRegistry registry = JsonConvert.DeserializeObject<UE427AssetRegistry>(json);
                return registry == null ? throw new InvalidDataException("The UE4.27 JSON is empty.") : registry.Make();
            }

            if (version >= 3 && version <= 7)
                return AssetRegistryUE4Legacy.FromJson(json).Make();

            throw new NotSupportedException(
                "The JSON targets AssetRegistry version " + version + ". Versions 3 through 8 are supported.");
        }

        private static string MakeSafeFileName(string value)
        {
            string safeName = string.IsNullOrWhiteSpace(value) ? "Asset" : value;
            foreach (char invalid in Path.GetInvalidFileNameChars())
                safeName = safeName.Replace(invalid, '_');

            safeName = safeName.Replace('/', '_').Replace('\\', '_').Trim().TrimEnd('.');
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "Asset";
            if (safeName.Length > 120) safeName = safeName.Substring(0, 120);
            return safeName;
        }
    }
}
