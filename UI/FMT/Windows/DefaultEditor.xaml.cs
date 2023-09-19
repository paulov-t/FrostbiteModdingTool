using FIFAModdingUI.Pages.Common;
using FIFAModdingUI.Windows;
using FMT;
using FMT.FileTools;
using FMT.Pages.Common;
using FMT.Windows;
using FolderBrowserEx;
using Frostbite.FileManagers;
using FrostbiteModdingUI.Models;
using FrostbiteSdk;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.Frostbite;
using FrostySdk.Frostbite.PluginInterfaces;
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
using static FrostySdk.ProfileManager.Tools;

namespace FrostbiteModdingUI.Windows
{
    /// <summary>
    /// Interaction logic for DefaultEditor.xaml
    /// </summary>
    public partial class DefaultEditor : MetroWindow, IEditorWindow
    {
        public Window OwnerWindow { get; set; }

        public LauncherOptions launcherOptions { get; set; }

        [Obsolete("Incorrect usage of Editor Windows")]
        public DefaultEditor()
        {
            throw new Exception("Incorrect usage of Editor Windows");
        }

        public DefaultEditor(Window owner)
        {
            if (string.IsNullOrEmpty(ProfileManager.ProfileName))
            {
                throw new Exception("Profile has not been loaded before Editor!");
            }
            InitializeComponent();
            this.DataContext = this;
            Loaded += DefaultEditor_Loaded;
            Owner = owner;

        }

        private async void DefaultEditor_Loaded(object sender, RoutedEventArgs e)
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


            try
            {
                Uri iconUri = null;
                if (File.Exists(ProfileManager.LoadedProfile.EditorIcon))
                {
                    
                    switch(new FileInfo(ProfileManager.LoadedProfile.EditorIcon).Extension)
                    {
                        case ".ico":
                            break;
                        case ".png":
                        case ".jpg":
                            var bmpBytes = new CSharpImageLibrary.ImageEngineImage(File.ReadAllBytes(ProfileManager.LoadedProfile.EditorIcon)).Save(
                                new CSharpImageLibrary.ImageFormats.ImageEngineFormatDetails(CSharpImageLibrary.ImageEngineFormat.BMP)
                                , CSharpImageLibrary.MipHandling.KeepTopOnly);
                            if (bmpBytes == null)
                                iconUri = new Uri("pack://application:,,,/FMT;component/FMTIcon24.ico");
                            else
                                Icon = BitmapFrame.Create(CreateBitmapImageFromBytes(bmpBytes));
                            break;
                        default:
                            iconUri = new Uri("pack://application:,,,/FMT;component/FMTIcon24.ico");
                            break;
                    }
                }
                else
                {
                    iconUri = new Uri("pack://application:,,,/FMT;component/FMTIcon24.ico");
                }

                if(iconUri != null) 
                { 
                    Icon = BitmapFrame.Create(iconUri);
                }
            }
            catch
            {

            }

            launcherOptions = await LauncherOptions.LoadAsync();

        }

        private System.Windows.Media.Imaging.BitmapImage CreateBitmapImageFromBytes(byte[] imageData)
        {
            BitmapImage bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
            if (imageData == null || imageData.Length == 0)
                return null;

            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.UriSource = null;
                bitmapImage.StreamSource = mem;
                bitmapImage.EndInit();
            }
            bitmapImage.Freeze();

            imageData = null;

            return bitmapImage;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
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
            ProjectManagement = null;
            ProjectManagement.Instance = null;
            if (AssetManager.Instance != null)
            {
                //AssetManager.Instance.Dispose();
                AssetManager.Instance = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Owner.Visibility = Visibility.Visible;

            base.OnClosed(e);
        }

