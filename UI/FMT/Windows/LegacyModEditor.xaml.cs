using FMT;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FIFAModdingUI.Windows
{
    /// <summary>
    /// Interaction logic for LegacyModEditor.xaml
    /// </summary>
    public partial class LegacyModEditor : Window
    {
        public LegacyModEditor()
        {
            InitializeComponent();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            new MainWindow().Show();
        }

        private void btnBrowseLegacyDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    txtLegacyDirectory.Text = dialog.SelectedPath;
                    var allFiles = Directory.GetFiles(dialog.SelectedPath, "*.*", SearchOption.AllDirectories);
                    lstFilesToInclude.ItemsSource = allFiles;
                }
            }
        }

        public static void RecursiveDelete(DirectoryInfo baseDir)
        {
            try
            {
                if (!baseDir.Exists)
                    return;

                foreach (var dir in baseDir.EnumerateDirectories())
                {
                    RecursiveDelete(dir);
                }
                baseDir.Delete(true);
            }
            catch (Exception)
            {

            }
        }

        private void SaveToCompressedFile_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtLegacyDirectory.Text))
            {
                DirectoryInfo directoryInfoTemp = new DirectoryInfo("TEMP");
                if (directoryInfoTemp.Exists)
                {
                    RecursiveDelete(directoryInfoTemp);
                }

                DirectoryCopy(txtLegacyDirectory.Text, "TEMP", true);
                List<string> listOfCompilableFiles = new List<string>();


                bool encryptFiles = chkEncryptFiles.IsChecked.HasValue
                            && chkEncryptFiles.IsChecked.Value;

                var allFiles = Directory.GetFiles("TEMP", "*.*", SearchOption.AllDirectories).Where(x => !x.Contains(".mod"));
                Task[] tasks = new Task[allFiles.Count()];
                //var index = 0;
                foreach (var file in allFiles)
                {
                    //tasks[index] = Task.Run(() =>
                    //{
                    StringBuilder sbFinalResult = new StringBuilder();

                    var encrypt = !file.Contains(".dds")
                        && !file.Contains(".db")
                       && encryptFiles;

                    if (encrypt)
                    {
                        var splitExtension = file.Split('.');
                        if (splitExtension[splitExtension.Length - 1] != "mod")
                            splitExtension[splitExtension.Length - 1] = "mod";

                        foreach (var str in splitExtension)
                        {
                            if (str == "mod")
                            {
                                sbFinalResult.Append(".mod");
                            }
                            else
                            {
                                sbFinalResult.Append(str);
                            }
                        }
                    }
                    else
                    {
                        sbFinalResult.Append(file);
                    }

                    //if (encrypt)
                    //{
                    //    Dispatcher.Invoke(() => {
                    //        txtSaveFileStatus.Text = "Encrypting " + file;
                    //    });
                    //    v2k4EncryptionInterop.encryptFile(file, sbFinalResult.ToString());
                    //    File.Delete(file);
                    //}

                    listOfCompilableFiles.Add(sbFinalResult.ToString());
                    //});
                    //index++;
                }

                //Task.WaitAll(tasks);
                txtSaveFileStatus.Text = "Compilation Complete";


                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Legacy Mod Files|.lmod";
                var saveFileDialogResult = saveFileDialog.ShowDialog();
                if (saveFileDialogResult.HasValue && saveFileDialogResult.Value)
                {
                    if (File.Exists(saveFileDialog.FileName))
                        File.Delete(saveFileDialog.FileName);

                    using (ZipArchive zipArchive = new ZipArchive(new FileStream(saveFileDialog.FileName, FileMode.OpenOrCreate), ZipArchiveMode.Create))
                    {
                        foreach (var compatFile in listOfCompilableFiles)
                        {
                            zipArchive.CreateEntryFromFile(compatFile, compatFile.Replace("TEMP\\", ""));
                        }
                    }
                    txtSaveFileStatus.Text = "Legacy Mod file saved";

                }
            }
        }


        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = System.IO.Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = System.IO.Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
    }
}
