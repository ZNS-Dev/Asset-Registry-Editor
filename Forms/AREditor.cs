using AssetRegistry;
using Codes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Asset_Registry_Editor;

public partial class AREditor : Form
{
    private string FileNameSave;
    private readonly List<AssetListItem> _allAssetItems = new();
    private bool _isRefreshingAssetList;

    public AREditor()
    {
        InitializeComponent();
        RTBassetregistryparsedread.MaxLength = int.MaxValue;
        CLBassets.DisplayMember = nameof(AssetListItem.AssetName);
        ReloadProfiles(null, true);
    }

    private void OpenARInRTB(string filePath)
    {
        FileNameSave = filePath;
        RTBassetregistryparsedread.Text = LoadingRegistryText;
        Fonctions.ReadAssetRegistryFile(filePath, out var OutJson);
        RTBassetregistryparsedread.Text = OutJson ?? string.Empty;
        RefreshAssetList();
    }

    private void AREditor_Load(object sender, EventArgs e) => Fonctions.RegisterFileAssociation(".bin", "Asset Registry Editor", "Unreal Engine Asset Registry file");

    public AREditor(string filePath)
    {
        InitializeComponent();
        RTBassetregistryparsedread.MaxLength = int.MaxValue;
        CLBassets.DisplayMember = nameof(AssetListItem.AssetName);
        ReloadProfiles(null, false);
        OpenARInRTB(filePath);
    }