        public string AdditionalTitle { get; set; }
        public string WindowTitle
        {
            get
            {
                var gameVersion = GameInstanceSingleton.GetGameVersion() ?? "FIFA";
                var readOnlyTitleText = !ProfileManager.LoadedProfile.CanLaunchMods ? "[READ ONLY]" : "";
                var initialTitle = $"FMT [{gameVersion}]{readOnlyTitleText} Editor - {App.ProductVersion} - ";
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(initialTitle);
                stringBuilder.Append("[" + AdditionalTitle + "]");
                return stringBuilder.ToString();
            }
        }

        public void UpdateWindowTitle(string additionalText)
        {
            AdditionalTitle = additionalText;
            this.DataContext = null;
            this.DataContext = this;
            this.UpdateLayout();
        }

        public ProjectManagement ProjectManagement { get; set; }

        public async Task InitialiseOfSelectedGame(string filePath)
        {
            DisableEditor();
            loadingDialog.Update("Loading Game Files", "");

            await GameInstanceSingleton.InitializeSingletonAsync(filePath, true, this);
            GameInstanceSingleton.Logger = this;

            loadingDialog.Update("Loading Game Files", "");

            await Task.Run(
                () =>
            {


                ProjectManagement = new ProjectManagement(filePath, loadingDialog);
                ProjectManagement.StartNewProject();
                InitialiseBrowsers();
            });


            await Dispatcher.InvokeAsync(() =>
            {
                miProject.IsEnabled = true;

                if (
                ProfileManager.LoadedProfile.Tools.Internal != null &&
                ProfileManager.LoadedProfile.Tools.Internal.Count > 0)
                {
                    MenuItem toolsMenuItem = new MenuItem();
                    toolsMenuItem.Header = new StackPanel();
                    var spToolsMenuItemHeader = (StackPanel)toolsMenuItem.Header;
                    spToolsMenuItemHeader.Children.Add(new Label() { Content = "Tools", FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 });
                    topMenu.Items.Add(toolsMenuItem);
                    foreach(var internalTool in ProfileManager.LoadedProfile.Tools.Internal)
                    {
                        MenuItem toolItem = new MenuItem();
                        toolItem.Header = new Label() { Content = internalTool.Name, FontSize = 10 };
                        toolItem.Click += (object sender, RoutedEventArgs e) => {

                            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                            {
                                var t = a.GetTypes().FirstOrDefault(x => x.Name.Contains(internalTool.Window, StringComparison.OrdinalIgnoreCase));
                                if (t != null)
                                {
                                    var toolWindow = (Window)Activator.CreateInstance(t);
                                    toolWindow.Show();
                                    return;
                                }
                            }
                        
                        };

                        toolsMenuItem.Items.Add(toolItem);
                    }
                }

                this.DataContext = null;
                this.DataContext = this;
                this.UpdateLayout();

                btnLaunchEditor.IsEnabled = ProfileManager.LoadedProfile.CanLaunchMods;

            });

            await Task.Run(
              () =>
              {

                  loadingDialog.Update("", "");

                ProjectManagement.Logger = this;
                EnableEditor();

            });

            //loadingDialog.Close();
            loadingDialog.Update("", "");


            DiscordInterop.DiscordRpcClient.UpdateDetails("In Editor [" + GameInstanceSingleton.Instance.GAMEVERSION + "]");

            LauncherOptions = await LauncherOptions.LoadAsync();
            swUseModData.IsEnabled = ProfileManager.LoadedProfile.CanUseModData && !ProfileManager.LoadedProfile.ForceUseModData;
            swUseModData.IsOn = LauncherOptions.UseModData.HasValue ? LauncherOptions.UseModData.Value : true;

            Dispatcher.Invoke(() =>
            {
                UpdateWindowTitle("New Project");
            });


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
            UpdateBrowsersAllFull();
        }

        LauncherOptions LauncherOptions { get; set; }


        public bool DoNotLog { get; set; }

        public string LogText { get; set; }

