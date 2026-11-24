using AssetRegistry;
using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Codes;

internal class Fonctions
{
    public static void SelectBinFile(out string BinFilePath, string dialogTitle)
    {
        BinFilePath = null;
        using OpenFileDialog openFileDialog = new();
        openFileDialog.InitialDirectory = "C:\\";
        openFileDialog.Title = dialogTitle;
        openFileDialog.Filter = "Binary Files (*.bin)|*.bin|All Files (*.*)|*.*";
        openFileDialog.CheckFileExists = true;
        openFileDialog.Multiselect = false;
        openFileDialog.RestoreDirectory = true;
        if (openFileDialog.ShowDialog() == DialogResult.OK)
            BinFilePath = openFileDialog.FileName;
    }

    public static void SelectDir(out string DirFilePath, string dialogDescription)
    {
        DirFilePath = null;
        IFileDialog folderDialog = null;
        IShellItem selectedFolder = null;

        try
        {
            folderDialog = (IFileDialog)new FileOpenDialogComObject();

            folderDialog.GetOptions(out FileOpenDialogOptions options);
            options |= FileOpenDialogOptions.PickFolders
                | FileOpenDialogOptions.ForceFileSystem
                | FileOpenDialogOptions.PathMustExist
                | FileOpenDialogOptions.NoChangeDirectory;
            folderDialog.SetOptions(options);
            folderDialog.SetTitle(string.IsNullOrWhiteSpace(dialogDescription) ? "Select a folder" : dialogDescription);

            IntPtr ownerHandle = Form.ActiveForm != null ? Form.ActiveForm.Handle : IntPtr.Zero;
            int result = folderDialog.Show(ownerHandle);
            const int ERROR_CANCELLED = unchecked((int)0x800704C7);
            if (result == ERROR_CANCELLED)
                return;

            System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(result);
            folderDialog.GetResult(out selectedFolder);

            IntPtr pathPointer = IntPtr.Zero;
            try
            {
                selectedFolder.GetDisplayName(ShellItemDisplayName.FileSystemPath, out pathPointer);
                DirFilePath = System.Runtime.InteropServices.Marshal.PtrToStringUni(pathPointer);
            }
            finally
            {
                if (pathPointer != IntPtr.Zero)
                    System.Runtime.InteropServices.Marshal.FreeCoTaskMem(pathPointer);
            }
        }
        finally
        {
            if (selectedFolder != null && System.Runtime.InteropServices.Marshal.IsComObject(selectedFolder))
                System.Runtime.InteropServices.Marshal.ReleaseComObject(selectedFolder);
            if (folderDialog != null && System.Runtime.InteropServices.Marshal.IsComObject(folderDialog))
                System.Runtime.InteropServices.Marshal.ReleaseComObject(folderDialog);
        }
    }


    public static void DumpAllAssetRegistry(string AssetRegistryPath, string OutPutFilePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(AssetRegistryPath) || !File.Exists(AssetRegistryPath))
                throw new FileNotFoundException("The source AssetRegistry file could not be found.", AssetRegistryPath);

