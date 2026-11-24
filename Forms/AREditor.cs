using AssetRegistry;
using Codes;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Asset_Registry_Editor;

public partial class AREditor : Form
{
    private string FileNameSave;

    public AREditor()
    {
        InitializeComponent();
        RTBassetregistryparsedread.MaxLength = int.MaxValue;
        ReloadProfiles(null);
    }

    private void OpenARInRTB(string filePath)
    {
        FileNameSave = filePath;
        RTBassetregistryparsedread.Text = LoadingRegistryText;
        Fonctions.ReadAssetRegistryFile(filePath, out var OutJson);
        RTBassetregistryparsedread.Text = OutJson;
    }

    private void AREditor_Load(object sender, EventArgs e) => Fonctions.RegisterFileAssociation(".bin", "Asset Registry Editor", "Unreal Engine Asset Registry file");

    public AREditor(string filePath)
    {
        InitializeComponent();
        RTBassetregistryparsedread.MaxLength = int.MaxValue;
        ReloadProfiles(null);
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

    private void BTNselectarpath2_Click(object sender, EventArgs e)
    {
        Fonctions.SelectBinFile(out var BinFilePath, SelectAssetRegistryDialogTitle);
        TBarpath2.Text = BinFilePath;
    }

    private void BTNdirtoexctractallpath_Click(object sender, EventArgs e)
    {
        Fonctions.SelectDir(out var DirFilePath, SelectOutputFolderTitle);
        TBdirtoexctractallpath.Text = DirFilePath;
    }

    private void BTNextract_Click(object sender, EventArgs e) => Fonctions.DumpAllAssetRegistry(TBarpath2.Text, TBdirtoexctractallpath.Text);

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

    private void CBprofiles_SelectedIndexChanged(object sender, EventArgs e) => UpdateProfileButtons();

    private ProfileInfo GetSelectedProfile()
    {
        return CBprofiles.SelectedItem as ProfileInfo;
    }

    private void ReloadProfiles(string selectedDirectory)
    {
        var profiles = ProfileManager.LoadProfiles();
        CBprofiles.SelectedIndexChanged -= CBprofiles_SelectedIndexChanged;
        CBprofiles.DataSource = null;
        CBprofiles.DisplayMember = nameof(ProfileInfo.Name);
        CBprofiles.DataSource = profiles;
        CBprofiles.SelectedIndex = -1;

        if (!string.IsNullOrWhiteSpace(selectedDirectory))
        {
            for (int i = 0; i < profiles.Count; i++)
            {
                if (string.Equals(profiles[i].DirectoryPath, selectedDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    CBprofiles.SelectedIndex = i;
                    break;
                }
            }
        }
        else if (profiles.Count > 0)
        {
            CBprofiles.SelectedIndex = 0;
        }

        CBprofiles.SelectedIndexChanged += CBprofiles_SelectedIndexChanged;
        UpdateProfileButtons();
    }

    private void UpdateProfileButtons()
    {
        bool hasProfile = GetSelectedProfile() != null;
        BTNopeninjectfolder.Enabled = hasProfile;
        BTNinjectprofile.Enabled = hasProfile;
    }

    private void BTNsearch_Click(object sender, EventArgs e)
    {
        string text = TBsearch.Text;
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