        public void LogSync(string text)
        {
            if (DoNotLog)
                return;

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

            var txt = string.Empty;
            await Dispatcher.InvokeAsync(() =>
            {
                txt = txtLog.Text;
            });

            var text = await Task.Run(() =>
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.Append(txt);
                stringBuilder.AppendLine(in_text);

                return stringBuilder.ToString();
            });

            await Dispatcher.InvokeAsync(() =>
            {
                txtLog.Text = text;
                txtLog.ScrollToEnd();
            });

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

            Debug.WriteLine("[ERROR] " + text);
            LogSync("[ERROR] " + text);
        }

        private void btnProjectWriteToMod_Click(object sender, RoutedEventArgs e)
        {
            DisableEditor();

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

        public virtual async Task<bool> SaveProjectWithDialog()
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
                //MessageBox.Show("FMTProj file type is EXPERIMENTAL. If concerned, use fbproject instead.");

                // Same file -- Simple Update
                if (ProjectManagement.Project is FMTProject fmtProj && fmtProj.Filename.Equals(saveFileDialog.FileName, StringComparison.OrdinalIgnoreCase))
                    fmtProj.Update();
                // Different file -- Create new file and Update
                else
                {
                    var storedPreviousModSettings = JsonConvert.DeserializeObject<ModSettings>(JsonConvert.SerializeObject(ProjectManagement.Project.ModSettings));

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
            openFileDialog.Filter = "Project files|*.fbproject;*.fmtproj;*.fifamod;*.fbmod;";
            var result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                if (!string.IsNullOrEmpty(openFileDialog.FileName))
                {
                    loadingDialog.Update("Loading Project", "Resetting files");
                    await AssetManager.Instance.ResetAsync();
                    //AssetManager.Instance.Reset();

                    loadingDialog.Update("Loading Project", "Loading Project File");

                    CancellationToken cancellation = default(CancellationToken);
                    try
                    {
                        await ProjectManagement.LoadProjectFromFile(openFileDialog.FileName, cancellation);
                    }
                    catch (Exception ex)
                    {
                        LogError("Unable to load project. This may be due to a Title Update. Message: " + ex.Message);
                    }
                    // A chunk clean up of bad and broken projects
                    await Task.Run(() =>
                    {
                        ChunkFileManager2022.CleanUpChunks();
                    });

                    await Task.Run(() =>
                    {
                        InitialiseBrowsers();
                    });
                    Log("Opened project successfully from " + openFileDialog.FileName);

                    UpdateWindowTitle(openFileDialog.FileName);

                    DiscordInterop.DiscordRpcClient.UpdateDetails("In Editor [" + GameInstanceSingleton.Instance.GAMEVERSION + "] - " + ProjectManagement.Project.DisplayName);



                }
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

            loadingDialog.Update("Launching game", "-", 0);
            await Dispatcher.InvokeAsync(() => { btnLaunchEditor.IsEnabled = false; });

            if (!string.IsNullOrEmpty(ProjectManagement.Project.Filename))
            {
                loadingDialog.Update("Launching game", "Autosaving project", 25);
                Log("Autosaving Project");
                bool saved = await Task.Run(() =>
                {
                    // Delete old Autosaves
                    foreach (var tFile in Directory.GetFiles(App.ApplicationDirectory, "*.fbproject"))
                    {
                        if (File.GetLastWriteTime(tFile) < DateTime.Now.AddDays(-2))
                            File.Delete(tFile);
                    };
                    var fnBeforeAutoSave = ProjectManagement.Project.Filename;
                    var result = ProjectManagement.Project.SaveAsync("Autosave-" + RandomSaver.Next().ToString() + ".fbproject", true).Result;
                    //return ProjectManagement.Project.Save(fnBeforeAutoSave);
                    ProjectManagement.Project.Filename = fnBeforeAutoSave;
                    return result;
                });
            }


            //Log("Deleting old test mods");
            foreach (var tFile in Directory.GetFiles(App.ApplicationDirectory, "*.fbmod")) { File.Delete(tFile); };

            var testmodname = "EditorProject.fbmod";

            var author = ProjectManagement.Project.ModSettings != null ? ProjectManagement.Project.ModSettings.Author : string.Empty;
            var category = ProjectManagement.Project.ModSettings != null ? ProjectManagement.Project.ModSettings.Category : string.Empty;
            var desc = ProjectManagement.Project.ModSettings != null ? ProjectManagement.Project.ModSettings.Description : string.Empty;
            var title = ProjectManagement.Project.ModSettings != null ? ProjectManagement.Project.ModSettings.Title : string.Empty;
            var version = ProjectManagement.Project.ModSettings != null ? ProjectManagement.Project.ModSettings.Version : string.Empty;

            loadingDialog.Update("Launching game", "Creating Mod", 50);
            await Task.Run(() =>
            {
                ProjectManagement.Project.WriteToMod(testmodname
                    , new ModSettings() { Author = author, Category = category, Description = desc, Title = title, Version = version });
            });

            var useModData = swUseModData.IsOn;

            if (launcherOptions != null)
            {
                launcherOptions.UseModData = swUseModData.IsOn;
                launcherOptions.Save();
            }

            try
            {
                loadingDialog.Update("Launching game", "Compiling", 99);

                await Task.Run(() =>
                {
                    ModdingSupport.ModExecutor frostyModExecutor = new ModdingSupport.ModExecutor();
                    ModdingSupport.ModExecutor.UseModData = useModData;
                    frostyModExecutor.UseSymbolicLinks = false;
                    frostyModExecutor.ForceRebuildOfMods = true;
                    frostyModExecutor.Run(this, GameInstanceSingleton.Instance.GAMERootPath, new List<string>() { testmodname }.ToArray()).Wait();
                });
            }
            catch (Exception ex)
            {
                LogError("Error when trying to compile mod and launch game. Message: " + ex.Message);
            }


            await Dispatcher.InvokeAsync(() => { btnLaunchEditor.IsEnabled = true; });

            //LegacyFileManager_FMTV2.CleanUpChunks();
            loadingDialog.Update("", "");

        }

        private async void btnProjectNew_Click(object sender, RoutedEventArgs e)
        {
            loadingDialog.Update("Resetting", "Resetting");
            await AssetManager.Instance.ResetAsync();
            //LegacyFileManager_FMTV2.CleanUpChunks(true); // no longer needed as it should be handled by the Asset Manager Reset
            ProjectManagement.Project = new FrostbiteProject(AssetManager.Instance, AssetManager.Instance.FileSystem);
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

        }

        public void UpdateAllBrowsers()
        {
            //dataBrowser.UpdateAssetListView();
            //textureBrowser.UpdateAssetListView();
        }

        public async Task UpdateBrowsersAllFull()
        {
            var dataBrowserData = ProjectManagement.Project.AssetManager
                                   .EnumerateEbx().OrderBy(x => x.Path).Select(x => (IAssetEntry)x).ToList();

            var camTextureAssets = new List<IAssetEntry>();
            foreach (var cam in AssetManager.Instance.CustomAssetManagers)
            {
                var camName = cam.Key;
                var camAssets = cam.Value.EnumerateAssets(false).OrderBy(x => x.Path).Select(x => (IAssetEntry)x).ToList();
                //camTextureAssets.AddRange(camAssets.Where(x => x.Name.Contains(".DDS", StringComparison.OrdinalIgnoreCase)));

                await Dispatcher.InvokeAsync(() =>
                {
                    var tabName = camName.Substring(0, 1).ToUpper() + camName.Substring(1);
                    bool mtiExists = false;
                    foreach (var tItem in MainViewer.Items)
                    {
                        if(tItem is MetroTabItem metroTabItem)
                        {
                            if(metroTabItem.Header.ToString() == tabName)
                            {
                                mtiExists = true;
                                break;
                            }
                        }
                    }

                    if (!mtiExists)
                    {
                        MetroTabItem camMetroTabItem = new MetroTabItem();
                        camMetroTabItem.Header = tabName;

                        Browser camBrowser = new Browser();
                        camBrowser.Name = camName;
                        camBrowser.AllAssetEntries = camAssets;
                        camMetroTabItem.Content = camBrowser;

                        MainViewer.Items.Add(camMetroTabItem);
                    }

                });
            }

            List<IAssetEntry> textureAssets = ProjectManagement.Project.AssetManager
                                .EnumerateEbx("TextureAsset").OrderBy(x => x.Path).Select(x => (IAssetEntry)x).ToList();

            List<IAssetEntry> meshAssets = new List<IAssetEntry>();
            if (ProfileManager.CanImportMeshes || ProfileManager.CanExportMeshes)
            {
                meshAssets = AssetManager.Instance
                                 .EnumerateEbx("SkinnedMeshAsset")
                                 .Union(AssetManager.Instance.EnumerateEbx("CompositeMeshAsset"))
                                 .Union(AssetManager.Instance.EnumerateEbx("RigidMeshAsset"))
                                 .OrderBy(x => x.Path).Select(x => (IAssetEntry)x)
                                 .ToList();
            }

            foreach(var caee in AssetManager.Instance.LoadTypesFromPluginByInterface<ICustomAssetEntryEnumerations>(nameof(ICustomAssetEntryEnumerations)))
            {
                if(caee == null)
                    continue;

                var customAssetsEnumerations = caee.GetCustomAssetEntriesEnumerations();
                if (customAssetsEnumerations == null)
                    continue;

                foreach(var customAssetEntries in customAssetsEnumerations)
                {
                    var camName = customAssetEntries.Key;
                    var camAssets = customAssetEntries.Value;
                    await Dispatcher.InvokeAsync(() =>
                    {
                        var tabName = camName.Substring(0, 1).ToUpper() + camName.Substring(1);
                        bool mtiExists = false;
                        foreach (var tItem in MainViewer.Items)
                        {
                            if (tItem is MetroTabItem metroTabItem)
                            {
                                if (metroTabItem.Header.ToString() == tabName)
                                {
                                    mtiExists = true;
                                    break;
                                }
                            }
                        }

                        if (!mtiExists)
                        {
                            MetroTabItem camMetroTabItem = new MetroTabItem();
                            camMetroTabItem.Header = tabName;

                            Browser camBrowser = new Browser();
                            camBrowser.Name = camName;
                            camBrowser.AllAssetEntries = camAssets;
                            camMetroTabItem.Content = camBrowser;

                            MainViewer.Items.Add(camMetroTabItem);
                        }

                    });
                }
            }

            await Dispatcher.InvokeAsync(() =>
            {
                dataBrowser.AllAssetEntries = dataBrowserData;

                textureBrowser.AllAssetEntries = textureAssets;

                if(meshAssets.Count == 0)
                    TabMeshBrowser.Visibility = Visibility.Collapsed;
                else 
                    meshBrowser.AllAssetEntries = meshAssets;
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
            //loadingDialog.Show();
            await Task.Delay(100);

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Project files|*.fbproject";
            var result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                if (!string.IsNullOrEmpty(openFileDialog.FileName))
                {
                    await loadingDialog.UpdateAsync("Loading Project", "Loading and Merging Project File");


                    var mergerProject = new FrostbiteProject();
                    await mergerProject.LoadAsync(openFileDialog.FileName);
                    // A chunk clean up of bad and broken projects
                    await Task.Run(() =>
                    {
                        ChunkFileManager2022.CleanUpChunks();
                    });

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

        public void LogProgress(int progress)
        {
        }

        public void ShowLoadingDialog(string message, string title, int progress)
        {
            loadingDialog.UpdateAsync(title, message, progress);
        }
    }
}