            AssetRegistryDocument document = AssetRegistryDocument.Read(File.ReadAllBytes(AssetRegistryPath));
            document.ExportJsonFolder(OutPutFilePath);
            ShowCompletionMessage("Extraction completed!");
        }
        catch (Exception ex)
        {
            ShowOperationError("Extraction AssetRegistry", ex);
        }
    }

    public static void ReadAssetRegistryFile(string FileToRead, out string OutJson)
    {
        OutJson = null;
        try
        {
            if (string.IsNullOrWhiteSpace(FileToRead) || !File.Exists(FileToRead))
                throw new FileNotFoundException("The AssetRegistry file to read could not be found.", FileToRead);

            AssetRegistryDocument document = AssetRegistryDocument.Read(File.ReadAllBytes(FileToRead));
            OutJson = document.ToJson();
        }
        catch (Exception ex)
        {
            ShowOperationError("Read AssetRegistry", ex);
        }
    }

    public static void SaveAssetRegistryJson(string FileToSave, string JsonToSave)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(FileToSave))
                FileToSave = "AssetRegistry.bin";
            File.WriteAllText(Path.ChangeExtension(FileToSave, ".json_NewFile"), JsonToSave ?? string.Empty);
        }
        catch (Exception ex)
        {
            ShowOperationError("Save JSON", ex);
        }
    }

    public static void SaveAssetRegistryBin(string FileNameOut, string JsonToBin)
    {
        try
        {
            byte[] bytes;
            if (!string.IsNullOrWhiteSpace(FileNameOut) && File.Exists(FileNameOut))
            {
                AssetRegistryDocument sourceDocument = AssetRegistryDocument.Read(File.ReadAllBytes(FileNameOut));
                bytes = sourceDocument.MakeFromJson(JsonToBin);
            }
            else
            {
                bytes = AssetRegistryDocument.MakeFromJsonAuto(JsonToBin);
            }

            string outputBase = string.IsNullOrWhiteSpace(FileNameOut) ? "AssetRegistry.bin" : FileNameOut;
            string outputPath = Path.ChangeExtension(outputBase, ".bin_NewFile");
            File.WriteAllBytes(outputPath, bytes);
            ShowCompletionMessage("Rebuild completed!\r\n\r\nFile created at:\r\n" + outputPath);
        }
        catch (Exception ex)
        {
            ShowOperationError("Rebuild AssetRegistry", ex);
        }
    }

    private static void ShowCompletionMessage(string message)
    {
        MessageBox.Show(
            message,
            "Asset Registry Editor",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void ShowOperationError(string operation, Exception exception)
    {
        string details = FormatExceptionDetails(exception);
        MessageBox.Show(operation + ":\r\n" + details, "Asset Registry Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static string FormatExceptionDetails(Exception exception)
    {
        if (exception == null) return "Unknown error.";

        if (exception is AggregateException aggregate)
        {
            string[] messages = new string[aggregate.InnerExceptions.Count];
            for (int i = 0; i < aggregate.InnerExceptions.Count; i++)
                messages[i] = FormatExceptionDetails(aggregate.InnerExceptions[i]);
            return exception.Message + "\r\n" + string.Join("\r\n", messages);
        }

        if (exception.InnerException != null)
            return exception.Message + "\r\n" + FormatExceptionDetails(exception.InnerException);

        return exception.Message;
    }

    public static void RegisterFileAssociation(string extension, string progId, string description)
    {
        using (RegistryKey registryKey = Registry.ClassesRoot.CreateSubKey(extension))
        {
            registryKey.SetValue("", progId);
        }

        using RegistryKey registryKey2 = Registry.ClassesRoot.CreateSubKey(progId);
        registryKey2.SetValue("", description);
        using RegistryKey registryKey3 = registryKey2.CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command");
        registryKey3.SetValue("", "\"" + Assembly.GetExecutingAssembly().Location + "\" \"%1\"");
    }
}

[System.Runtime.InteropServices.ComImport]
[System.Runtime.InteropServices.Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
[System.Runtime.InteropServices.ClassInterface(System.Runtime.InteropServices.ClassInterfaceType.None)]
internal class FileOpenDialogComObject
{
}

[System.Flags]
internal enum FileOpenDialogOptions : uint
{
    NoChangeDirectory = 0x00000008,
    PickFolders = 0x00000020,
    ForceFileSystem = 0x00000040,
    PathMustExist = 0x00000800
}

[System.Runtime.InteropServices.ComImport]
[System.Runtime.InteropServices.Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
[System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileDialog
{
    [System.Runtime.InteropServices.PreserveSig]
    int Show(IntPtr parent);

    void SetFileTypes(uint count, IntPtr filterSpec);
    void SetFileTypeIndex(uint index);
    void GetFileTypeIndex(out uint index);
    void Advise(IntPtr events, out uint cookie);
    void Unadvise(uint cookie);
    void SetOptions(FileOpenDialogOptions options);
    void GetOptions(out FileOpenDialogOptions options);
    void SetDefaultFolder(IShellItem folder);
    void SetFolder(IShellItem folder);
    void GetFolder(out IShellItem folder);
    void GetCurrentSelection(out IShellItem item);
    void SetFileName([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string fileName);
    void GetFileName([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] out string fileName);
    void SetTitle([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string title);
    void SetOkButtonLabel([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string text);
    void SetFileNameLabel([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string label);
    void GetResult(out IShellItem item);
    void AddPlace(IShellItem item, uint alignment);
    void SetDefaultExtension([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string extension);
    void Close(int result);
    void SetClientGuid(ref Guid guid);
    void ClearClientData();
    void SetFilter(IntPtr filter);
}

[System.Runtime.InteropServices.ComImport]
[System.Runtime.InteropServices.Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
[System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItem
{
    void BindToHandler(IntPtr bindContext, ref Guid handlerId, ref Guid interfaceId, out IntPtr result);
    void GetParent(out IShellItem parent);
    void GetDisplayName(ShellItemDisplayName nameType, out IntPtr name);
    void GetAttributes(uint attributeMask, out uint attributes);
    void Compare(IShellItem item, uint hint, out int order);
}

internal enum ShellItemDisplayName : uint
{
    FileSystemPath = 0x80058000
}

