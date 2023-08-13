using FIFAModdingUI.Windows;
using FMT.FileTools;
using FMT.FileTools.Modding;
using FMT.Logging;
using FMT.Windows;
using FrostbiteModdingUI.Models;
using FrostbiteModdingUI.Windows;
using FrostySdk;
using FrostySdk.Managers;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using v2k4FIFAModdingCL;
using static FrostySdk.ProfileManager;

//namespace FIFAModdingUI
namespace FMT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public List<Window> EditorWindows = new List<Window>();

        private List<Profile> profiles;

        public List<Profile> ProfilesWithEditor
        {
            get
            {

                if (profiles == null || !profiles.Any())
                    profiles = ProfileManager.EditorProfiles.ToList();
                return profiles;

            }
            set { profiles = value; }
        }

        public string WindowTitle { get; set; }


        public MainWindow()
        {
            InitializeComponent();

            WindowTitle = "Frostbite Modding Tool " + App.ProductVersion;

            if (AssetManager.Instance != null)
            {
                AssetManager.Instance.Dispose();
                AssetManager.Instance = null;
            }
            DataContext = this;

            IsVisibleChanged += MainWindow_IsVisibleChanged;


            Loaded += MainWindow_Loaded;
        }

        private void InitializeOfSelectedGame(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                AppSettings.Settings.GameInstallEXEPath = filePath;

                if (GameInstanceSingleton.InitializeSingleton(filePath, false))
                {
                    DialogResult = true;
                }
                else
                {
                    throw new FileNotFoundException($"Unable to initialise against {filePath}");
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.StartupArgs.Length == 0)
                return;

            for (int i = 0; i < App.StartupArgs.Length; ++i)
            {
                FileLogger.WriteLine($"Arg {i}: {App.StartupArgs[i]}");

                if (App.StartupArgs[i].Contains("Game="))
                {
                    var splitArg = App.StartupArgs[i].Split("=");
                    if (splitArg.Length == 2)
                    {
                        var gameProfile = splitArg[1];
                        if (!string.IsNullOrEmpty(gameProfile) && ProfilesWithEditor.Any(x => x.Name == gameProfile))
                        {
                            var SelectedProfile = ProfilesWithEditor.Single(x => x.Name == gameProfile);
                            ProfileManager.Initialize(SelectedProfile.Name);
                            if (App.StartupArgs.Contains("Editor"))
                            {
                                var bS = new FindGameEXEWindow().ShowDialog();
                                if (bS.HasValue && bS.Value == true && !string.IsNullOrEmpty(AppSettings.Settings.GameInstallEXEPath))
                                {
                                    foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                                    {
                                        var t = a.GetTypes().FirstOrDefault(x => x.Name.Contains(SelectedProfile.EditorScreen, StringComparison.OrdinalIgnoreCase));
                                        if (t != null)
                                        {
                                            App.MainEditorWindow = (Window)Activator.CreateInstance(t, this);
                                            App.MainEditorWindow.Show();
                                            // Empty the Args so we don't do this again once the App has loaded
                                            App.StartupArgs = new string[0];
                                            return;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                OpenGame(SelectedProfile);
                            }
                        }
                        
                    }
                }
            }
        }

        private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(Visibility == Visibility.Visible)
            {
                GCHelpers.ClearGarbage(true);
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            foreach (var w in EditorWindows)
            {
                if (w != null)
                    w.Close();
            }

            EditorWindows.Clear();

            if (App.MainEditorWindow != null)
                App.MainEditorWindow.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (App.MainEditorWindow != null)
                App.MainEditorWindow.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (App.MainEditorWindow != null)
                App.MainEditorWindow.Close();

            Application.Current.Shutdown();
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnEditor_Click(object sender, RoutedEventArgs e)
        {
            //new EditorLoginWindow().Show();
            App.MainEditorWindow = new FIFA21Editor(this);
            if (App.MainEditorWindow != null)
                App.MainEditorWindow.Show();

            this.Visibility = Visibility.Hidden;
        }

        private void btnLauncher_Click(object sender, RoutedEventArgs e)
        {
            var lw = new LaunchWindow(this);
            try
            {
                if (lw != null)
                {
                    lw.Show();
                    this.Visibility = Visibility.Hidden;
                }
            }
            catch
            {

            }
        }

        private void cbLanguageSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbLanguageSelection.SelectedItem != null && cbLanguageSelection.SelectedIndex >= 1)
            {
                string selectedLanguage = null;
                var selectedItem = ((ComboBoxItem)cbLanguageSelection.SelectedItem).Content.ToString();
                switch (selectedItem)
                {
                    case "English":
                        selectedLanguage = "en";
                        break;
                    case "Deutsch":
                        selectedLanguage = "de";
                        break;
                    case "Português":
                        selectedLanguage = "pt";
                        break;
                }

                if (!string.IsNullOrEmpty(selectedLanguage))
                {
                    App.LoadLanguageFile(selectedLanguage);
                }
            }
        }



        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            Profile profile = (Profile)((Tile)sender).Tag;
            ProfileManager.LoadedProfile = profile;
            OpenGame(profile);
        }

        private void OpenGame(Profile profile)
        {
            var bS = new FindGameEXEWindow().ShowDialog();
            if (bS.HasValue && bS.Value == true && !string.IsNullOrEmpty(AppSettings.Settings.GameInstallEXEPath))
            {
                if (new FileInfo(AppSettings.Settings.GameInstallEXEPath).Name.Replace(".exe", "").Replace(" ", "") != profile.Name.Replace(" ", ""))
                {
                    MessageBox.Show("Your EXE does not match the Profile selected!");
                    return;
                }

                var gameveditordialog = new GameVsEditorVsTools(this, profile).ShowDialog();
                if (bS.HasValue && bS.Value == true)
                    this.Visibility = Visibility.Hidden;
                else
                    this.Visibility = Visibility.Visible;

            }
        }
    }
}
