using AvalonDock.Layout;
using FIFAModdingUI.Pages.Common;
using FMT;
using FMT.Controls.Pages;
using FMT.FileTools;
using FMT.FileTools.Modding;
using FMT.Pages.Common;
using FMT.Windows;
using FolderBrowserEx;
using Frostbite.FileManagers;
using FrostbiteModdingUI.Models;
using FrostbiteModdingUI.Windows;
using FrostbiteSdk;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.Frostbite;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.ModsAndProjects.Projects;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using v2k4FIFAModding.Frosty;
using v2k4FIFAModdingCL;

namespace FIFAModdingUI.Windows
{
    /// <summary>
    /// Interaction logic for FIFA21Editor.xaml
    /// </summary>
    public partial class FIFA21Editor : MetroWindow, IEditorWindow, INotifyPropertyChanged
    {
        public Window OwnerWindow { get; set; }

        public LauncherOptions launcherOptions { get; set; }

        [Obsolete("Incorrect usage of Editor Windows")]
        public FIFA21Editor()
        {
            throw new Exception("Incorrect usage of Editor Windows");
        }

        public FIFA21Editor(Window owner)
        {
            InitializeComponent();
            this.DataContext = this;



            Loaded += FIFA21Editor_Loaded;
            Owner = owner;
        }

        private async void FIFA21Editor_Loaded(object sender, RoutedEventArgs e)
        {

            if (CacheManager.DoesCacheNeedsRebuilding())
            {
                loadingDialog.Update("", "");
                Dispatcher.Invoke(() => {
                    cacheManagerControl.Visibility = Visibility.Visible;
                });
                await cacheManagerControl.Rebuild(forceRebuild: true);
            }
            Dispatcher.Invoke(() => {
                cacheManagerControl.Visibility = Visibility.Collapsed;
            });

            if (!string.IsNullOrEmpty(AppSettings.Settings.GameInstallEXEPath))
            {
                await InitialiseOfSelectedGame(AppSettings.Settings.GameInstallEXEPath);
            }
            else
            {
                var findGameEXEWindow = new FindGameEXEWindow();
                var result = findGameEXEWindow.ShowDialog();
                if (result.HasValue && !string.IsNullOrEmpty(AppSettings.Settings.GameInstallEXEPath))
                {
                    await InitialiseOfSelectedGame(AppSettings.Settings.GameInstallEXEPath);
                }
                else
                {
                    findGameEXEWindow.Close();
                    this.Close();
                }
            }

            launcherOptions = await LauncherOptions.LoadAsync();

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Loaded -= FIFA21Editor_Loaded;
            if (ProjectManagement != null)
            {
                if (ProjectManagement.Project != null && ProjectManagement.Project.IsDirty)
                {
                    if (MessageBox.Show("Your project has been changed. Would you still like to close?", "Project has not been saved", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        EnableEditor();
                        return;
                    }
                }

            }

            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            GameInstanceSingleton.Instance = null;
            ProjectManagement.Instance = null;
            if (AssetManager.Instance != null)
            {
                AssetManager.Instance.Dispose();
                AssetManager.Instance = null;
            }

            GCHelpers.ClearGarbage(true);
            Owner.Visibility = Visibility.Visible;

            base.OnClosed(e);
        }

        public string AdditionalTitle { get; set; }
        public string WindowTitle
        {
            get
            {
                var gv = GameInstanceSingleton.GetGameVersion() ?? "FIFA";
                var initialTitle = $"FMT [{gv}] Editor - {App.ProductVersion} - ";
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(initialTitle);
                stringBuilder.Append("[" + AdditionalTitle + "]");
                //if(!ProfilesLibrary.EnableExecution)
                //{
                //    stringBuilder.Append(" (Read Only)");
                //}
                return stringBuilder.ToString();
            }
        }

        public void UpdateWindowTitle(string additionalText)
        {
            AdditionalTitle = additionalText;
            this.DataContext = null;
            this.DataContext = this;
            this.UpdateLayout();
            DiscordInterop.DiscordRpcClient.UpdateDetails($"In Editor [{ProfileManager.Game}] {AdditionalTitle}");
        }

        public static ProjectManagement ProjectManagement => ProjectManagement.Instance;

        public async Task InitialiseOfSelectedGame(string filePath)
        {
            DisableEditor();
            loadingDialog.Update("Loading Game Files", "");

            

            await GameInstanceSingleton.InitializeSingletonAsync(filePath, true, this);
            GameInstanceSingleton.Logger = this;
            lstProjectFiles.Items.Clear();
            lstProjectFiles.ItemsSource = null;

            loadingDialog.Update("Loading Game Files", "");

            //await Task.Run(
            //    () =>
            //{

            new ProjectManagement(filePath, this);
            //await ProjectManagement.StartNewProjectAsync();
            //InitialiseBrowsers();
            await UpdateAllBrowsersFull();


            await Dispatcher.InvokeAsync(() =>
                {
                    miProject.IsEnabled = true;
                    miMod.IsEnabled = true;
                    //miProjectConverter.IsEnabled = true;
                    miImportKitCreator.IsEnabled = true;

                    //this.DataContext = null;
                    //this.DataContext = this;
                    //this.UpdateLayout();

                });

            this.DataContext = null;
            this.DataContext = this;
            //this.UpdateLayout();
            await loadingDialog.UpdateAsync("", "");

                EnableEditor();

            //});

            await loadingDialog.UpdateAsync("", "");


            DiscordInterop.DiscordRpcClient.UpdateDetails("In Editor [" + GameInstanceSingleton.Instance.GAMEVERSION + "]");

            LauncherOptions = await LauncherOptions.LoadAsync();
            swUseModData.IsEnabled = ProfileManager.LoadedProfile.CanUseModData && !ProfileManager.LoadedProfile.ForceUseModData;
            swUseModData.IsOn = LauncherOptions.UseModData.HasValue ? LauncherOptions.UseModData.Value : true;

            TabFaces.Visibility = !ProfileManager.CanExportMeshes && !ProfileManager.CanImportMeshes ? Visibility.Collapsed : Visibility.Visible;
            TabBoots.Visibility = !ProfileManager.CanExportMeshes && !ProfileManager.CanImportMeshes ? Visibility.Collapsed : Visibility.Visible;

            await Dispatcher.InvokeAsync(() =>
            {
                //swEnableLegacyInjection.Visibility = ProfileManager.CanUseLiveLegacyMods ? Visibility.Visible : Visibility.Collapsed;
                swEnableLegacyInjection.IsOn = LauncherOptions.UseLegacyModSupport.HasValue && LauncherOptions.UseLegacyModSupport.Value && ProfileManager.CanUseLiveLegacyMods;
                swEnableLegacyInjection.IsEnabled = ProfileManager.CanUseLiveLegacyMods;

                btnLaunchFIFAInEditor.IsEnabled = ProfileManager.EnableExecution;

                UpdateWindowTitle("New Project");

            });

            if(ProfileManager.IsLoaded(EGame.FIFA20, EGame.FIFA21))
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    btnCompileLegacyModFromFolder.Visibility = Visibility.Visible;
                    btnCleanUpLegacyFiles.Visibility = Visibility.Visible;
                });
            }

