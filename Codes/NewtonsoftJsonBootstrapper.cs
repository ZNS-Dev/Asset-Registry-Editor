using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Codes;

internal static class NewtonsoftJsonBootstrapper
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/JamesNK/Newtonsoft.Json/releases/latest";
    private const string NuGetPackageUrl = "https://www.nuget.org/api/v2/package/Newtonsoft.Json/{0}";
    private const int ApiTimeoutMilliseconds = 8000;
    private const int DownloadTimeoutMilliseconds = 20000;
    private const long MaximumApiResponseBytes = 1024 * 1024;
    private const long MaximumPackageBytes = 25 * 1024 * 1024;

    internal static bool EnsureAvailable()
    {
        RegisterAssemblyResolver();

        string applicationDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string dllPath = Path.Combine(applicationDirectory, "Newtonsoft.Json.dll");
        bool dllExists = IsValidNewtonsoftAssembly(dllPath);
        Version installedVersion = dllExists ? GetInstalledVersion(dllPath) : null;

        Version latestVersion;
        try
        {
            latestVersion = GetLatestStableVersion();
        }
        catch (Exception ex)
        {
            if (dllExists)
                return true;

            ShowStartupError("Newtonsoft.Json.dll is missing and the latest stable version could not be checked. The application cannot start.\n\n" + ex.Message);
            return false;
        }

        if (dllExists && installedVersion != null && ComparePackageVersions(installedVersion, latestVersion) >= 0)
            return true;

        try
        {
            InstallVersion(latestVersion, dllPath, applicationDirectory);
            return true;
        }
        catch (Exception ex)
        {
            if (IsValidNewtonsoftAssembly(dllPath))
            {
                Version fallbackVersion = GetInstalledVersion(dllPath);
                MessageBox.Show(
                    "The Newtonsoft.Json update could not be completed. The application will continue with the currently installed version.\n\nInstalled version: " + (fallbackVersion == null ? "Unknown" : fallbackVersion.ToString(3)) + "\nAvailable version: " + latestVersion.ToString(3) + "\n\nDetails: " + ex.Message,
                    "Newtonsoft.Json update",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return true;
            }

            ShowStartupError("Newtonsoft.Json.dll is missing and could not be installed. The application cannot start.\n\n" + ex.Message);
            return false;
        }
    }

    private static void RegisterAssemblyResolver() => AppDomain.CurrentDomain.AssemblyResolve += ResolveNewtonsoftJson;

    private static Assembly ResolveNewtonsoftJson(object sender, ResolveEventArgs args)
    {
        AssemblyName requestedName;
        try
        {
            requestedName = new AssemblyName(args.Name);
        }
        catch
        {
            return null;
        }

        if (!string.Equals(requestedName.Name, "Newtonsoft.Json", StringComparison.OrdinalIgnoreCase))
            return null;

        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Newtonsoft.Json.dll");
        return File.Exists(path) ? Assembly.LoadFrom(path) : null;
    }

    private static Version GetLatestStableVersion()
    {
        string response = DownloadText(LatestReleaseUrl, ApiTimeoutMilliseconds, MaximumApiResponseBytes, true);
        Match match = Regex.Match(response, "\\\"tag_name\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase);
        if (!match.Success)
            throw new InvalidDataException("GitHub did not return a valid release tag.");

        string tag = match.Groups[1].Value.Trim();
        if (tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            tag = tag.Substring(1);

        if (!Version.TryParse(tag, out Version version) || version.Major < 1 || version.Minor < 0 || version.Build < 0)
            throw new InvalidDataException("The latest GitHub release tag is not a valid stable version: " + tag);

        return new Version(version.Major, version.Minor, version.Build);
    }

    private static bool IsValidNewtonsoftAssembly(string dllPath)
    {
        if (!File.Exists(dllPath))
            return false;

        try
        {
            AssemblyName assemblyName = AssemblyName.GetAssemblyName(dllPath);
            return string.Equals(assemblyName.Name, "Newtonsoft.Json", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static Version GetInstalledVersion(string dllPath)
    {
        try
        {
            FileVersionInfo information = FileVersionInfo.GetVersionInfo(dllPath);
            Version version = ParseVersion(information.ProductVersion) ?? ParseVersion(information.FileVersion);
            if (version != null)
                return version;

            string versionPath = Path.Combine(Path.GetDirectoryName(dllPath), "Newtonsoft.Json.version");
            if (File.Exists(versionPath))
                return ParseVersion(File.ReadAllText(versionPath));

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static Version ParseVersion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        Match match = Regex.Match(value, "(?<![0-9])([0-9]+)\\.([0-9]+)\\.([0-9]+)(?:\\.([0-9]+))?");
        if (!match.Success)
            return null;

        if (!int.TryParse(match.Groups[1].Value, out int major) ||
            !int.TryParse(match.Groups[2].Value, out int minor) ||
            !int.TryParse(match.Groups[3].Value, out int build))
            return null;

        return new Version(major, minor, build);
    }

    private static int ComparePackageVersions(Version left, Version right)
    {
        int result = left.Major.CompareTo(right.Major);
        if (result != 0)
            return result;

        result = left.Minor.CompareTo(right.Minor);
        return result != 0 ? result : left.Build.CompareTo(right.Build);
    }

    private static void InstallVersion(Version version, string dllPath, string applicationDirectory)
    {
        string packagePath = Path.Combine(Path.GetTempPath(), "Newtonsoft.Json." + version.ToString(3) + "." + Guid.NewGuid().ToString("N") + ".nupkg");
        string stagedDllPath = Path.Combine(applicationDirectory, "Newtonsoft.Json.dll.new");
        string backupDllPath = Path.Combine(applicationDirectory, "Newtonsoft.Json.dll.bak");

        try
        {
            DownloadFile(string.Format(NuGetPackageUrl, version.ToString(3)), packagePath, DownloadTimeoutMilliseconds, MaximumPackageBytes);
            ExtractFrameworkAssembly(packagePath, stagedDllPath, version);
            ReplaceAssembly(stagedDllPath, dllPath, backupDllPath);

            try
            {
                string versionPath = Path.Combine(applicationDirectory, "Newtonsoft.Json.version");
                File.WriteAllText(versionPath, version.ToString(3), new UTF8Encoding(false));
            }
            catch
            {
            }
        }
        finally
        {
            TryDeleteFile(packagePath);
            TryDeleteFile(stagedDllPath);
        }
    }

    private static void ExtractFrameworkAssembly(string packagePath, string stagedDllPath, Version requestedVersion)
    {
        TryDeleteFile(stagedDllPath);

        using (FileStream packageStream = File.OpenRead(packagePath))
        using (ZipArchive archive = new(packageStream, ZipArchiveMode.Read, false))
        {
            ZipArchiveEntry nuspecEntry = archive.Entries.FirstOrDefault(entry => entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidDataException("The downloaded NuGet package does not contain its package specification.");
            Version packageVersion;
            using (Stream nuspecStream = nuspecEntry.Open())
            {
                XDocument nuspec = XDocument.Load(nuspecStream);
                XElement metadata = nuspec.Descendants().FirstOrDefault(element => element.Name.LocalName == "metadata");
                XElement idElement = metadata?.Elements().FirstOrDefault(element => element.Name.LocalName == "id");
                XElement versionElement = metadata?.Elements().FirstOrDefault(element => element.Name.LocalName == "version");

                if (idElement == null || !string.Equals(idElement.Value.Trim(), "Newtonsoft.Json", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The downloaded NuGet package is not Newtonsoft.Json.");

                if (versionElement == null || !Version.TryParse(versionElement.Value.Trim(), out packageVersion))
                    throw new InvalidDataException("The downloaded NuGet package contains an invalid version number.");
            }

            if (ComparePackageVersions(packageVersion, requestedVersion) != 0)
                throw new InvalidDataException("The downloaded NuGet package version does not match the GitHub release version.");

            ZipArchiveEntry assemblyEntry = archive.GetEntry("lib/net45/Newtonsoft.Json.dll") ?? throw new InvalidDataException("The NuGet package does not contain the .NET Framework 4.5 assembly.");
            using Stream source = assemblyEntry.Open();
            using FileStream destination = new(stagedDllPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            source.CopyTo(destination);
        }

        AssemblyName assemblyName = AssemblyName.GetAssemblyName(stagedDllPath);
        if (!string.Equals(assemblyName.Name, "Newtonsoft.Json", StringComparison.OrdinalIgnoreCase))
        {
            TryDeleteFile(stagedDllPath);
            throw new InvalidDataException("The extracted file is not a valid Newtonsoft.Json assembly.");
        }

        FileInfo stagedFile = new(stagedDllPath);
        if (!stagedFile.Exists || stagedFile.Length < 100000)
        {
            TryDeleteFile(stagedDllPath);
            throw new InvalidDataException("The extracted Newtonsoft.Json assembly is incomplete.");
        }
    }

    private static void ReplaceAssembly(string stagedDllPath, string dllPath, string backupDllPath)
    {
        TryDeleteFile(backupDllPath);

        if (!File.Exists(dllPath))
        {
            File.Move(stagedDllPath, dllPath);
            return;
        }

        try
        {
            File.Replace(stagedDllPath, dllPath, backupDllPath);
            TryDeleteFile(backupDllPath);
            return;
        }
        catch
        {
            if (!File.Exists(dllPath))
                throw;
        }

        TryDeleteFile(backupDllPath);
        File.Move(dllPath, backupDllPath);
        try
        {
            File.Move(stagedDllPath, dllPath);
            TryDeleteFile(backupDllPath);
        }
        catch
        {
            if (!File.Exists(dllPath) && File.Exists(backupDllPath))
                File.Move(backupDllPath, dllPath);
            throw;
        }
    }

    private static string DownloadText(string url, int timeoutMilliseconds, long maximumBytes, bool useGitHubHeaders)
    {
        HttpWebRequest request = CreateRequest(url, timeoutMilliseconds, useGitHubHeaders);
        using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        if (response.ContentLength > maximumBytes)
            throw new InvalidDataException("The server response is larger than expected.");

        using Stream input = response.GetResponseStream();
        using MemoryStream output = new();
        CopyWithLimit(input, output, maximumBytes);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private static void DownloadFile(string url, string destinationPath, int timeoutMilliseconds, long maximumBytes)
    {
        HttpWebRequest request = CreateRequest(url, timeoutMilliseconds, false);
        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        {
            if (response.ContentLength > maximumBytes)
                throw new InvalidDataException("The NuGet package is larger than expected.");

            using Stream input = response.GetResponseStream();
            using FileStream output = new(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            CopyWithLimit(input, output, maximumBytes);
        }

        FileInfo downloadedFile = new(destinationPath);
        if (!downloadedFile.Exists || downloadedFile.Length == 0)
            throw new InvalidDataException("The NuGet package download is empty.");
    }

    private static HttpWebRequest CreateRequest(string url, int timeoutMilliseconds, bool useGitHubHeaders)
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.UserAgent = "AssetRegistryEditor/1.0";
        request.Timeout = timeoutMilliseconds;
        request.ReadWriteTimeout = timeoutMilliseconds;
        request.AllowAutoRedirect = true;
        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        request.KeepAlive = false;

        if (useGitHubHeaders)
        {
            request.Accept = "application/vnd.github+json";
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        }

        return request;
    }

    private static void CopyWithLimit(Stream input, Stream output, long maximumBytes)
    {
        byte[] buffer = new byte[81920];
        long totalBytes = 0;
        int bytesRead;

        while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            totalBytes += bytesRead;
            if (totalBytes > maximumBytes)
                throw new InvalidDataException("The downloaded file is larger than expected.");

            output.Write(buffer, 0, bytesRead);
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private static void ShowStartupError(string message)
    {
        MessageBox.Show(message, "Asset Registry Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
