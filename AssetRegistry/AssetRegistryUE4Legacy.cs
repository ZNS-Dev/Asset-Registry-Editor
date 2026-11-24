using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UE427AssetData = AssetRegistry.FAssetData;
using UE427TagValuePair = AssetRegistry.TagValuePair;

namespace AssetRegistry
{
    internal sealed class AssetRegistryUE4Legacy
    {
        private static readonly byte[] RegistryGuidBytes =
        {
            0xE7, 0x9E, 0x7F, 0x71, 0x3A, 0x49, 0xB0, 0xE9,
            0x32, 0x91, 0xB3, 0x88, 0x07, 0x81, 0x38, 0x1B
        };

        private const int FirstSupportedVersion = 3; // ChangedAssetData
        private const int LastSupportedVersion = 7;  // AddedDependencyFlags
        private const int MaxArrayCount = 2_000_000;

        private readonly List<UE427AssetData> _assets;
        private readonly List<NameEntry> _nameTable;
        private readonly Dictionary<string, int> _nameIndices;
        private readonly byte[] _opaqueData;
        private readonly byte[] _nameTableTrailing;
        private readonly List<AssetIdentity> _originalIdentity;
        private readonly bool _hasSourceBaseline;

        private AssetRegistryUE4Legacy(
            int version,
            List<UE427AssetData> assets,
            List<NameEntry> nameTable,
            byte[] opaqueData,
            byte[] nameTableTrailing,
            List<AssetIdentity> originalIdentity,
            bool hasSourceBaseline)
        {
            Version = version;
            _assets = assets ?? new List<UE427AssetData>();
            _nameTable = nameTable ?? new List<NameEntry>();
            _nameIndices = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < _nameTable.Count; i++)
            {
                if (!_nameIndices.ContainsKey(_nameTable[i].Name))
                    _nameIndices.Add(_nameTable[i].Name, i);
            }
            _opaqueData = opaqueData ?? Array.Empty<byte>();
            _nameTableTrailing = nameTableTrailing ?? Array.Empty<byte>();
            _originalIdentity = originalIdentity ?? new List<AssetIdentity>();
            _hasSourceBaseline = hasSourceBaseline;
        }

        public int Version { get; private set; }
        public int AssetCount { get { return _assets.Count; } }

        public static int PeekVersion(byte[] data)
        {
            if (data == null || data.Length < 20)
                throw new InvalidDataException("The AssetRegistry file is too short to contain its GUID and version.");

            for (int i = 0; i < RegistryGuidBytes.Length; i++)
            {
                if (data[i] != RegistryGuidBytes[i])
                    throw new NotSupportedException("A non-versioned AssetRegistry GUID was recognized. Legacy formats earlier than ChangedAssetData are not supported yet.");
            }

            return BitConverter.ToInt32(data, 16);
        }