    // ================== Auto-Update ====================== //
    private string ExtractJsonValue(string json, string key)
    {
        Match match = Regex.Match(
            json,
            $"\"{Regex.Escape(key)}\"\\s*:\\s*\"([^\"]+)\"",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value : null;
    }

    private Version ExtractVersion(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        Match match = Regex.Match(input, @"\d+(\.\d+)+");
        return match.Success && Version.TryParse(match.Value, out Version parsed)
            ? parsed
            : null;
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            string localText = label1 != null ? label1.Text : "";
            Version localVersion = ExtractVersion(localText);

            if (localVersion == null) return;

            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            using HttpClient client = new();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Asset Registry Editor");

            string apiUrl = "https://api.github.com/repos/ZNS-Dev/Asset-Registry-Editor/releases/latest";
            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode) return;

            string json = await response.Content.ReadAsStringAsync();

            string remoteTag = ExtractJsonValue(json, "tag_name");
            string remoteName = ExtractJsonValue(json, "name");
            string downloadUrl = ExtractExeUrlFromAssets(json);

            Version remoteVersion = ExtractVersion(remoteTag) ?? ExtractVersion(remoteName);

            if (remoteVersion == null) return;

            if (remoteVersion > localVersion)
            {

                DialogResult dialog = MessageBox.Show(
                    $"A new version is available!\n\n" +
                    $"• Current version: {localVersion}\n" +
                    $"• New version: {remoteVersion}\n\n" +
                    $"Would you like to download it and restart the application?",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (dialog == DialogResult.Yes)
                {
                    if (string.IsNullOrEmpty(downloadUrl))
                    {
                        MessageBox.Show("Error: No .exe file was found in the Release.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    await ReplaceAndRestartAsync(client, downloadUrl);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Update Error] {ex.Message}");
        }
    }

    private async Task ReplaceAndRestartAsync(HttpClient client, string downloadUrl)
    {
        try
        {
            string currentExePath = Application.ExecutablePath;
            string tempExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update_temp.exe");

            byte[] exeData = await client.GetByteArrayAsync(downloadUrl);
            File.WriteAllBytes(tempExePath, exeData);

            string cmdArgs = $"/c timeout /t 2 /nobreak > nul & move /y \"{tempExePath}\" \"{currentExePath}\" & start \"\" \"{currentExePath}\"";

            ProcessStartInfo psi = new()
            {
                FileName = "cmd.exe",
                Arguments = cmdArgs,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = true
            };

            Process.Start(psi);
            Application.Exit();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to update application: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string ExtractExeUrlFromAssets(string json)
    {
        Match match = Regex.Match(json, @"\""browser_download_url\""\s*:\s*\""([^\""]+\.exe)\""", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    // ======================================================== //

    private void BTNselectarpath3_Click(object sender, EventArgs e)
    {
        Fonctions.SelectBinFile(out var BinFilePath, SelectAssetRegistryDialogTitle);
        FileNameSave = BinFilePath;
        OpenARInRTB(BinFilePath);
    }

    private void BTNsavefiletojson_Click(object sender, EventArgs e)
    {
        FileNameSave ??= "AssetRegistry.bin";
        Fonctions.SaveAssetRegistryJson(FileNameSave, RTBassetregistryparsedread.Text);
    }

    private void BTNsavefiletobin_Click(object sender, EventArgs e)
    {
        FileNameSave ??= "AssetRegistry.bin";
        Fonctions.SaveAssetRegistryBin(FileNameSave, RTBassetregistryparsedread.Text);
    }

    private void BTNcreateprofile_Click(object sender, EventArgs e)
    {
        string profileName = TBprofilename.Text.Trim();
        if (string.IsNullOrWhiteSpace(profileName))
        {
            MessageBox.Show("Enter a profile name first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            TBprofilename.Focus();
            return;
        }

        Fonctions.SelectBinFile(out string originalAssetRegistryPath, "Select the game's original AssetRegistry.bin");
        if (string.IsNullOrWhiteSpace(originalAssetRegistryPath))
            return;

        ProfileInfo profile;
        try
        {
            profile = ProfileManager.CreateProfile(profileName, originalAssetRegistryPath);
            TBprofilename.Clear();
            ReloadProfiles(profile.DirectoryPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Profile creation failed:\r\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        OpenInjectFolder(profile);
    }

    private void BTNopeninjectfolder_Click(object sender, EventArgs e) => OpenInjectFolder(GetSelectedProfile());

    private void OpenInjectFolder(ProfileInfo profile)
    {
        if (profile == null)
            return;

        try
        {
            Process.Start(new ProcessStartInfo(profile.InjectDirectory) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not open the Inject folder:\r\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BTNinjectprofile_Click(object sender, EventArgs e)
    {
        ProfileInfo profile = GetSelectedProfile();
        if (profile == null)
            return;

        try
        {
            ProfileInjectionResult result = ProfileManager.InjectAll(profile);
            MessageBox.Show(
                "Injection completed successfully.\r\n\r\nJSON files injected: " + result.JsonFileCount +
                "\r\nTotal assets in custom registry: " + result.AssetCount +
                "\r\n\r\nCustom AssetRegistry created at:\r\n" + result.OutputPath,
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Profile injection failed:\r\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CBprofiles_SelectedIndexChanged(object sender, EventArgs e) => ApplySelectedProfile(true);

    private ProfileInfo GetSelectedProfile()
    {
        return CBprofiles.SelectedItem as ProfileInfo;
    }

    private void ReloadProfiles(string selectedDirectory, bool loadSelectedProfile = true)
    {
        List<ProfileInfo> profiles = ProfileManager.LoadProfiles();
        CBprofiles.SelectedIndexChanged -= CBprofiles_SelectedIndexChanged;
        CBprofiles.DataSource = null;
        CBprofiles.DisplayMember = nameof(ProfileInfo.Name);
        CBprofiles.DataSource = profiles;
        CBprofiles.SelectedIndex = -1;

        int selectedIndex = -1;
        if (!string.IsNullOrWhiteSpace(selectedDirectory))
        {
            for (int i = 0; i < profiles.Count; i++)
            {
                if (string.Equals(profiles[i].DirectoryPath, selectedDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }
        }
        else
        {
            string lastUsedProfileName = ProfileManager.LoadLastUsedProfileName();
            if (!string.IsNullOrWhiteSpace(lastUsedProfileName))
            {
                for (int i = 0; i < profiles.Count; i++)
                {
                    if (string.Equals(profiles[i].Name, lastUsedProfileName, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }
        }

        if (selectedIndex < 0 && profiles.Count > 0)
            selectedIndex = 0;

        CBprofiles.SelectedIndex = selectedIndex;
        CBprofiles.SelectedIndexChanged += CBprofiles_SelectedIndexChanged;

        ApplySelectedProfile(loadSelectedProfile);
    }

    private void ApplySelectedProfile(bool loadAssetRegistry)
    {
        ProfileInfo profile = GetSelectedProfile();
        UpdateProfileButtons();
        if (profile == null)
            return;

        try
        {
            ProfileManager.SaveLastUsedProfile(profile);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[Profile Persistence Error] " + ex.Message);
        }

        if (!loadAssetRegistry)
            return;

        if (!File.Exists(profile.AssetRegistryPath))
        {
            MessageBox.Show(
                "The selected profile's AssetRegistry.bin could not be found:\r\n" + profile.AssetRegistryPath,
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        string profileAssetRegistryPath = Path.GetFullPath(profile.AssetRegistryPath);
        if (string.IsNullOrWhiteSpace(FileNameSave) ||
            !string.Equals(Path.GetFullPath(FileNameSave), profileAssetRegistryPath, StringComparison.OrdinalIgnoreCase))
        {
            OpenARInRTB(profileAssetRegistryPath);
        }
    }

    private void UpdateProfileButtons()
    {
        bool hasProfile = GetSelectedProfile() != null;
        BTNopeninjectfolder.Enabled = hasProfile;
        BTNinjectprofile.Enabled = hasProfile;
    }

    private void RefreshAssetList()
    {
        _allAssetItems.Clear();
        _isRefreshingAssetList = true;
        CLBassets.BeginUpdate();
        try
        {
            CLBassets.Items.Clear();
            CLBassets.Enabled = false;
            BTNselectallassets.Enabled = false;
            BTNclearassetselection.Enabled = false;
            BTNextractselected.Enabled = false;
            LBLselectedcount.Text = "0 selected";
            TBassetfilter.Text = string.Empty;
        }
        finally
        {
            CLBassets.EndUpdate();
            _isRefreshingAssetList = false;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(RTBassetregistryparsedread.Text))
                return;

            JObject root = JObject.Parse(
                RTBassetregistryparsedread.Text,
                new JsonLoadSettings { LineInfoHandling = LineInfoHandling.Load });
            if (root["FAssetDatas"] is not JArray assets)
                return;

            for (int assetIndex = 0; assetIndex < assets.Count; assetIndex++)
            {
                JToken token = assets[assetIndex];
                if (token is not JObject asset)
                    continue;

                string objectPath = asset.Value<string>("ObjectPath");
                string assetName = asset.Value<string>("AssetName");
                string packageName = asset.Value<string>("PackageName");

                if (string.IsNullOrWhiteSpace(objectPath))
                {
                    if (!string.IsNullOrWhiteSpace(packageName) && !string.IsNullOrWhiteSpace(assetName))
                        objectPath = packageName + "." + assetName;
                    else
                        objectPath = assetName ?? packageName ?? "Unknown asset";
                }

                string displayName = GetAssetDisplayName(assetName, objectPath);
                IJsonLineInfo lineInfo = token as IJsonLineInfo;
                AssetListItem item = new(
                    displayName,
                    objectPath,
                    assetIndex,
                    lineInfo != null && lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0,
                    lineInfo != null && lineInfo.HasLineInfo() ? lineInfo.LinePosition : 0);

                _allAssetItems.Add(item);
            }

            ApplyAssetFilter();
        }
        catch (JsonException)
        {
            _allAssetItems.Clear();
            ApplyAssetFilter();
        }
    }

    private void ApplyAssetFilter()
    {
        if (_isRefreshingAssetList)
            return;

        string filter = TBassetfilter.Text.Trim();
        CLBassets.BeginUpdate();
        _isRefreshingAssetList = true;
        try
        {
            CLBassets.Items.Clear();
            foreach (AssetListItem item in _allAssetItems)
            {
                if (filter.Length == 0 || item.AssetName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    CLBassets.Items.Add(item, item.IsChecked);
            }

            CLBassets.Enabled = CLBassets.Items.Count > 0;
            BTNselectallassets.Enabled = CLBassets.Items.Count > 0;
            BTNclearassetselection.Enabled = _allAssetItems.Count > 0;
        }
        finally
        {
            CLBassets.EndUpdate();
            _isRefreshingAssetList = false;
        }

        UpdateAssetSelectionState();
    }

    private void TBassetfilter_TextChanged(object sender, EventArgs e) => ApplyAssetFilter();

    private void CLBassets_MouseUp(object sender, MouseEventArgs e)
    {
        if (CLBassets.SelectedIndex >= 0)
            CLBassets.SelectedIndex = -1;
    }

    private void CLBassets_ItemCheck(object sender, ItemCheckEventArgs e)
    {
        if (_isRefreshingAssetList || e.Index < 0 || e.Index >= CLBassets.Items.Count)
            return;

        if (CLBassets.Items[e.Index] is AssetListItem item)
            item.IsChecked = e.NewValue == CheckState.Checked;

        UpdateAssetSelectionState();
    }

    private void UpdateAssetSelectionState()
    {
        int checkedCount = 0;
        foreach (AssetListItem item in _allAssetItems)
        {
            if (item.IsChecked)
                checkedCount++;
        }

        LBLselectedcount.Text = checkedCount + " selected";
        BTNextractselected.Enabled = checkedCount > 0;
        BTNclearassetselection.Enabled = _allAssetItems.Count > 0;
        BTNselectallassets.Enabled = CLBassets.Items.Count > 0;
    }

    private void BTNselectallassets_Click(object sender, EventArgs e)
    {
        _isRefreshingAssetList = true;
        try
        {
            for (int i = 0; i < CLBassets.Items.Count; i++)
            {
                if (CLBassets.Items[i] is AssetListItem item)
                    item.IsChecked = true;
                CLBassets.SetItemChecked(i, true);
            }
        }
        finally
        {
            _isRefreshingAssetList = false;
        }

        UpdateAssetSelectionState();
    }

    private void BTNclearassetselection_Click(object sender, EventArgs e)
    {
        _isRefreshingAssetList = true;
        try
        {
            foreach (AssetListItem item in _allAssetItems)
                item.IsChecked = false;

            for (int i = 0; i < CLBassets.Items.Count; i++)
                CLBassets.SetItemChecked(i, false);
        }
        finally
        {
            _isRefreshingAssetList = false;
        }

        UpdateAssetSelectionState();
    }

    private void BTNextractselected_Click(object sender, EventArgs e)
    {
        List<AssetListItem> selectedAssets = new();
        foreach (AssetListItem asset in _allAssetItems)
        {
            if (asset.IsChecked)
                selectedAssets.Add(asset);
        }

        if (selectedAssets.Count == 0)
        {
            MessageBox.Show("Select at least one asset to extract.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Fonctions.SelectDir(out string outputDirectory, SelectOutputFolderTitle);
            if (string.IsNullOrWhiteSpace(outputDirectory))
                return;

            JObject root = JObject.Parse(RTBassetregistryparsedread.Text);
            JArray assets = root["FAssetDatas"] as JArray ?? throw new InvalidDataException("The current JSON does not contain the FAssetDatas array.");
            Directory.CreateDirectory(outputDirectory);
            int exportedCount = 0;
            HashSet<string> usedFileNames = new(StringComparer.OrdinalIgnoreCase);

            foreach (AssetListItem selectedAsset in selectedAssets)
            {
                if (selectedAsset.AssetIndex < 0 || selectedAsset.AssetIndex >= assets.Count)
                    throw new InvalidDataException("The asset list no longer matches the current JSON. Reopen the AssetRegistry file and try again.");

                JToken assetToken = assets[selectedAsset.AssetIndex];
                if (assetToken == null || assetToken.Type != JTokenType.Object)
                    throw new InvalidDataException("An asset selected for extraction is not a valid JSON object.");

                JObject currentAsset = (JObject)assetToken;
                string currentObjectPath = currentAsset.Value<string>("ObjectPath");
                if (!string.IsNullOrWhiteSpace(currentObjectPath) &&
                    !string.Equals(currentObjectPath, selectedAsset.ObjectPath, StringComparison.Ordinal))
                    throw new InvalidDataException("The asset order or identity changed in the JSON editor. Reopen the AssetRegistry file before extracting.");

                string safeName = MakeSafeAssetFileName(selectedAsset.AssetName);
                string fileName = safeName + ".json";
                int duplicateIndex = 2;
                while (!usedFileNames.Add(fileName))
                {
                    fileName = safeName + "_" + duplicateIndex + ".json";
                    duplicateIndex++;
                }

                string outputPath = Path.Combine(outputDirectory, fileName);
                File.WriteAllText(outputPath, assetToken.ToString(Formatting.Indented), new UTF8Encoding(false));
                exportedCount++;
            }

            MessageBox.Show(
                "Extraction completed successfully.\r\n\r\nJSON files created: " + exportedCount +
                "\r\n\r\nOutput folder:\r\n" + outputDirectory,
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Selected asset extraction failed:\r\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string MakeSafeAssetFileName(string value)
    {
        string safeName = string.IsNullOrWhiteSpace(value) ? "Asset" : value;
        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            safeName = safeName.Replace(invalidCharacter, '_');

        safeName = safeName.Replace('/', '_').Replace('\\', '_').Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "Asset";
        if (safeName.Length > 120)
            safeName = safeName.Substring(0, 120);
        return safeName;
    }

    private static string GetAssetDisplayName(string assetName, string objectPath)
    {
        if (!string.IsNullOrWhiteSpace(assetName) &&
            !string.Equals(assetName, "None", StringComparison.OrdinalIgnoreCase))
            return assetName;

        string name = objectPath ?? string.Empty;
        int dotIndex = name.LastIndexOf('.');
        if (dotIndex >= 0 && dotIndex < name.Length - 1)
            name = name.Substring(dotIndex + 1);

        int slashIndex = Math.Max(name.LastIndexOf('/'), name.LastIndexOf('\\'));
        if (slashIndex >= 0 && slashIndex < name.Length - 1)
            name = name.Substring(slashIndex + 1);

        return string.IsNullOrWhiteSpace(name) ? "Unknown asset" : name;
    }

    private sealed class AssetListItem
    {
        public AssetListItem(string assetName, string objectPath, int assetIndex, int lineNumber, int linePosition)
        {
            AssetName = assetName;
            ObjectPath = objectPath;
            AssetIndex = assetIndex;
            LineNumber = lineNumber;
            LinePosition = linePosition;
        }

        public string AssetName { get; private set; }
        public override string ToString() => AssetName;
        public string ObjectPath { get; private set; }
        public int AssetIndex { get; private set; }
        public int LineNumber { get; private set; }
        public int LinePosition { get; private set; }
        public bool IsChecked { get; set; }
    }

    private void TBsearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
            return;

        e.Handled = true;
        e.SuppressKeyPress = true;
        BTNsearch.PerformClick();
    }

    private void BTNsearch_Click(object sender, EventArgs e)
    {
        string text = TBsearch.Text;
        if (string.IsNullOrWhiteSpace(text))
            return;

        int start = RTBassetregistryparsedread.SelectionStart + RTBassetregistryparsedread.SelectionLength;
        int num = RTBassetregistryparsedread.Find(text, start, RichTextBoxFinds.None);
        if (num != -1)
        {
            RTBassetregistryparsedread.SelectionStart = num;
            RTBassetregistryparsedread.SelectionLength = text.Length;
            RTBassetregistryparsedread.Focus();
        }
        else
        {
            MessageBox.Show(SearchNoMatchText, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

}