            EnableEditor();

        }



        public static readonly DependencyProperty ProfileSupportsLegacyModsProperty = DependencyProperty.Register(
            "CanUseLiveLegacyMods", typeof(bool),
            typeof(ProfileManager.Profile)
            );

        public Visibility ProfileSupportsLegacyMods
        {
            get
            {
                if (ProfileSupportsLegacyModsProperty != null)
                    return (bool)GetValue(ProfileSupportsLegacyModsProperty) ? Visibility.Visible : Visibility.Collapsed;
                else
                    return ProfileManager.CanUseLiveLegacyMods ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void InitialiseBrowsers()
        {
            _ = UpdateAllBrowsersFull();
        }

        LauncherOptions LauncherOptions { get; set; }

        private void btnBrowseFIFADirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Find your FIFA exe";
            dialog.Multiselect = false;
            dialog.Filter = "exe files (*.exe)|*.exe";
            dialog.FilterIndex = 0;
            dialog.ShowDialog(this);
            var filePath = dialog.FileName;
            if (!string.IsNullOrEmpty(filePath))
            {



                //BuildTextureBrowser(null);

                //Dispatcher.BeginInvoke((Action)(() =>
                //{
                //    GameplayMain.Initialize();
                //}));

            }


            //_ = Start();
        }

        //private void btnLaunchFIFAMods(object sender, RoutedEventArgs e)
        //{
        //    ProjectManagement.FrostyProject.Save("test_gp_speed_change.fbproject");
        //    ProjectManagement.FrostyProject.WriteToMod("test_gp_speed_change.fbmod"
        //        , new ModSettings() { Author = "paulv2k4", Category = "Gameplay", Description = "Gameplay Test", Title = "Gameplay Test", Version = "1.00" });

        //    paulv2k4ModdingExecuter.FrostyModExecutor frostyModExecutor = new paulv2k4ModdingExecuter.FrostyModExecutor();
        //    frostyModExecutor.Run(AssetManager.Instance.fs, this, "", "", new System.Collections.Generic.List<string>() { @"test_gp_speed_change.fbmod" }.ToArray()).Wait();

        //}

        private static System.Windows.Media.Imaging.BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new System.Windows.Media.Imaging.BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        public bool DoNotLog { get; set; }

        public string LogText { get; set; }

        public void LogSync(string text)
        {
            if (DoNotLog)
                return;

            txtLog.ScrollToEnd();

            var stringBuilder = new StringBuilder();

            var txt = string.Empty;
            Dispatcher.Invoke(() =>
            {
                txt = txtLog.Text;
            });
            stringBuilder.Append(txt);
            stringBuilder.AppendLine(text);

            Dispatcher.Invoke(() =>
            {
                txtLog.Text = text;
                txtLog.ScrollToEnd();
            });

        }

        public void Log(string text, params object[] vars)
        {
            if (DoNotLog)
                return;

            LogAsync(text);
        }

        public async void LogAsync(string in_text)
        {
            if (DoNotLog)
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                txtLog.Text += in_text + Environment.NewLine;
                txtLog.ScrollToEnd();
            });
            //var txt = string.Empty;
            //await Dispatcher.InvokeAsync(() =>
            //{
            //    txt = txtLog.Text;
            //});

            //var text = await Task.Run(() =>
            //{
            //    var stringBuilder = new StringBuilder();

            //    stringBuilder.Append(txt);
            //    stringBuilder.AppendLine(in_text);

            //    return stringBuilder.ToString();
            //});

            //await Dispatcher.InvokeAsync(() =>
            //{
            //    txtLog.Text = text;
            //    txtLog.ScrollToEnd();
            //});

        }

        public void LogWarning(string text, params object[] vars)
        {
            if (DoNotLog)
                return;

            Debug.WriteLine("[WARNING] " + text);
            //LogAsync("[WARNING] " + text);
            LogSync("[WARNING] " + text);
        }

        public void LogError(string text, params object[] vars)
        {
            if (DoNotLog)
                return;

            FileLogger.WriteLine("[ERROR] " + text);
            Debug.WriteLine("[ERROR] " + text);
            LogSync("[ERROR] " + text);
        }

        private void btnProjectWriteToMod_Click(object sender, RoutedEventArgs e)
        {
            DisableEditor();
            txtLog.Text = string.Empty;
            //// ---------------------------------------------------------
            //// Remove chunks and actual unmodified files before writing
            //LegacyFileManager_FMTV2.CleanUpChunks();
            loadingDialog.Update("Saving", "Saving");

            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Mod files|*.fbmod";
                var resultValue = saveFileDialog.ShowDialog();
                if (resultValue.HasValue && resultValue.Value)
                {
                    ProjectManagement.Project.WriteToMod(saveFileDialog.FileName, ProjectManagement.Project.ModSettings);
                    if (File.Exists(saveFileDialog.FileName))
                    {
                        Log("Saved mod successfully to " + saveFileDialog.FileName);
                    }
                }

            }
            catch (Exception SaveException)
            {
                LogError(SaveException.ToString());
            }

            loadingDialog.Update("", "");

            EnableEditor();
        }

        private void btnProjectWriteToFIFAMod_Click(object sender, RoutedEventArgs e)
        {
            // ---------------------------------------------------------
            // Remove chunks and actual unmodified files before writing
            ChunkFileManager2022.CleanUpChunks();

            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "FET files|*.fifamod";
                var resultValue = saveFileDialog.ShowDialog();
                if (resultValue.HasValue && resultValue.Value)
                {
                    loadingDialog.Update("Saving", "Saving to FIFAMod");

                    ProjectManagement.Project.WriteToFIFAMod(saveFileDialog.FileName, ProjectManagement.Project.ModSettings);
                    if (File.Exists(saveFileDialog.FileName))
                    {
                        Log($"[{DateTime.Now.ToShortTimeString()}] Saved mod successfully to {saveFileDialog.FileName}");
                    }
                }

            }
            catch (Exception SaveException)
            {
                LogError(SaveException.ToString());
            }
            finally
            {
                loadingDialog.Update("", "");
            }
        }

        private void CompileLMOD(string filename, bool tryEncrypt = true)
        {
            try
            {
                List<string> listOfCompilableFiles = new List<string>();
                var index = 0;
                var allFiles = Directory.GetFiles("Temp", "*.*", SearchOption.AllDirectories).Where(x => !x.Contains(".mod"));
                foreach (var file in allFiles)
                {
                    StringBuilder sbFinalResult = new StringBuilder();

                    var encrypt = !file.Contains(".dds")
                        && !file.Contains(".db") && tryEncrypt;

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
                    //    Log("Encrypting " + file);
                    //    v2k4EncryptionInterop.encryptFile(file, sbFinalResult.ToString());
                    //    File.Delete(file);
                    //}

                    listOfCompilableFiles.Add(sbFinalResult.ToString());
                    index++;
                }

                Log("Encryption compilation complete");

                if (File.Exists(filename))
                    File.Delete(filename);

                using (ZipArchive zipArchive = new ZipArchive(new FileStream(filename, FileMode.OpenOrCreate), ZipArchiveMode.Create))
                {
                    foreach (var compatFile in listOfCompilableFiles)
                    {
                        zipArchive.CreateEntryFromFile(compatFile, compatFile.Replace("Temp\\", ""));
                    }
                }

                Log($"Legacy Mod file saved to {filename}");
            }
            catch (DllNotFoundException)
            {
                if (tryEncrypt)
                {
                    LogError("Error in trying to Encrypt your files. Will revert to unencrypted version of lmod instead.");
                    CompileLMOD(filename, false);
                }
            }
            catch (Exception e)
            {
                LogError("Error in LMOD Compile. Report the message below to Paulv2k4.");
                LogError(e.Message);
            }

        }

        private async void btnProjectSave_Click(object sender, RoutedEventArgs e)
        {
            await SaveProjectWithDialog();
        }

        private async Task<bool> SaveProjectWithDialog()
        {
            loadingDialog.UpdateAsync("Saving Project", "Sweeping up debris", 0);
            //borderLoading.Visibility = Visibility.Visible;
            //loadingDialog.Show();
            await Task.Delay(100);
            // ---------------------------------------------------------
            // Remove chunks and actual unmodified files before writing
            //ChunkFileManager2022.CleanUpChunks();

            await loadingDialog.UpdateAsync("", "");

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "FMTProject files|*.fmtproj|FBProject files|*.fbproject|All Files (*.*)|*.*";
            var result = saveFileDialog.ShowDialog();
            if (!result.HasValue || !result.Value)
                return false;

            if (string.IsNullOrEmpty(saveFileDialog.FileName))
                return false;

            await loadingDialog.UpdateAsync("Saving Project", "Saving project to file");

            // FMT Project file type
            if (saveFileDialog.FileName.EndsWith(".fmtproj", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("FMTProj file type is EXPERIMENTAL. If concerned, use fbproject instead.");

                // Same file -- Simple Update
                if (ProjectManagement.Project is FMTProject fmtProj && fmtProj.Filename.Equals(saveFileDialog.FileName, StringComparison.OrdinalIgnoreCase))
                    fmtProj.Update();
                // Different file -- Create new file and Update
                else
                {
                    var storedPreviousModSettings = ProjectManagement.Project.ModSettings.CloneJson();

                    ProjectManagement.Project = new FMTProject(saveFileDialog.FileName);
                    ProjectManagement.Project.ModSettings.UpdateFromOtherModSettings(storedPreviousModSettings);
                    ((FMTProject)ProjectManagement.Project).Update();
                }
            }
            // Legacy fbproject file type
            else if (saveFileDialog.FileName.EndsWith(".fbproject", StringComparison.OrdinalIgnoreCase))
            {
                // Same file -- Simple Update
                if (ProjectManagement.Project is FrostbiteProject fbProj)
                    await fbProj.SaveAsync(saveFileDialog.FileName, true);
                // Different file -- Create new file and Update
                else
                {
                    ModSettings modSettings = ProjectManagement.Project.ModSettings.CloneJson();
                    ProjectManagement.Project = new FrostbiteProject();
                    ProjectManagement.Project.ModSettings.Author = modSettings.Author;
                    ProjectManagement.Project.ModSettings.Description = modSettings.Description;
                    ProjectManagement.Project.ModSettings.Title = modSettings.Title;
                    ProjectManagement.Project.ModSettings.Version = modSettings.Version;
                    await ProjectManagement.Project.SaveAsync(saveFileDialog.FileName, true);
                }
            }
            // Unknown File
            else
            {
                Log("Unknown file type detected. Project failed to save to " + saveFileDialog.FileName);
                return false;
            }


            lstProjectFiles.ItemsSource = null;
            lstProjectFiles.ItemsSource = ProjectManagement.Project.ModifiedAssetEntries;


            Log("Saved project successfully to " + saveFileDialog.FileName);
            UpdateWindowTitle(saveFileDialog.FileName);


            await loadingDialog.UpdateAsync("", "");

            return true;
        }

        private void EnableEditor()
        {
            Dispatcher.Invoke(() =>
            {
                this.IsEnabled = true;
            });
        }

        private void DisableEditor()
        {
            Dispatcher.Invoke(() =>
            {
                this.IsEnabled = false;
            });
        }

        private async void btnProjectOpen_Click(object sender, RoutedEventArgs e)
        {
            DisableEditor();
            //LoadingDialog loadingDialog = new LoadingDialog("Loading Project", "Cleaning loose Legacy Files");
            loadingDialog.Update("Loading Project", "Cleaning loose Legacy Files");
            await Task.Delay(100);
            // ---------------------------------------------------------
            // Remove chunks and actual unmodified files before writing
            //LegacyFileManager_FMTV2.CleanUpChunks(true); // no longer needed as it should be handled by the Asset Manager Reset

            OpenFileDialog openFileDialog = new OpenFileDialog();
            //openFileDialog.Filter = "Project files|*.fbproject;*.fifamod;*.fmtproj";
            openFileDialog.Filter = "Project files|*.fbproject;*.fmtproj;*.fifamod;*.fbmod;";
            var result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                if (string.IsNullOrEmpty(openFileDialog.FileName))
                    return;

                    loadingDialog.Update("Loading Project", "Resetting files");
                    await AssetManager.Instance.ResetAsync();
                    //AssetManager.Instance.Reset();

                    await loadingDialog.UpdateAsync("Loading Project", "Loading Project File");
                    await Task.Delay(1);

                    var fiFile = new FileInfo(openFileDialog.FileName);
                    if (!fiFile.Exists)
                    {
                        LogError("Unable to load project. The file doesn't exist");
                        return;
                    }

                    ProjectManagement.Project = null;

                    var fileExtension = fiFile.Extension.ToLower();
                    switch (fileExtension)
                    {

                        case ".fbproject":
                            CancellationToken cancellation = default(CancellationToken);
                            try
                            {
                                ProjectManagement.Project = new FrostbiteProject();
                                await ProjectManagement.Project.LoadAsync(openFileDialog.FileName, cancellation);
                            }
                            catch (Exception ex)
                            {
                                LogError("Unable to load project. This may be due to a Title Update. Message: " + ex.Message);
                            }
                            break;
                        case ".fmtproj":
                            //var mbFmtProj = MessageBox.Show("This is a NEW/UPCOMMING file format. This does nothing right now!", "EXPERIMENTAL");
                            try
                            {
                                ProjectManagement.Project = FMTProject.Read(openFileDialog.FileName);
                            }
                            catch (Exception ex)
                            {
                                LogError("Unable to load project. This may be due to a Title Update. Message: " + ex.Message);
                            }
                            break;
                        case ".fifamod":

                            var mbFIFAMod = MessageBox.Show(
                                "You are opening a compiled FIFAMod from FIFA Editor Tool. " + Environment.NewLine +
                                "This is experimental and may crash the Application. " + Environment.NewLine +
                                "This process may be missing or losing files. " + Environment.NewLine +
                                "Please always give credit to other's work!", "EXPERIMENTAL");
                            if (mbFIFAMod == MessageBoxResult.OK)
                            {
                                using (FIFAModReader reader = new FIFAModReader(new FileStream(fiFile.FullName, FileMode.Open)))
                                {
                                    ProjectManagement.Project = new FrostbiteProject();
                                    ProjectManagement.Project.Load(reader);
                                    ProjectManagement.Project.ModSettings.MergedModList.Add(ProjectManagement.Project.ModSettings);
                                }
                            }
                            break;
                        case ".fbmod":

                            var mbFBMod = MessageBox.Show(
                                "You are opening a compiled FBMod from Frostbite Modding Tool. " + Environment.NewLine +
                                "This is experimental and may crash the Application. " + Environment.NewLine +
                                "This process may be missing or losing files. " + Environment.NewLine +
                                "Please always give credit to other's work!", "EXPERIMENTAL");
                            if (mbFBMod == MessageBoxResult.OK)
                            {
                                FrostbiteMod frostbiteMod = new FrostbiteMod(fiFile.FullName);
                                ProjectManagement.Instance.Project = new FMTProject("loadInFbMod");
                                if (ProjectManagement.Instance.Project.Load(frostbiteMod))
                                {
                                    ProjectManagement.Project.ModSettings.MergedModList.Add(ProjectManagement.Project.ModSettings);
                                    Log($"Successfully opened {fiFile.FullName}");
                                }
                                else
                                {
                                    Log($"Failed to open {fiFile.FullName}");
                                }
                        }
                            break;
                    }

                    // A chunk clean up of bad and broken projects
                    await Task.Run(() =>
                    {
                        ChunkFileManager2022.CleanUpChunks();
                    });
                    lstProjectFiles.ItemsSource = null;
                    lstProjectFiles.ItemsSource = ProjectManagement.Project.ModifiedAssetEntries;

                    await Task.Run(() =>
                    {
                        InitialiseBrowsers();
                    });
                    Log("Opened project successfully from " + openFileDialog.FileName);

                    UpdateWindowTitle(openFileDialog.FileName);

                    DiscordInterop.DiscordRpcClient.UpdateDetails("In Editor [" + GameInstanceSingleton.Instance.GAMEVERSION + "] - " + ProjectManagement.Project.DisplayName);



            }
            loadingDialog.Update("", "");
            //loadingDialog.Close();
            //loadingDialog = null;
            Dispatcher.Invoke(() =>
            {
                this.IsEnabled = true;
            });
        }

        public Random RandomSaver = new Random();

        private async void btnLaunchFIFAInEditor_Click(object sender, RoutedEventArgs e)
        {
            //LegacyFileManager_FMTV2.CleanUpChunks();
            if(string.IsNullOrEmpty(ProjectManagement.Project.ModSettings.Title))
            {
                LogError("Unable to launch. Please provide a Title in the Mod Details screen.");
                return;
            }

            loadingDialog.UpdateAsync("Launching game", "-", 0);
            await Dispatcher.InvokeAsync(() => { btnLaunchFIFAInEditor.IsEnabled = false; });

            if (!string.IsNullOrEmpty(ProjectManagement.Project.Filename))
            {
                loadingDialog.UpdateAsync("Launching game", "Autosaving project", 25);
                Log("Autosaving Project");
                bool saved = await Task.Run(async() =>
                {
                    // Delete old Autosaves
                    foreach (var tFile in Directory.GetFiles(App.ApplicationDirectory, "*.fbproject"))
                    {
                        if (File.GetLastWriteTime(tFile) < DateTime.Now.AddDays(-2))
                            File.Delete(tFile);
                    };
                    var fnBeforeAutoSave = ProjectManagement.Project.Filename;
                    var result = await ProjectManagement.Project.SaveAsync("Autosave-" + RandomSaver.Next().ToString() + ".fbproject", true);
                    //return ProjectManagement.Project.Save(fnBeforeAutoSave);
                    ProjectManagement.Project.Filename = fnBeforeAutoSave;
                    return result;
                });
            }

            foreach (var tFile in Directory.GetFiles(App.ApplicationDirectory, "*.fbmod")) { File.Delete(tFile); };

            var testmodname = "EditorProject.fbmod";

            var author = ProjectManagement.Project.ModSettings != null ? ProjectManagement.Project.ModSettings.Author : string.Empty;
            var category = ProjectManagement.Project.ModSettings != null ? ProjectManagement.Project.ModSettings.Category : string.Empty;
            var desc = ProjectManagement.Project.ModSettings != null ? ProjectManagement.Project.ModSettings.Description : string.Empty;
            var title = ProjectManagement.Project.ModSettings != null ? ProjectManagement.Project.ModSettings.Title : string.Empty;
            var version = ProjectManagement.Project.ModSettings != null ? ProjectManagement.Project.ModSettings.Version : string.Empty;

            loadingDialog.UpdateAsync("Launching game", "Creating Mod", 50);
            await Task.Run(() =>
            {
                ProjectManagement.Project.WriteToMod(testmodname
                    , new ModSettings() { Author = author, Category = category, Description = desc, Title = title, Version = version });
            });

            var useModData = swUseModData.IsOn;

            if (launcherOptions != null)
            {
                launcherOptions.UseModData = swUseModData.IsOn;
                launcherOptions.UseLegacyModSupport = swEnableLegacyInjection.IsOn;
                launcherOptions.Save();
            }

            try
            {
                loadingDialog.UpdateAsync("Launching game", "Compiling", 99);

                ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
                ModdingSupport.ModExecutor.UseModData = useModData;
                frostyModExecutor.UseSymbolicLinks = false;
                frostyModExecutor.ForceRebuildOfMods = true;
                await Task.Run(async() =>
                {
                    await frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { testmodname }.ToArray());
                });
            }
            catch (Exception ex)
            {
                LogError("Error when trying to compile mod and launch game. Message: " + ex.Message);
            }

            await InjectLegacyDLL();

            await Dispatcher.InvokeAsync(() => { btnLaunchFIFAInEditor.IsEnabled = true; });

            //LegacyFileManager_FMTV2.CleanUpChunks();
            loadingDialog.Update("", "");

        }

        private async Task<bool> InjectLegacyDLL()
        {
            try
            {
                if (swEnableLegacyInjection.IsOn)
                {
                    string legacyModSupportFile = null;
                    if (ProfileManager.IsFIFA20DataVersion())
                    {
                        legacyModSupportFile = Directory.GetParent(App.ApplicationDirectory) + @"\FIFA20Legacy.dll";
                    }
                    else if (ProfileManager.IsFIFA21DataVersion())
                    {
                        legacyModSupportFile = Directory.GetParent(App.ApplicationDirectory) + @"\FIFA21Legacy.dll";
                    }

                    if (!string.IsNullOrEmpty(legacyModSupportFile))
                    {

                        if (File.Exists(legacyModSupportFile))
                        {
                            if (File.Exists(GameInstanceSingleton.Instance.GAMERootPath + "v2k4LegacyModSupport.dll"))
                                File.Delete(GameInstanceSingleton.Instance.GAMERootPath + "v2k4LegacyModSupport.dll");

                            File.Copy(legacyModSupportFile, GameInstanceSingleton.Instance.GAMERootPath + "v2k4LegacyModSupport.dll");

                            if (File.Exists(@GameInstanceSingleton.Instance.GAMERootPath + @"v2k4LegacyModSupport.dll"))
                            {
                                var legmodsupportdllpath = @GameInstanceSingleton.Instance.GAMERootPath + @"v2k4LegacyModSupport.dll";
                                return await GameInstanceSingleton.InjectDLLAsync(legmodsupportdllpath);
                            }
                        }


                    }
                }
            }
            catch (Exception e)
            {
                LogError(e.ToString());
            }

            return false;
        }

        private async void btnProjectNew_Click(object sender, RoutedEventArgs e)
        {
            loadingDialog.Update("Resetting", "Resetting");
            await AssetManager.Instance.ResetAsync();
            //LegacyFileManager_FMTV2.CleanUpChunks(true); // no longer needed as it should be handled by the Asset Manager Reset
            ProjectManagement.Project = new FrostbiteProject(AssetManager.Instance, AssetManager.Instance.FileSystem);
            lstProjectFiles.ItemsSource = null;
            UpdateWindowTitle("New Project");

            Log("New Project Created");
            loadingDialog.Update("", "");

        }

        private async void btnCompileLegacyModFromFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.AllowMultiSelect = false;
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var legacyDirectory = dialog.SelectedFolder;
                    await CompileLegacyModFromFolder(legacyDirectory);
                }
            }
        }

        private readonly string LegacyProjectExportedFolder = App.ApplicationDirectory + "\\TEMPLEGACYPROJECT";
        private readonly string LegacyTempFolder = App.ApplicationDirectory + "\\TEMP";

        public event PropertyChangedEventHandler PropertyChanged;

        private async Task<bool> CompileLegacyModFromFolder(string legacyDirectory)
        {
            if (string.IsNullOrEmpty(legacyDirectory))
            {
                MessageBox.Show("Inputted Legacy Directory is Empty", "Save Failed");
                return false;
            }

            await Task.Run(() =>
            {
                DirectoryInfo directoryInfoTemp = new DirectoryInfo(LegacyTempFolder);
                if (directoryInfoTemp.Exists)
                {
                    RecursiveDelete(directoryInfoTemp);
                }

                DirectoryCopy(legacyDirectory, LegacyTempFolder, true);
                List<string> listOfCompilableFiles = new List<string>();

                var allFiles = Directory.GetFiles(LegacyTempFolder, "*.*", SearchOption.AllDirectories).Where(x => !x.Contains(".mod"));
                Task[] tasks = new Task[allFiles.Count()];
                foreach (var file in allFiles)
                {
                    var fIFile = new FileInfo(file);
                    //tasks[index] = Task.Run(() =>
                    //{
                    StringBuilder sbFinalResult = new StringBuilder();

                    var encrypt = !fIFile.Extension.Contains(".dds", StringComparison.OrdinalIgnoreCase)
                        && !fIFile.Extension.Contains(".db", StringComparison.OrdinalIgnoreCase)
                        && !fIFile.Extension.Contains(".loc", StringComparison.OrdinalIgnoreCase);

                    if (encrypt)
                    {
                        var lastIndexOfDot = fIFile.FullName.LastIndexOf('.');
                        var newFile = fIFile.FullName.Substring(0, lastIndexOfDot) + ".mod";
                        sbFinalResult.Append(newFile);
                    }
                    else
                    {
                        sbFinalResult.Append(file);
                    }

                    //if (encrypt)
                    //{
                    //    var fI = new FileInfo(file);
                    //    if (fI != null && fI.Extension.Contains("mod"))
                    //    {
                    //        fI.Delete();
                    //    }
                    //    Log("Encrypting " + file);
                    //    v2k4EncryptionInterop.encryptFile(file, sbFinalResult.ToString());
                    //}

                    listOfCompilableFiles.Add(sbFinalResult.ToString());
                }

                Log("Legacy Compiler :: Compilation Complete");


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
                            var indexOfTemp = compatFile.LastIndexOf("TEMP\\");
                            var newZipLocation = compatFile.Substring(indexOfTemp + 5, compatFile.Length - (indexOfTemp + 5));
                            zipArchive.CreateEntryFromFile(compatFile, newZipLocation);
                        }
                    }
                    Log("Legacy Compiler :: Legacy Mod file saved");

                    foreach (var f in listOfCompilableFiles.Where(x => x.EndsWith(".mod")))
                    {
                        var fI = new FileInfo(f);
                        if (fI != null)
                        {
                            fI.Delete();
                        }
                    }

                }

                if (directoryInfoTemp.Exists)
                {
                    RecursiveDelete(directoryInfoTemp);
                    Log("Legacy Compiler :: Cleaned up encrypted files");
                }


            });

            return true;
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

        private void MainViewer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //var t = sender as TabControl;
            //if (t != null)
            //{
            //    var selectedTab = ((TabItem)t.SelectedItem);
            //    var selectedTabHeader = selectedTab.Header;

            //    if (selectedTabHeader.ToString().Contains("Player"))
            //    {
            //        var legacyFiles = ProjectManagement.FrostyProject.AssetManager.EnumerateCustomAssets("legacy").OrderBy(x => x.Path).ToList();
            //        var mainDb = legacyFiles.FirstOrDefault(x => x.Filename.Contains("fifa_ng_db") && x.Type == "DB");
            //        var mainDbMeta = legacyFiles.FirstOrDefault(x => x.Filename.Contains("fifa_ng_db-meta") && x.Type == "XML");
            //        playerEditor.LoadPlayerList(mainDb as LegacyFileEntry, mainDbMeta as LegacyFileEntry);
            //        playerEditor.UpdateLayout();
            //    }
            //}
        }

        private void btnOpenModDetailsPanel_Click(object sender, RoutedEventArgs e)
        {
            var mdw = new ModDetailsWindow();
            mdw.Show();
        }

        private void btnRevertAsset_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            AssetEntry assetEntry = button.Tag as AssetEntry;
            AssetManager.Instance.RevertAsset(assetEntry);
            lstProjectFiles.ItemsSource = null;
            lstProjectFiles.ItemsSource = ProjectManagement.Project.ModifiedAssetEntries;
            Log("Reverted Asset " + assetEntry.Name);
        }


        void CreateTestEditor()
        {
            dynamic dHotspotObject = new ExpandoObject { };
            List<dynamic> testDList = new List<dynamic>();
            for (System.Single i = 0; i < 20; i++)
            {
                testDList.Add(new
                {
                    Bounds = new { x = 0, y = 0, z = 0, w = i }
                    ,
                    Group = new CString("Test G " + i.ToString())
                    ,
                    Name = new CString("Test N " + i.ToString())
                    ,
                    Rotation = (float)1.45 + i
                });
            }
            dHotspotObject.Name = "Test Obj";
            dHotspotObject.Hotspots = testDList;
            EbxAsset testEbxAsset = new EbxAsset();
            testEbxAsset.SetRootObject(dHotspotObject);

            TestEbxViewer.Children.Add(
                new Editor(testEbxAsset));

        }

        public void UpdateAllBrowsers()
        {
            //dataBrowser.UpdateAssetListView();
            //textureBrowser.UpdateAssetListView();
            //gameplayBrowser.UpdateAssetListView();
            //legacyBrowser.UpdateAssetListView();
            //faceBrowser.UpdateAssetListView();
            //bootsBrowser.UpdateAssetListView();
        }

        public async Task UpdateAllBrowsersFull()
        {
            var dataBrowserData = ProjectManagement.Project.AssetManager
                                  .EnumerateEbx()
                                  .Where(x => !x.Path.ToLower().Contains("character/kit")).OrderBy(x => x.Path).Select(x => (IAssetEntry)x).ToList();

            // Kit Browser
            var kitList = ProjectManagement.Project.AssetManager
                               .EnumerateEbx()
                               .Where(x => x.Path.ToLower().Contains("character/kit"))
                               .OrderBy(x => x.Path)
                               .Select(x => (IAssetEntry)x);

            var gpBrowserData = ProjectManagement.Project.AssetManager
                                  .EnumerateEbx()
                                  .Where(x => x.Path.Contains("fifa/attribulator", StringComparison.OrdinalIgnoreCase))
                                  .OrderBy(x => x.Path)
                                  .Select(x => (IAssetEntry)x).ToList();

            var legacyFiles = ProjectManagement.Project.AssetManager.EnumerateCustomAssets("legacy").OrderBy(x => x.Path).Select(x => (IAssetEntry)x).ToList();

            List<IAssetEntry> textureAssets = ProjectManagement.Project.AssetManager
                                .EnumerateEbx("TextureAsset").OrderBy(x => x.Path).Select(x => (IAssetEntry)x).ToList();
            textureAssets.AddRange(legacyFiles.Where(x => x.Name.Contains(".DDS", StringComparison.OrdinalIgnoreCase)));


            var faceList = ProjectManagement.Project.AssetManager
                                  .EnumerateEbx().Where(x => x.Path.ToLower().Contains("character/player")).OrderBy(x => x.Path).Select(x => (IAssetEntry)x);
            faceList = faceList.OrderBy(x => x.Name).ToList();

            var bootList = ProjectManagement.Project.AssetManager
                                 .EnumerateEbx().Where(x => x.Path.ToLower().Contains("character/shoe")).OrderBy(x => x.Path).Select(x => (IAssetEntry)x);
            bootList = bootList.OrderBy(x => x.Name);

            await Dispatcher.InvokeAsync(() =>
            {
                dataBrowser.AllAssetEntries = dataBrowserData;

                kitBrowser.AllAssetEntries = kitList;

                gameplayBrowser.AllAssetEntries = gpBrowserData;

                legacyBrowser.AllAssetEntries = legacyFiles;

                textureBrowser.AllAssetEntries = textureAssets;

                faceBrowser.AllAssetEntries = faceList;

                bootsBrowser.AllAssetEntries = bootList;
            });

            UpdateAllBrowsers();

            Log("Updated Browsers");
        }

        private void btnCleanUpLegacyFiles_Click(object sender, RoutedEventArgs e)
        {
            //LegacyFileManager_M21.CleanUpChunks();
            ChunkFileManager2022.CleanUpChunks(true);
            Log("Legacy files have been cleaned");
        }

        //private async void btnProjectWriteToLegacyMod_Click(object sender, RoutedEventArgs e)
        //{
        //    if(ProjectManagement.Project.AssetManager.EnumerateCustomAssets("legacy", true).Count() == 0)
        //    {
        //        MessageBox.Show("You do not have any modified legacy items to save!", "Save Failed");
        //        return;
        //    }

        //    LoadingDialog loadingDialog = new LoadingDialog("Saving Legacy Mod", "Exporting files");
        //    loadingDialog.Show();

        //    var exportFolder = LegacyProjectExportedFolder;
        //    if (!Directory.Exists(exportFolder))
        //        Directory.CreateDirectory(exportFolder);

        //    RecursiveDelete(new DirectoryInfo(exportFolder));

        //    foreach (var f in ProjectManagement.Project.AssetManager.EnumerateCustomAssets("legacy", true))
        //    {
        //        LegacyFileEntry lfe = f as LegacyFileEntry;
        //        if(lfe != null)
        //        {
        //            await loadingDialog.UpdateAsync("Saving Legacy Mod", "Exporting " + lfe.Filename);
        //            var lfeStream = (MemoryStream)ProjectManagement.Project.AssetManager.GetCustomAsset("legacy", lfe);

        //            if (!Directory.Exists(exportFolder + "\\" + lfe.Path))
        //                Directory.CreateDirectory(exportFolder + "\\" + lfe.Path);

        //            using (var nw = new NativeWriter(new FileStream(exportFolder + "\\" + lfe.Path + "\\" + lfe.Filename + "." + lfe.Type, FileMode.Create)))
        //            {
        //                nw.WriteBytes(lfeStream.ToArray());
        //            }
        //        }
        //    }

        //    await CompileLegacyModFromFolder(exportFolder);

        //    loadingDialog.Close();
        //    loadingDialog = null;


        //}

        private void btnOpenEmbeddedFilesWindow_Click(object sender, RoutedEventArgs e)
        {
            FrostbiteModEmbeddedFiles frostbiteModEmbeddedFiles = new FrostbiteModEmbeddedFiles();
            frostbiteModEmbeddedFiles.ShowDialog();
        }

        private void btnImportKitCreatorZip_Click(object sender, RoutedEventArgs e)
        {
            KitCreatorImport kitCreatorImport = new KitCreatorImport();
            kitCreatorImport.ShowDialog();
        }

        private async void btnProjectMerge_Click(object sender, RoutedEventArgs e)
        {
            loadingDialog.Update("Loading Project", "");
            await Task.Delay(100);

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Project files|*.fbproject";
            var result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                if (string.IsNullOrEmpty(openFileDialog.FileName))
                    return;

                {
                    await loadingDialog.UpdateAsync("Loading Project", "Loading and Merging Project File");
                    IEnumerable<Assembly> currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                    var assemblyWithType = currentAssemblies.FirstOrDefault(x => x.GetTypes().Any(x => x.GetInterface(nameof(IProject)) != null));
                    if (assemblyWithType == null)
                        return;

                    var mergerProject = new FrostbiteProject();
                    await mergerProject.LoadAsync(openFileDialog.FileName);
                    // A chunk clean up of bad and broken projects
                    await Task.Run(() =>
                    {
                        ChunkFileManager2022.CleanUpChunks();
                    });
                    lstProjectFiles.ItemsSource = null;
                    lstProjectFiles.ItemsSource = ProjectManagement.Project.ModifiedAssetEntries;

                    await Task.Run(() =>
                    {
                        InitialiseBrowsers();
                    });
                    Log($"Merged project successfully with {mergerProject.EBXCount} EBX, {mergerProject.RESCount} RES, {mergerProject.ChunkCount} Chunks, {mergerProject.LegacyCount} Legacy files");
                }
            }
            //loadingDialog.Close();
            loadingDialog.Update("", "");
        }

        private void btnModifyLocaleINI_Click(object sender, RoutedEventArgs e)
        {
            LocaleINIEditor localeINIEditor = new LocaleINIEditor();
            localeINIEditor.ShowDialog();
        }

        private void btnModifyInitfs_Click(object sender, RoutedEventArgs e)
        {
            InitfsEditor initfsEditor = new InitfsEditor();
            initfsEditor.ShowDialog();
        }

        private async void btnOpenDbFile_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "";
            if (openFileDialog.ShowDialog().Value)
            {
                FileInfo fileInfo = new FileInfo(openFileDialog.FileName);
                if (fileInfo.Exists)
                {
                    LayoutDocumentGroupPaneDatabases.AllowDuplicateContent = false;
                    foreach (var child in LayoutDocumentGroupPaneDatabases.Children)
                    {
                        if (child.Title == fileInfo.Name)
                        {
                            LayoutDocumentGroupPaneDatabases.SelectedContentIndex = LayoutDocumentGroupPaneDatabases.Children.IndexOf(child);
                            return;
                        }
                    }

                    loadingDialog.Update("Loading DB File", "Loading");

                    var newLayoutDoc = new LayoutDocument();
                    newLayoutDoc.Title = fileInfo.Name;
                    FIFADBEditor dbEditor = new FIFADBEditor();
                    dbEditor.EditorMode = FIFADBEditor.DBEditorMode.Career;
                    await dbEditor.Load(fileInfo.FullName);
                    newLayoutDoc.Content = dbEditor;
                    LayoutDocumentGroupPaneDatabases.Children.Insert(0, newLayoutDoc);
                    LayoutDocumentGroupPaneDatabases.SelectedContentIndex = 0;
                }
            }

            loadingDialog.Update("", "");

        }

        private void btnSaveDbFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "";
                if (saveFileDialog.ShowDialog() == true)
                {
                    FIFADBEditor dbEditor = LayoutDocumentGroupPaneDatabases.SelectedContent.Content as FIFADBEditor;
                    if (dbEditor != null)
                    {
                        if (dbEditor.DB is FifaLibrary.CareerFile careerFile)
                        {
                            careerFile.SaveEa(saveFileDialog.FileName);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        private void modLTU_Click(object sender, RoutedEventArgs e)
        {
            LTUEditor ltuEditor = new LTUEditor();
            ltuEditor.ShowDialog();
        }
    }
}