        public static AssetRegistryUE4Legacy Read(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            using MemoryStream stream = new(data, false);
            using BinaryReader reader = new(stream, Encoding.UTF8, true);
            byte[] guid = ReadExact(reader, RegistryGuidBytes.Length);
            if (!guid.SequenceEqual(RegistryGuidBytes))
                throw new InvalidDataException("Invalid AssetRegistry GUID for the UE4 Legacy reader.");

            int version = reader.ReadInt32();
            if (version < FirstSupportedVersion || version > LastSupportedVersion)
            {
                if (version < FirstSupportedVersion)
                    throw new NotSupportedException("AssetRegistry version " + version + " uses the older FString format (before ChangedAssetData). The exact UE4 engine version is also required to interpret ChunkIDs and PackageFlags.");
                throw new NotSupportedException("AssetRegistry version " + version + " is not a supported UE4 Legacy format (versions 3 to 7).");
            }

            long nameTableOffset = reader.ReadInt64();
            long assetDataStart = stream.Position;
            if (nameTableOffset < assetDataStart || nameTableOffset > stream.Length - 4)
                throw new InvalidDataException("Invalid name table offset: " + nameTableOffset + ".");

            // The name table is stored at an offset in the header. Read it first
            // so each FName can be resolved while the assets are parsed.
            stream.Position = nameTableOffset;
            int nameCount = ReadCount(reader, "NameTable", MaxArrayCount);
            List<NameEntry> nameTable = new(nameCount);
            for (int i = 0; i < nameCount; i++)
            {
                string name = ReadFString(reader);
                ushort nonCasePreservingHash = reader.ReadUInt16();
                ushort casePreservingHash = reader.ReadUInt16();
                nameTable.Add(new NameEntry(name, nonCasePreservingHash, casePreservingHash));
            }
            byte[] nameTableTrailing = ReadExact(reader, checked((int)(stream.Length - stream.Position)));

            stream.Position = assetDataStart;
            int assetCount = ReadCount(reader, "Assets", MaxArrayCount);
            List<UE427AssetData> assets = new(assetCount);

            for (int i = 0; i < assetCount; i++)
            {
                UE427AssetData asset = new()
                {
                    ObjectPath = ReadFName(reader, nameTable),
                    PackagePath = ReadFName(reader, nameTable),
                    AssetClass = ReadFName(reader, nameTable),
                    PackageName = ReadFName(reader, nameTable),
                    AssetName = ReadFName(reader, nameTable),
                    TagAndValue = new List<UE427TagValuePair>(),
                    TaggedAssetBundles = new List<global::AssetRegistry.AssetBundleEntry>(),
                    ChunkIDs = new List<int>()
                };

                int tagCount = ReadCount(reader, "Tags", 1_000_000);
                for (int t = 0; t < tagCount; t++)
                {
                    string key = ReadFName(reader, nameTable);
                    string value = ReadFString(reader);
                    asset.TagAndValue.Add(new UE427TagValuePair(key, value));
                }

                int chunkCount = ReadCount(reader, "ChunkIDs", 1_000_000);
                for (int c = 0; c < chunkCount; c++)
                    asset.ChunkIDs.Add(reader.ReadInt32());

                asset.PackageFlags = reader.ReadUInt32();
                assets.Add(asset);
            }

            long assetDataEnd = stream.Position;
            if (assetDataEnd > nameTableOffset)
                throw new InvalidDataException("The asset list extends beyond the start of the name table.");

            // Unparsed sections are preserved without modification. They may contain
            // dependency and package data from the source registry.
            byte[] opaqueData = ReadBytesAt(reader, assetDataEnd, nameTableOffset - assetDataEnd);

            if (assets.Count > 0 && nameTable.Count == 0)
                throw new InvalidDataException("The name table is empty even though the registry contains assets.");

            return new AssetRegistryUE4Legacy(version, assets, nameTable, opaqueData, nameTableTrailing,
                assets.Select(CreateIdentity).ToList(), true);
        }

        public string ToJson()
        {
            LegacyRegistryJson dto = new()
            {
                Version = Version,
                Header = "717F9EE7-E9B0-493A-88B3-91321B388107",
                FAssetDatas = _assets
            };
            return JsonConvert.SerializeObject(dto, Formatting.Indented);
        }

        public byte[] Make()
        {
            EnsureNameTableContainsAssetNames();

            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream, Encoding.UTF8, true);
            writer.Write(RegistryGuidBytes);
            writer.Write(Version);
            long nameOffsetPosition = stream.Position;
            writer.Write((long)0);

            writer.Write(_assets.Count);
            for (int i = 0; i < _assets.Count; i++)
            {
                UE427AssetData asset = NormalizeAsset(_assets[i]);
                WriteFName(writer, asset.ObjectPath);
                WriteFName(writer, asset.PackagePath);
                WriteFName(writer, asset.AssetClass);
                WriteFName(writer, asset.PackageName);
                WriteFName(writer, asset.AssetName);

                writer.Write(asset.TagAndValue.Count);
                for (int t = 0; t < asset.TagAndValue.Count; t++)
                {
                    writer.Write(FindOrAddName(GetNameParts(asset.TagAndValue[t].Item1).BaseName));
                    writer.Write(GetNameParts(asset.TagAndValue[t].Item1).Number);
                    WriteFString(writer, asset.TagAndValue[t].Item2 ?? string.Empty);
                }

                writer.Write(asset.ChunkIDs.Count);
                for (int c = 0; c < asset.ChunkIDs.Count; c++)
                    writer.Write(asset.ChunkIDs[c]);
                writer.Write(asset.PackageFlags);
            }

