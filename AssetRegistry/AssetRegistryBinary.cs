using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AssetRegistry
{
    internal static class AssetRegistryBinary
    {
        private const uint StoreBeginMagic = 0x12345679;
        private const uint StoreOldBeginMagic = 0x12345678;
        private const uint StoreEndMagic = 0x87654321;
        private const uint NumberedNameBit = 0x80000000;
        private static readonly byte[] ExpectedGuidBytes =
        {
            0xE7, 0x9E, 0x7F, 0x71, 0x3A, 0x49, 0xB0, 0xE9,
            0x32, 0x91, 0xB3, 0x88, 0x07, 0x81, 0x38, 0x1B
        };
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly Encoding Utf16 = Encoding.Unicode;

        private sealed class NameRef
        {
            public string Name;
            public uint Number;
        }

        private sealed class NumberlessExportPath
        {
            public uint ClassObject;
            public uint Object;
            public uint Package;
        }

        private sealed class ExportPath
        {
            public NameRef Class;
            public NameRef Object;
            public NameRef Package;
        }

        private sealed class NumberlessPair
        {
            public uint Key;
            public uint Value;
        }

        private sealed class NumberedPair
        {
            public NameRef Key;
            public uint Value;
        }

        private sealed class StoreData
        {
            public string[] NameTable = Array.Empty<string>();
            public uint[] NumberlessNames = Array.Empty<uint>();
            public NameRef[] Names = Array.Empty<NameRef>();
            public NumberlessExportPath[] NumberlessExportPaths = Array.Empty<NumberlessExportPath>();
            public ExportPath[] ExportPaths = Array.Empty<ExportPath>();
            public string[] Texts = Array.Empty<string>();
            public uint[] AnsiOffsets = Array.Empty<uint>();
            public uint[] WideOffsets = Array.Empty<uint>();
            public byte[] AnsiBytes = Array.Empty<byte>();
            public byte[] WideBytes = Array.Empty<byte>();
            public NumberlessPair[] NumberlessPairs = Array.Empty<NumberlessPair>();
            public NumberedPair[] Pairs = Array.Empty<NumberedPair>();
        }

        public static AssetRegistry Read(byte[] data)
        {
            using MemoryStream ms = new(data, false);
            using BinaryReader reader = new(ms, Utf8, true);
            byte[] guidBytes = ReadExact(reader, ExpectedGuidBytes.Length);
            if (!guidBytes.SequenceEqual(ExpectedGuidBytes))
                throw new InvalidDataException(
                    "Invalid AssetRegistry GUID. Expected: " + AssetRegistry.RegistryGuid +
                    "; received bytes: " + BitConverter.ToString(guidBytes));

            int version = reader.ReadInt32();
            if (version != AssetRegistry.SupportedVersion)
                throw new NotSupportedException("This reader supports UE4.27 FixedTags format (version 8). Detected version: " + version);

            AssetRegistry result = new() { Version = version, Header = AssetRegistry.RegistryGuid };
            StoreData store = ReadStore(reader, version);

            int assetCount = ReadCount(reader, "AssetData", 2_000_000);
            result.FAssetDatas = new List<FAssetData>(assetCount);
            for (int i = 0; i < assetCount; i++)
            {
                FAssetData asset = new()
                {
                    ObjectPath = ToText(ReadNameRef(reader, store.NameTable)),
                    PackagePath = ToText(ReadNameRef(reader, store.NameTable)),
                    AssetClass = ToText(ReadNameRef(reader, store.NameTable)),
                    PackageName = ToText(ReadNameRef(reader, store.NameTable)),
                    AssetName = ToText(ReadNameRef(reader, store.NameTable))
                };

                ulong map = reader.ReadUInt64();
                int tagCount = (int)((map >> 32) & 0xFFFF);
                uint pairBegin = (uint)map;
                bool numberlessKeys = (map & 0x8000000000000000UL) != 0;
                asset.TagAndValue = new List<TagValuePair>(tagCount);
                for (int t = 0; t < tagCount; t++)
                {
                    ulong pairIndex = (ulong)pairBegin + (uint)t;
                    string key;
                    uint valueId;
                    if (numberlessKeys)
                    {
                        if (pairIndex >= (ulong)store.NumberlessPairs.Length) throw new InvalidDataException("Tag index is out of range (NumberlessPairs).");
                        NumberlessPair pair = store.NumberlessPairs[(int)pairIndex];
                        if (pair.Key >= (uint)store.NameTable.Length) throw new InvalidDataException("Invalid tag name index.");
                        key = store.NameTable[pair.Key];
                        valueId = pair.Value;
                    }
                    else
                    {
                        if (pairIndex >= (ulong)store.Pairs.Length) throw new InvalidDataException("Tag index is out of range (Pairs).");
                        NumberedPair pair = store.Pairs[(int)pairIndex];
                        key = ToText(pair.Key);
                        valueId = pair.Value;
                    }
                    asset.TagAndValue.Add(new TagValuePair(key, ResolveValue(store, valueId)));
                }

                int bundleCount = ReadCount(reader, "Bundles", 100_000);
                asset.TaggedAssetBundles = new List<AssetBundleEntry>(bundleCount);
                for (int b = 0; b < bundleCount; b++)
                {
                    AssetBundleEntry bundle = new()
                    {
                        BundleName = ToText(ReadNameRef(reader, store.NameTable))
                    };
                    int bundleAssetCount = ReadCount(reader, "BundleAssets", 1_000_000);
                    bundle.BundleAssets = new List<AssetBundleAsset>(bundleAssetCount);
                    for (int ba = 0; ba < bundleAssetCount; ba++)
                    {
                        bundle.BundleAssets.Add(new AssetBundleAsset
                        {
                            AssetPathName = ToText(ReadNameRef(reader, store.NameTable)),
                            SubPathString = ReadFString(reader)
                        });
                    }
                    asset.TaggedAssetBundles.Add(bundle);
                }

                int chunkCount = ReadCount(reader, "ChunkIDs", 1_000_000);
                asset.ChunkIDs = new List<int>(chunkCount);
                for (int c = 0; c < chunkCount; c++) asset.ChunkIDs.Add(reader.ReadInt32());
                asset.PackageFlags = reader.ReadUInt32();
                result.FAssetDatas.Add(asset);
            }

            ReadDependenciesAndPackages(reader, result, store.NameTable);
            if (ms.Position != ms.Length)
                throw new InvalidDataException("Unparsed data remains at the end of the registry: " + (ms.Length - ms.Position) + " bytes.");
            return result;
        }

        private static StoreData ReadStore(BinaryReader reader, int version)
        {
            if (version < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(version), version, $"« {nameof(version)} » ne peut pas être négative");
            }

            int nameCount = ReadCount(reader, "NameBatch", 2_000_000);
            reader.ReadUInt32();
            reader.ReadUInt64();
            SkipBytes(reader, checked((long)nameCount * 8));

            bool[] unicode = new bool[nameCount];
            int[] lengths = new int[nameCount];
            for (int i = 0; i < nameCount; i++)
            {
                byte first = reader.ReadByte();
                byte second = reader.ReadByte();
                unicode[i] = (first & 0x80) != 0;
                lengths[i] = ((first & 0x7F) << 8) | second;
            }

            string[] names = new string[nameCount];
            for (int i = 0; i < nameCount; i++)
            {
                int byteCount = checked(lengths[i] * (unicode[i] ? 2 : 1));
                byte[] raw = ReadExact(reader, byteCount);
                names[i] = (unicode[i] ? Utf16 : Utf8).GetString(raw);
            }

            uint magic = reader.ReadUInt32();
            if (magic != StoreBeginMagic && magic != StoreOldBeginMagic)
                throw new InvalidDataException("Invalid FStore magic value.");
            int[] counts = new int[11];
            for (int i = 0; i < counts.Length; i++) counts[i] = ReadCount(reader, "FStore[" + i + "]", 10_000_000);

            string[] texts = Array.Empty<string>();
            if (magic == StoreBeginMagic)
            {
                reader.ReadUInt32();
                texts = ReadArray(reader, counts[4], () => ReadFString(reader));
            }

            uint[] numberlessNames = ReadArray(reader, counts[0], () => reader.ReadUInt32());
            NameRef[] storeNames = ReadArray(reader, counts[1], () => ReadNameRef(reader, names));

            NumberlessExportPath[] numberlessExportPaths = ReadArray(reader, counts[2], () => new NumberlessExportPath
            {
                ClassObject = reader.ReadUInt32(),
                Object = reader.ReadUInt32(),
                Package = reader.ReadUInt32()
            });

            ExportPath[] exportPaths = ReadArray(reader, counts[3], () => new ExportPath
            {
                Class = ReadNameRef(reader, names),
                Object = ReadNameRef(reader, names),
                Package = ReadNameRef(reader, names)
            });

            if (magic == StoreOldBeginMagic)
                texts = ReadArray(reader, counts[4], () => ReadFString(reader));

            uint[] ansiOffsets = ReadArray(reader, counts[5], () => reader.ReadUInt32());
            uint[] wideOffsets = ReadArray(reader, counts[6], () => reader.ReadUInt32());
            byte[] ansiBytes = ReadExact(reader, counts[7]);
            byte[] wideBytes = ReadExact(reader, checked(counts[8] * 2));
            NumberlessPair[] numberlessPairs = ReadArray(reader, counts[9], () => new NumberlessPair
            {
                Key = reader.ReadUInt32(),
                Value = reader.ReadUInt32()
            });
            NumberedPair[] pairs = ReadArray(reader, counts[10], () => new NumberedPair
            {
                Key = ReadNameRef(reader, names),
                Value = reader.ReadUInt32()
            });

            if (reader.ReadUInt32() != StoreEndMagic) throw new InvalidDataException("Invalid FStore EndMagic value.");
            return new StoreData
            {
                NameTable = names,
                NumberlessNames = numberlessNames,
                Names = storeNames,
                NumberlessExportPaths = numberlessExportPaths,
                ExportPaths = exportPaths,
                Texts = texts,
                AnsiOffsets = ansiOffsets,
                WideOffsets = wideOffsets,
                AnsiBytes = ansiBytes,
                WideBytes = wideBytes,
                NumberlessPairs = numberlessPairs,
                Pairs = pairs
            };
        }

        private static string ResolveValue(StoreData store, uint id)
        {
            int type = (int)(id & 7);
            int index = (int)(id >> 3);
            switch (type)
            {
                case 0: return ReadNullTerminatedUtf8(store.AnsiBytes, GetIndexed(store.AnsiOffsets, index, "AnsiString"));
                case 1: return ReadNullTerminatedUtf16(store.WideBytes, checked(GetIndexed(store.WideOffsets, index, "WideString") * 2));
                case 2:
                    return GetIndexed(store.NameTable, checked((int)GetIndexed(store.NumberlessNames, index, "NumberlessName")), "Name");
                case 3: return ToText(GetIndexed(store.Names, index, "Name"));
                case 4:
                    {
                        NumberlessExportPath path = GetIndexed(store.NumberlessExportPaths, index, "NumberlessExportPath");
                        string cls = GetIndexed(store.NameTable, checked((int)path.ClassObject), "ClassObject");
                        string obj = GetIndexed(store.NameTable, checked((int)path.Object), "Object");
                        string pkg = GetIndexed(store.NameTable, checked((int)path.Package), "Package");
                        return BuildExportPath(cls, obj, pkg);
                    }
                case 5:
                    {
                        ExportPath path = GetIndexed(store.ExportPaths, index, "ExportPath");
                        return BuildExportPath(ToText(path.Class), ToText(path.Object), ToText(path.Package));
                    }
                case 6: return GetIndexed(store.Texts, index, "LocalizedText");
                default: throw new InvalidDataException("Unknown FStore value type: " + type);
            }
        }

        private static string BuildExportPath(string className, string objectName, string packageName)
        {
            return (className ?? "None") + "'" + (packageName ?? "None") + "." + (objectName ?? "None") + "'";
        }

        private static void ReadDependenciesAndPackages(BinaryReader reader, AssetRegistry registry, string[] nameTable)
        {
            long sectionSize = reader.ReadInt64();
            if (sectionSize < 4 || sectionSize > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new InvalidDataException("Invalid dependency section size.");
            long sectionEnd = reader.BaseStream.Position + sectionSize;
            int dependencyCount = ReadCount(reader, "DependsData", 2_000_000);
            registry.DependsData = new List<DependsNodeData>(dependencyCount);
            for (int i = 0; i < dependencyCount; i++)
            {
                DependsNodeData node = new();
                byte bits = reader.ReadByte();
                if ((bits & 1) != 0) node.Identifier.PackageName = ToText(ReadNameRef(reader, nameTable));
                if ((bits & 2) != 0) node.Identifier.PrimaryAssetType = ToText(ReadNameRef(reader, nameTable));
                if ((bits & 4) != 0) node.Identifier.ObjectName = ToText(ReadNameRef(reader, nameTable));
                if ((bits & 8) != 0) node.Identifier.ValueName = ToText(ReadNameRef(reader, nameTable));

                node.PackageDependencies = ReadIntArray(reader, "PackageDependencies");
                int packageFlagWords = DivideRoundUp(5 * node.PackageDependencies.Count, 32);
                node.PackageFlagsWords = ReadIntValues(reader, packageFlagWords);
                node.NameDependencies = ReadIntArray(reader, "NameDependencies");
                node.ManageDependencies = ReadIntArray(reader, "ManageDependencies");
                int manageFlagWords = DivideRoundUp(node.ManageDependencies.Count, 32);
                node.ManageFlagsWords = ReadIntValues(reader, manageFlagWords);
                node.Referencers = ReadIntArray(reader, "Referencers");
                registry.DependsData.Add(node);
            }
            if (reader.BaseStream.Position > sectionEnd) throw new InvalidDataException("Dependency data exceeds its section size.");
            reader.BaseStream.Position = sectionEnd;

            int packageCount = ReadCount(reader, "PackageData", 2_000_000);
            registry.PackageData = new List<AssetPackageData>(packageCount);
            for (int i = 0; i < packageCount; i++)
            {
                AssetPackageData package = new()
                {
                    PackageName = ToText(ReadNameRef(reader, nameTable)),
                    DiskSize = reader.ReadInt64(),
                    PackageGuid = new Guid(ReadExact(reader, 16)),
                    CookedHash = ReadExact(reader, 16)
                };
                registry.PackageData.Add(package);
            }
        }

        public static byte[] Write(AssetRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (registry.Version != AssetRegistry.SupportedVersion)
                throw new NotSupportedException("Writing is limited to UE4.27 FixedTags format (version 8).");

            List<FAssetData> assets = registry.FAssetDatas ?? new List<FAssetData>();
            List<DependsNodeData> dependencies = registry.DependsData ?? new List<DependsNodeData>();
            List<AssetPackageData> packages = registry.PackageData ?? new List<AssetPackageData>();

            List<string> nameTable = new();
            Dictionary<string, int> nameIndices = new(StringComparer.Ordinal);
            void addName(string name)
            {
                SplitName(name, out string baseName, out uint number);
                if (string.IsNullOrEmpty(baseName)) baseName = "None";
                if (!nameIndices.ContainsKey(baseName))
                {
                    nameIndices.Add(baseName, nameTable.Count);
                    nameTable.Add(baseName);
                }
            }

            addName("None");
            foreach (FAssetData assetValue in assets)
            {
                FAssetData asset = assetValue ?? new FAssetData();
                string objectPath = string.IsNullOrWhiteSpace(asset.ObjectPath)
                    ? (asset.PackageName ?? "None") + "." + (asset.AssetName ?? "None")
                    : asset.ObjectPath;
                addName(objectPath);
                addName(asset.PackagePath);
                addName(asset.AssetClass);
                addName(asset.PackageName);
                addName(asset.AssetName);
                if (asset.TagAndValue != null)
                    foreach (TagValuePair tag in asset.TagAndValue)
                        if (tag != null) addName(tag.Item1);
                if (asset.TaggedAssetBundles != null)
                {
                    foreach (AssetBundleEntry bundle in asset.TaggedAssetBundles)
                    {
                        if (bundle == null) continue;
                        addName(bundle.BundleName);
                        if (bundle.BundleAssets != null)
                            foreach (AssetBundleAsset path in bundle.BundleAssets)
                                if (path != null) addName(path.AssetPathName);
                    }
                }
            }
            foreach (DependsNodeData node in dependencies)
            {
                if (node != null && node.Identifier != null)
                {
                    addName(node.Identifier.PackageName); addName(node.Identifier.PrimaryAssetType);
                    addName(node.Identifier.ObjectName); addName(node.Identifier.ValueName);
                }
            }
            foreach (AssetPackageData package in packages)
                if (package != null) addName(package.PackageName);

            List<string> values = new();
            Dictionary<string, int> valueIndices = new(StringComparer.Ordinal);
            List<NumberedPair> pairs = new();
            List<Tuple<int, int>> ranges = new();
            foreach (FAssetData assetValue in assets)
            {
                FAssetData asset = assetValue ?? new FAssetData();
                int begin = pairs.Count;
                if (asset.TagAndValue != null)
                {
                    foreach (TagValuePair tag in asset.TagAndValue)
                    {
                        if (tag == null) continue;
                        string value = tag.Item2 ?? string.Empty;
                        if (!valueIndices.TryGetValue(value, out int valueIndex))
                        {
                            valueIndex = values.Count;
                            valueIndices.Add(value, valueIndex);
                            values.Add(value);
                        }
                        pairs.Add(new NumberedPair { Key = ParseName(tag.Item1), Value = ((uint)valueIndex << 3) });
                    }
                }
                int count = pairs.Count - begin;
                if (count > ushort.MaxValue)
                    throw new InvalidDataException("An asset cannot contain more than 65,535 tags in FixedTags format.");
                ranges.Add(Tuple.Create(begin, count));
            }

            byte[] ansiBytes;
            uint[] ansiOffsets = new uint[values.Count];
            using (MemoryStream valueStream = new())
            {
                for (int i = 0; i < values.Count; i++)
                {
                    ansiOffsets[i] = checked((uint)valueStream.Position);
                    byte[] encoded = Utf8.GetBytes(values[i]);
                    valueStream.Write(encoded, 0, encoded.Length);
                    valueStream.WriteByte(0);
                }
                ansiBytes = valueStream.ToArray();
            }

            using MemoryStream output = new();
            using BinaryWriter writer = new(output, Utf8, true);
            writer.Write(ExpectedGuidBytes);
            writer.Write(AssetRegistry.SupportedVersion);
            WriteNameBatch(writer, nameTable);
            WriteStore(writer, nameIndices, ansiOffsets, ansiBytes, pairs);
            writer.Write(assets.Count);
            for (int i = 0; i < assets.Count; i++)
            {
                FAssetData asset = assets[i] ?? new FAssetData();
                string objectPath = string.IsNullOrWhiteSpace(asset.ObjectPath)
                    ? (asset.PackageName ?? "None") + "." + (asset.AssetName ?? "None")
                    : asset.ObjectPath;
                WriteName(writer, objectPath, nameIndices);
                WriteName(writer, asset.PackagePath, nameIndices);
                WriteName(writer, asset.AssetClass, nameIndices);
                WriteName(writer, asset.PackageName, nameIndices);
                WriteName(writer, asset.AssetName, nameIndices);

                Tuple<int, int> range = ranges[i];
                writer.Write(((ulong)(uint)range.Item2 << 32) | (uint)range.Item1);

                List<AssetBundleEntry> bundles = asset.TaggedAssetBundles ?? new List<AssetBundleEntry>();
                writer.Write(bundles.Count);
                foreach (AssetBundleEntry bundleValue in bundles)
                {
                    AssetBundleEntry bundle = bundleValue ?? new AssetBundleEntry();
                    WriteName(writer, bundle.BundleName, nameIndices);
                    List<AssetBundleAsset> bundleAssets = bundle.BundleAssets ?? new List<AssetBundleAsset>();
                    writer.Write(bundleAssets.Count);
                    foreach (AssetBundleAsset pathValue in bundleAssets)
                    {
                        AssetBundleAsset path = pathValue ?? new AssetBundleAsset();
                        WriteName(writer, path.AssetPathName, nameIndices);
                        WriteFString(writer, path.SubPathString ?? string.Empty);
                    }
                }

                List<int> chunks = asset.ChunkIDs ?? new List<int>();
                writer.Write(chunks.Count);
                foreach (int chunk in chunks) writer.Write(chunk);
                writer.Write(asset.PackageFlags);
            }

            WriteDependencies(writer, dependencies, nameIndices);
            WritePackages(writer, packages, nameIndices);
            writer.Flush();
            return output.ToArray();
        }

        private static void WriteNameBatch(BinaryWriter writer, List<string> names)
        {
            writer.Write(names.Count);
            uint stringBytes = 0;
            foreach (string name in names) stringBytes = checked(stringBytes + (uint)Utf8.GetByteCount(name));
            writer.Write(stringBytes);
            writer.Write((ulong)0);
            for (int i = 0; i < names.Count; i++) writer.Write((ulong)0);
            foreach (string name in names)
            {
                int length = Utf8.GetByteCount(name);
                if (length > 0x7FFF) throw new InvalidDataException("Name is too long for an FName entry: " + name);
                writer.Write((byte)((length >> 8) & 0x7F));
                writer.Write((byte)(length & 0xFF));
            }
            foreach (string name in names) writer.Write(Utf8.GetBytes(name));
        }

        private static void WriteStore(BinaryWriter writer, Dictionary<string, int> nameIndices, uint[] ansiOffsets, byte[] ansiBytes, List<NumberedPair> pairs)
        {
            writer.Write(StoreBeginMagic);
            int[] counts = { 0, 0, 0, 0, 0, ansiOffsets.Length, 0, ansiBytes.Length, 0, 0, pairs.Count };
            foreach (int count in counts) writer.Write(count);
            writer.Write((uint)0);
            foreach (uint offset in ansiOffsets) writer.Write(offset);
            writer.Write(ansiBytes);
            foreach (NumberedPair pair in pairs)
            {
                WriteName(writer, ToText(pair.Key), nameIndices);
                writer.Write(pair.Value);
            }
            writer.Write(StoreEndMagic);
        }

        private static void WriteDependencies(BinaryWriter writer, List<DependsNodeData> dependencies, Dictionary<string, int> nameIndices)
        {
            using MemoryStream section = new();
            using BinaryWriter sectionWriter = new(section, Utf8, true);
            sectionWriter.Write(dependencies.Count);
            foreach (DependsNodeData nodeValue in dependencies)
            {
                DependsNodeData node = nodeValue ?? new DependsNodeData();
                AssetIdentifierData identifier = node.Identifier ?? new AssetIdentifierData();
                byte flags = 0;
                if (!string.IsNullOrEmpty(identifier.PackageName)) flags |= 1;
                if (!string.IsNullOrEmpty(identifier.PrimaryAssetType)) flags |= 2;
                if (!string.IsNullOrEmpty(identifier.ObjectName)) flags |= 4;
                if (!string.IsNullOrEmpty(identifier.ValueName)) flags |= 8;
                sectionWriter.Write(flags);
                if ((flags & 1) != 0) WriteName(sectionWriter, identifier.PackageName, nameIndices);
                if ((flags & 2) != 0) WriteName(sectionWriter, identifier.PrimaryAssetType, nameIndices);
                if ((flags & 4) != 0) WriteName(sectionWriter, identifier.ObjectName, nameIndices);
                if ((flags & 8) != 0) WriteName(sectionWriter, identifier.ValueName, nameIndices);

                WriteIntArray(sectionWriter, node.PackageDependencies);
                int packageFlagWords = DivideRoundUp(5 * (node.PackageDependencies?.Count ?? 0), 32);
                WriteFixedIntArray(sectionWriter, node.PackageFlagsWords, packageFlagWords);
                WriteIntArray(sectionWriter, node.NameDependencies);
                WriteIntArray(sectionWriter, node.ManageDependencies);
                int manageFlagWords = DivideRoundUp(node.ManageDependencies?.Count ?? 0, 32);
                WriteFixedIntArray(sectionWriter, node.ManageFlagsWords, manageFlagWords);
                WriteIntArray(sectionWriter, node.Referencers);
            }
            sectionWriter.Flush();
            writer.Write(section.Length);
            section.Position = 0;
            section.CopyTo(writer.BaseStream);
        }

        private static void WritePackages(BinaryWriter writer, List<AssetPackageData> packages, Dictionary<string, int> nameIndices)
        {
            writer.Write(packages.Count);
            foreach (AssetPackageData packageValue in packages)
            {
                AssetPackageData package = packageValue ?? new AssetPackageData();
                WriteName(writer, package.PackageName, nameIndices);
                writer.Write(package.DiskSize);
                writer.Write(package.PackageGuid.ToByteArray());
                WriteFixedBytes(writer, package.CookedHash, 16);
            }
        }

        private static void WriteIntArray(BinaryWriter writer, List<int> values)
        {
            values ??= new List<int>();
            writer.Write(values.Count);
            foreach (int value in values) writer.Write(value);
        }

        private static void WriteFixedIntArray(BinaryWriter writer, List<int> values, int expectedCount)
        {
            values ??= new List<int>();
            for (int i = 0; i < expectedCount; i++) writer.Write(i < values.Count ? values[i] : 0);
        }

        private static void WriteFixedBytes(BinaryWriter writer, byte[] value, int count)
        {
            if (value == null || value.Length != count) writer.Write(new byte[count]);
            else writer.Write(value);
        }

        private static void WriteName(BinaryWriter writer, string rawName, Dictionary<string, int> nameIndices)
        {
            SplitName(rawName, out string baseName, out uint number);
            if (string.IsNullOrEmpty(baseName)) baseName = "None";
            int index = 0;
            if (nameIndices != null && !nameIndices.TryGetValue(baseName, out index))
                throw new InvalidDataException("Name is missing from the FName table: " + baseName);
            if (number != 0)
            {
                writer.Write((uint)index | NumberedNameBit);
                writer.Write(number);
            }
            else writer.Write((uint)index);
        }

        private static NameRef ReadNameRef(BinaryReader reader, string[] names)
        {
            uint index = reader.ReadUInt32();
            uint number = 0;
            if ((index & NumberedNameBit) != 0)
            {
                index -= NumberedNameBit;
                number = reader.ReadUInt32();
            }
            if (names == null)
                return new NameRef { Name = null, Number = number };
            if (index >= (uint)names.Length) throw new InvalidDataException("FName index is out of range: " + index);
            return new NameRef { Name = names[index], Number = number };
        }

        private static NameRef ParseName(string raw)
        {
            SplitName(raw, out string name, out uint number);
            return new NameRef { Name = string.IsNullOrEmpty(name) ? "None" : name, Number = number };
        }

        private static void SplitName(string raw, out string name, out uint number)
        {
            name = raw ?? string.Empty;
            number = 0;
            int separator = name.LastIndexOf('_');
            if (separator <= 0 || separator == name.Length - 1) return;
            if (uint.TryParse(name.Substring(separator + 1), out uint suffix) && suffix < uint.MaxValue)
            {
                name = name.Substring(0, separator);
                number = suffix + 1;
            }
        }

        private static string ToText(NameRef name)
        {
            if (name == null || string.IsNullOrEmpty(name.Name)) return "None";
            return name.Number == 0 ? name.Name : name.Name + "_" + (name.Number - 1).ToString();
        }

        private static string ReadNullTerminatedUtf8(byte[] data, uint offset)
        {
            int start = checked((int)offset);
            if (start < 0 || start >= data.Length) throw new InvalidDataException("ANSI offset is out of range.");
            int end = start;
            while (end < data.Length && data[end] != 0) end++;
            if (end == data.Length) throw new InvalidDataException("Unterminated ANSI string.");
            return Utf8.GetString(data, start, end - start);
        }

        private static string ReadNullTerminatedUtf16(byte[] data, uint offset)
        {
            int start = checked((int)offset);
            if (start < 0 || start > data.Length - 2 || (start & 1) != 0) throw new InvalidDataException("Unicode offset is out of range.");
            int end = start;
            while (end + 1 < data.Length && (data[end] != 0 || data[end + 1] != 0)) end += 2;
            if (end + 1 >= data.Length) throw new InvalidDataException("Unterminated Unicode string.");
            return Utf16.GetString(data, start, end - start);
        }

        private static string ReadFString(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length == 0) return string.Empty;
            if (length == int.MinValue) throw new InvalidDataException("Invalid FString.");
            if (length > 0)
            {
                byte[] bytes = ReadExact(reader, length);
                int contentLength = bytes.Length > 0 && bytes[bytes.Length - 1] == 0 ? bytes.Length - 1 : bytes.Length;
                return Utf8.GetString(bytes, 0, contentLength);
            }
            int charCount = -length;
            byte[] wide = ReadExact(reader, checked(charCount * 2));
            int byteCount = wide.Length >= 2 && wide[wide.Length - 1] == 0 && wide[wide.Length - 2] == 0 ? wide.Length - 2 : wide.Length;
            return Utf16.GetString(wide, 0, byteCount);
        }

        private static void WriteFString(BinaryWriter writer, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                writer.Write(0);
                return;
            }
            bool useUnicode = value.Any(c => c > 0x7F);
            if (useUnicode)
            {
                byte[] bytes = Utf16.GetBytes(value + "\0");
                writer.Write(-((bytes.Length) / 2));
                writer.Write(bytes);
            }
            else
            {
                byte[] bytes = Encoding.ASCII.GetBytes(value + "\0");
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }

        private static int ReadCount(BinaryReader reader, string label, int maximum)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > maximum) throw new InvalidDataException("Invalid count for " + label + ": " + count);
            return count;
        }

        private static T[] ReadArray<T>(BinaryReader reader, int count, Func<T> read)
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            T[] result = new T[count];
            for (int i = 0; i < count; i++) result[i] = read();
            return result;
        }

        private static List<int> ReadIntArray(BinaryReader reader, string label)
        {
            int count = ReadCount(reader, label, 5_000_000);
            return ReadIntValues(reader, count);
        }

        private static List<int> ReadIntValues(BinaryReader reader, int count)
        {
            List<int> values = new(count);
            for (int i = 0; i < count; i++) values.Add(reader.ReadInt32());
            return values;
        }

        private static T GetIndexed<T>(T[] values, int index, string label)
        {
            if (index < 0 || index >= values.Length) throw new InvalidDataException("Index " + label + " is out of range: " + index);
            return values[index];
        }

        private static uint GetIndexed(uint[] values, int index, string label)
        {
            if (index < 0 || index >= values.Length) throw new InvalidDataException("Index " + label + " is out of range: " + index);
            return values[index];
        }

        private static string GetIndexed(string[] values, int index, string label)
        {
            if (index < 0 || index >= values.Length) throw new InvalidDataException("Index " + label + " is out of range: " + index);
            return values[index];
        }

        private static void SkipBytes(BinaryReader reader, long count)
        {
            if (count < 0 || count > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new InvalidDataException("Truncated binary block.");
            reader.BaseStream.Position += count;
        }

        private static byte[] ReadExact(BinaryReader reader, int count)
        {
            if (count < 0 || count > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new InvalidDataException("Truncated binary block.");
            byte[] bytes = reader.ReadBytes(count);
            if (bytes.Length != count) throw new EndOfStreamException();
            return bytes;
        }

        private static int DivideRoundUp(int value, int divisor)
        {
            return value == 0 ? 0 : (value + divisor - 1) / divisor;
        }
    }
}
