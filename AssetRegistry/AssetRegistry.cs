using System;
using System.Collections.Generic;

namespace AssetRegistry
{

    public sealed class AssetRegistry
    {
        public const int SupportedVersion = 8;
        public const string RegistryGuid = "717F9EE7-E9B0-493A-88B3-91321B388107";

        public int Version { get; set; } = SupportedVersion;
        public bool FilterEditorOnly { get; set; }
        public string Header { get; set; } = RegistryGuid;
        public List<FAssetData> FAssetDatas { get; set; } = new List<FAssetData>();
        public List<DependsNodeData> DependsData { get; set; } = new List<DependsNodeData>();
        public List<AssetPackageData> PackageData { get; set; } = new List<AssetPackageData>();

        public void Read(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            AssetRegistry parsed = AssetRegistryBinary.Read(data);
            Version = parsed.Version;
            FilterEditorOnly = parsed.FilterEditorOnly;
            Header = parsed.Header;
            FAssetDatas = parsed.FAssetDatas;
            DependsData = parsed.DependsData;
            PackageData = parsed.PackageData;
        }

        public byte[] Make()
        {
            return AssetRegistryBinary.Write(this);
        }

    }

    public sealed class FAssetData
    {
        public string ObjectPath { get; set; } = string.Empty;
        public string PackagePath { get; set; } = string.Empty;
        public string AssetClass { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public List<TagValuePair> TagAndValue { get; set; } = new List<TagValuePair>();
        public List<int> ChunkIDs { get; set; } = new List<int>();
        public uint PackageFlags { get; set; }
        public List<AssetBundleEntry> TaggedAssetBundles { get; set; } = new List<AssetBundleEntry>();

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(ObjectPath)
                ? (PackageName ?? string.Empty) + "." + (AssetName ?? string.Empty)
                : ObjectPath;
        }
    }

    public sealed class TagValuePair
    {
        public string Item1 { get; set; } = string.Empty;
        public string Item2 { get; set; } = string.Empty;

        public TagValuePair() { }
        public TagValuePair(string key, string value)
        {
            Item1 = key ?? string.Empty;
            Item2 = value ?? string.Empty;
        }
    }

    public sealed class AssetBundleEntry
    {
        public string BundleName { get; set; } = string.Empty;
        public List<AssetBundleAsset> BundleAssets { get; set; } = new List<AssetBundleAsset>();
    }

    public sealed class AssetBundleAsset
    {
        public string AssetPathName { get; set; } = string.Empty;
        public string SubPathString { get; set; } = string.Empty;
    }

    public sealed class AssetIdentifierData
    {
        public string PackageName { get; set; }
        public string PrimaryAssetType { get; set; }
        public string ObjectName { get; set; }
        public string ValueName { get; set; }
    }

    public sealed class DependsNodeData
    {
        public AssetIdentifierData Identifier { get; set; } = new AssetIdentifierData();
        public List<int> PackageDependencies { get; set; } = new List<int>();
        public List<int> PackageFlagsWords { get; set; } = new List<int>();
        public List<int> NameDependencies { get; set; } = new List<int>();
        public List<int> ManageDependencies { get; set; } = new List<int>();
        public List<int> ManageFlagsWords { get; set; } = new List<int>();
        public List<int> Referencers { get; set; } = new List<int>();
    }

    public sealed class AssetPackageData
    {
        public string PackageName { get; set; } = string.Empty;
        public long DiskSize { get; set; }
        public Guid PackageGuid { get; set; }
        public byte[] CookedHash { get; set; } = new byte[16];
    }
}