            writer.Write(_opaqueData);
            long nameTableOffset = stream.Position;

            writer.BaseStream.Position = nameOffsetPosition;
            writer.Write(nameTableOffset);
            writer.BaseStream.Position = nameTableOffset;

            writer.Write(_nameTable.Count);
            for (int i = 0; i < _nameTable.Count; i++)
            {
                NameEntry entry = _nameTable[i];
                WriteFString(writer, entry.Name);
                writer.Write(entry.NonCasePreservingHash);
                writer.Write(entry.CasePreservingHash);
            }
            writer.Write(_nameTableTrailing);
            writer.Flush();
            return stream.ToArray();
        }

        public byte[] MakeFromJson(string json)
        {
            LegacyRegistryJson dto = DeserializeRegistryJson(json);
            if (dto.Version != 0 && dto.Version != Version)
                throw new InvalidDataException("The JSON targets AssetRegistry version " + dto.Version + " while the source file uses version " + Version + ".");

            ValidateOriginalAssetIdentity(dto.FAssetDatas);
            if (dto.FAssetDatas.Count > _originalIdentity.Count && _hasSourceBaseline)
                EnsureAppendIsSafe();
            _assets.Clear();
            for (int i = 0; i < dto.FAssetDatas.Count; i++)
                _assets.Add(NormalizeAsset(dto.FAssetDatas[i]));
            return Make();
        }

        public static AssetRegistryUE4Legacy FromJson(string json)
        {
            LegacyRegistryJson dto = DeserializeRegistryJson(json);
            int version = dto.Version == 0 ? LastSupportedVersion : dto.Version;
            if (version < FirstSupportedVersion || version > LastSupportedVersion)
                throw new InvalidDataException("The JSON must target an AssetRegistry version between 3 and 7.");

            return new AssetRegistryUE4Legacy(
                version,
                dto.FAssetDatas.Select(NormalizeAsset).ToList(),
                new List<NameEntry>(),
                Array.Empty<byte>(),
                Array.Empty<byte>(),
                new List<AssetIdentity>(),
                false);
        }

        public void InjectJsonFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("The JSON folder has not been specified.", nameof(folderPath));
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException("JSON folder not found: " + folderPath);

            string[] files = Directory.GetFiles(folderPath, "*.json", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            if (files.Length == 0)
                throw new InvalidDataException("No JSON files were found in: " + folderPath);

            EnsureAppendIsSafe();
            for (int i = 0; i < files.Length; i++)
            {
                UE427AssetData asset = JsonConvert.DeserializeObject<UE427AssetData>(File.ReadAllText(files[i])) ?? throw new InvalidDataException("The JSON file does not contain a valid FAssetData object: " + files[i]);
                _assets.Add(NormalizeAsset(asset));
            }
        }

        public void ExportJsonFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("The destination folder has not been specified.", nameof(folderPath));

            Directory.CreateDirectory(folderPath);
            for (int i = 0; i < _assets.Count; i++)
            {
                UE427AssetData asset = _assets[i];
                string safeName = MakeSafeFileName(asset.ToString());
                string fileName = i.ToString("D5") + "_" + safeName + ".json";
                File.WriteAllText(
                    Path.Combine(folderPath, fileName),
                    JsonConvert.SerializeObject(asset, Formatting.Indented),
                    new UTF8Encoding(false));
            }
        }

        private static LegacyRegistryJson DeserializeRegistryJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("The registry JSON is empty.");

            LegacyRegistryJson dto = JsonConvert.DeserializeObject<LegacyRegistryJson>(json);
            if (dto == null || dto.FAssetDatas == null)
                throw new InvalidDataException("The JSON does not contain the fAssetDatas list.");
            dto.FAssetDatas = dto.FAssetDatas.Select(NormalizeAsset).ToList();
            return dto;
        }

        private void EnsureAppendIsSafe()
        {
            bool noOpaqueSections = _opaqueData.Length == 0 && _nameTableTrailing.Length == 0;
            bool emptyDependencyMarker = _opaqueData.Length == 8 &&
                _opaqueData.All(value => value == 0) && _nameTableTrailing.Length == 0;
            if (!noOpaqueSections && !emptyDependencyMarker)
            {
                throw new InvalidDataException(
                    "Adding assets is disabled for this UE4 Legacy registry because dependency/package data is present but has not been decoded. " +
                    "Existing assets can still be exported and their tags can be edited without reindexing this data.");
            }
        }

        private void ValidateOriginalAssetIdentity(IList<UE427AssetData> updatedAssets)
        {
            if (_originalIdentity.Count == 0) return;
            if (updatedAssets.Count < _originalIdentity.Count)
                throw new InvalidDataException("Deleting assets is not allowed in Legacy format because the preserved dependency section references the original entries.");

            for (int i = 0; i < _originalIdentity.Count; i++)
            {
                AssetIdentity before = _originalIdentity[i];
                UE427AssetData after = updatedAssets[i];
                if (after == null ||
                    !string.Equals(before.ObjectPath, after.ObjectPath ?? string.Empty, StringComparison.Ordinal) ||
                    !string.Equals(before.PackagePath, after.PackagePath ?? string.Empty, StringComparison.Ordinal) ||
                    !string.Equals(before.AssetClass, after.AssetClass ?? string.Empty, StringComparison.Ordinal) ||
                    !string.Equals(before.PackageName, after.PackageName ?? string.Empty, StringComparison.Ordinal) ||
                    !string.Equals(before.AssetName, after.AssetName ?? string.Empty, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("Existing asset identity fields cannot be changed with the Legacy reader because the additional dependency/package data is preserved without reindexing. Tags, ChunkIDs, and PackageFlags can be modified.");
                }
            }
        }

        private static AssetIdentity CreateIdentity(UE427AssetData asset)
        {
            return new AssetIdentity
            {
                ObjectPath = asset.ObjectPath ?? string.Empty,
                PackagePath = asset.PackagePath ?? string.Empty,
                AssetClass = asset.AssetClass ?? string.Empty,
                PackageName = asset.PackageName ?? string.Empty,
                AssetName = asset.AssetName ?? string.Empty
            };
        }

        private sealed class AssetIdentity
        {
            public string ObjectPath { get; set; }
            public string PackagePath { get; set; }
            public string AssetClass { get; set; }
            public string PackageName { get; set; }
            public string AssetName { get; set; }
        }

        private void EnsureNameTableContainsAssetNames()
        {
            for (int i = 0; i < _assets.Count; i++)
            {
                UE427AssetData asset = NormalizeAsset(_assets[i]);
                EnsureName(GetNameParts(asset.ObjectPath).BaseName);
                EnsureName(GetNameParts(asset.PackagePath).BaseName);
                EnsureName(GetNameParts(asset.AssetClass).BaseName);
                EnsureName(GetNameParts(asset.PackageName).BaseName);
                EnsureName(GetNameParts(asset.AssetName).BaseName);
                for (int t = 0; t < asset.TagAndValue.Count; t++)
                    EnsureName(GetNameParts(asset.TagAndValue[t].Item1).BaseName);
            }
        }

        private void EnsureName(string name)
        {
            if (_nameIndices.ContainsKey(name))
                return;

            UnrealNameHash.Generate(name, out ushort nonCaseHash, out ushort caseHash);
            int index = _nameTable.Count;
            _nameTable.Add(new NameEntry(name, nonCaseHash, caseHash));
            _nameIndices.Add(name, index);
        }

        private int FindOrAddName(string name)
        {
            if (_nameIndices.TryGetValue(name, out int index))
                return index;

            EnsureName(name);
            return _nameIndices[name];
        }

        private void WriteFName(BinaryWriter writer, string value)
        {
            NameParts parts = GetNameParts(value);
            writer.Write(FindOrAddName(parts.BaseName));
            writer.Write(parts.Number);
        }

        private static string ReadFName(BinaryReader reader, IList<NameEntry> names)
        {
            int index = reader.ReadInt32();
            int number = reader.ReadInt32();
            if (index < 0 || index >= names.Count || number < 0)
                throw new InvalidDataException("Invalid FName reference: index " + index + ", number " + number + ".");

            string name = names[index].Name;
            return number > 0
                ? name + "_" + (number - 1).ToString(System.Globalization.CultureInfo.InvariantCulture)
                : name;
        }

        private static UE427AssetData NormalizeAsset(UE427AssetData asset)
        {
            if (asset == null)
            {
                throw new InvalidDataException("A FAssetData entry is null.");
            }

            asset.TagAndValue ??= new List<UE427TagValuePair>();
            asset.ChunkIDs ??= new List<int>();
            asset.TaggedAssetBundles ??= new List<global::AssetRegistry.AssetBundleEntry>();
            asset.ObjectPath ??= string.Empty;
            asset.PackagePath ??= string.Empty;
            asset.AssetClass ??= string.Empty;
            asset.PackageName ??= string.Empty;
            asset.AssetName ??= string.Empty;
            for (int i = 0; i < asset.TagAndValue.Count; i++)
            {
                if (asset.TagAndValue[i] == null) asset.TagAndValue[i] = new UE427TagValuePair();
                if (asset.TagAndValue[i].Item1 == null) asset.TagAndValue[i].Item1 = string.Empty;
                if (asset.TagAndValue[i].Item2 == null) asset.TagAndValue[i].Item2 = string.Empty;
            }
            return asset;
        }

        private static NameParts GetNameParts(string value)
        {
            string name = value ?? string.Empty;
            int separator = name.LastIndexOf('_');
            if (separator > 0 && separator < name.Length - 1)
            {
                if (int.TryParse(name.Substring(separator + 1), out int suffix) && suffix >= 0 &&
                    suffix.ToString(System.Globalization.CultureInfo.InvariantCulture) == name.Substring(separator + 1))
                {
                    return new NameParts(name.Substring(0, separator), checked(suffix + 1));
                }
            }
            return new NameParts(name, 0);
        }

        private static int ReadCount(BinaryReader reader, string label, int maximum)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > maximum)
                throw new InvalidDataException("Invalid " + label + " count: " + count + ".");
            return count;
        }

        private static string ReadFString(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length == 0) return string.Empty;
            if (length == int.MinValue)
                throw new InvalidDataException("Invalid FString length.");

            if (length > 0)
            {
                if (length > 64 * 1024 * 1024)
                    throw new InvalidDataException("ANSI FString is too long.");
                byte[] bytes = ReadExact(reader, length);
                int textLength = bytes.Length > 0 && bytes[bytes.Length - 1] == 0 ? bytes.Length - 1 : bytes.Length;
                return Encoding.Default.GetString(bytes, 0, textLength);
            }

            int charCount = -length;
            if (charCount > 32 * 1024 * 1024)
                throw new InvalidDataException("Unicode FString is too long.");
            byte[] wide = ReadExact(reader, checked(charCount * 2));
            int byteLength = wide.Length >= 2 && wide[wide.Length - 1] == 0 && wide[wide.Length - 2] == 0
                ? wide.Length - 2
                : wide.Length;
            return Encoding.Unicode.GetString(wide, 0, byteLength);
        }

        private static void WriteFString(BinaryWriter writer, string value)
        {
            string text = value ?? string.Empty;
            bool ascii = true;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] > 0x7F)
                {
                    ascii = false;
                    break;
                }
            }

            if (ascii)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(text + "\0");
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            else
            {
                byte[] bytes = Encoding.Unicode.GetBytes(text + "\0");
                writer.Write(-((bytes.Length / 2)));
                writer.Write(bytes);
            }
        }

        private static byte[] ReadBytesAt(BinaryReader reader, long start, long count)
        {
            if (count < 0 || count > int.MaxValue)
                throw new InvalidDataException("Invalid additional data size: " + count + ".");
            reader.BaseStream.Position = start;
            return ReadExact(reader, (int)count);
        }

        private static byte[] ReadExact(BinaryReader reader, int count)
        {
            if (count < 0) throw new InvalidDataException("Negative read size.");
            byte[] bytes = reader.ReadBytes(count);
            if (bytes.Length != count)
                throw new EndOfStreamException("Unexpected end of the AssetRegistry file.");
            return bytes;
        }

        private static string MakeSafeFileName(string value)
        {
            string safe = string.IsNullOrWhiteSpace(value) ? "Asset" : value;
            foreach (char invalid in Path.GetInvalidFileNameChars()) safe = safe.Replace(invalid, '_');
            safe = safe.Replace('/', '_').Replace('\\', '_').Trim().TrimEnd('.');
            if (safe.Length == 0) safe = "Asset";
            if (safe.Length > 120) safe = safe.Substring(0, 120);
            return safe;
        }

        private sealed class LegacyRegistryJson
        {
            public int Version { get; set; }
            public string Header { get; set; }
            public List<UE427AssetData> FAssetDatas { get; set; } = new List<UE427AssetData>();
        }

        private sealed class NameEntry
        {
            public NameEntry(string name, ushort nonCasePreservingHash, ushort casePreservingHash)
            {
                Name = name ?? string.Empty;
                NonCasePreservingHash = nonCasePreservingHash;
                CasePreservingHash = casePreservingHash;
            }

            public string Name { get; private set; }
            public ushort NonCasePreservingHash { get; private set; }
            public ushort CasePreservingHash { get; private set; }
        }

        private sealed class NameParts
        {
            public NameParts(string baseName, int number)
            {
                BaseName = baseName;
                Number = number;
            }
            public string BaseName { get; private set; }
            public int Number { get; private set; }
        }

        private static class UnrealNameHash
        {
            private static readonly uint[] DeprecatedTable = BuildTable(0x04C11DB7u, false);
            private static readonly uint[] Crc32Table = BuildTable(0xEDB88320u, true);

            public static void Generate(string value, out ushort nonCasePreserving, out ushort casePreserving)
            {
                uint oldCrc = 0;
                string text = value ?? string.Empty;
                for (int i = 0; i < text.Length; i++)
                {
                    ushort ch = text[i];
                    if (ch >= 'a' && ch <= 'z') ch = (ushort)(ch - 32);
                    oldCrc = ((oldCrc >> 8) & 0x00FFFFFFu) ^ DeprecatedTable[(int)((oldCrc ^ (ch & 0xFF)) & 0xFF)];
                    if (ch > 0xFF)
                        oldCrc = ((oldCrc >> 8) & 0x00FFFFFFu) ^ DeprecatedTable[(int)((oldCrc ^ (ch >> 8)) & 0xFF)];
                }

                uint crc = 0xFFFFFFFFu;
                for (int i = 0; i < text.Length; i++)
                {
                    uint ch = text[i];
                    crc = (crc >> 8) ^ Crc32Table[(int)((crc ^ ch) & 0xFF)];
                    ch >>= 8;
                    crc = (crc >> 8) ^ Crc32Table[(int)((crc ^ ch) & 0xFF)];
                    crc = (crc >> 8) ^ Crc32Table[(int)(crc & 0xFF)];
                    crc = (crc >> 8) ^ Crc32Table[(int)(crc & 0xFF)];
                }

                nonCasePreserving = (ushort)(oldCrc & 0xFFFF);
                casePreserving = (ushort)((~crc) & 0xFFFF);
            }

            private static uint[] BuildTable(uint polynomial, bool reflected)
            {
                uint[] table = new uint[256];
                for (uint i = 0; i < 256; i++)
                {
                    uint crc = reflected ? i : i << 24;
                    for (int bit = 0; bit < 8; bit++)
                    {
                        if (reflected)
                            crc = (crc & 1) != 0 ? (crc >> 1) ^ polynomial : crc >> 1;
                        else
                            crc = (crc & 0x80000000u) != 0 ? (crc << 1) ^ polynomial : crc << 1;
                    }
                    table[i] = crc;
                }
                return table;
            }
        }
    }
}
