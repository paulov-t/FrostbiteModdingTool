﻿using FIFAModdingUI.Windows;
using FrostbiteModdingUI.Windows;
using FrostySdk;
using FrostySdk.Managers;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static FrostySdk.ProfilesLibrary;

//namespace FIFAModdingUI
namespace FMT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<Window> EditorWindows = new List<Window>();

        //public List<Profile> ProfilesWithEditorScreen = ProfilesLibrary.EditorProfiles.ToList();

        private List<Profile> profiles;

        public List<Profile> ProfilesWithEditor
        {
            get {

#pragma warning disable CA1416 // Validate platform compatibility
                if (profiles == null || !profiles.Any())
                    profiles = ProfilesLibrary.EditorProfiles.ToList();
                return profiles;
#pragma warning restore CA1416 // Validate platform compatibility

            }
            set { profiles = value; }
        }

        public string WindowTitle { get; set; }


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MainWindow()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            InitializeComponent();

            var assembly = Assembly.GetExecutingAssembly();
            //WindowTitle = "Frostbite Modding Tool " + System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;
            WindowTitle = "Frostbite Modding Tool " + System.Diagnostics.FileVersionInfo.GetVersionInfo(System.AppContext.BaseDirectory + assembly.ManifestModule.Name).ProductVersion;

            //App.AppInsightClient.TrackPageView("MainWindow");

            // This is unfinished. The plugins need to be loaded to find any of the editor windows to load them dynamically
            DataContext = this;

            // ------------------------------------------


        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            foreach (var w in EditorWindows)
            {
                if (w != null)
                    w.Close();
            }

            EditorWindows.Clear();

            //foreach (var pr in ProfilesWithEditorScreen)
            //{

            //}

            //ProfilesWithEditorScreen.Clear();

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
            if(App.MainEditorWindow != null)
                App.MainEditorWindow.Show();

            this.Visibility = Visibility.Hidden;
        }

        private void btnMadden21Editor_Click(object sender, RoutedEventArgs e)
        {
            App.MainEditorWindow = new Madden21Editor(this);
            App.MainEditorWindow.Show();
            this.Visibility = Visibility.Hidden;
        }

        private void btnLauncher_Click(object sender, RoutedEventArgs e)
        {
            new LaunchWindow(this).Show();
            this.Visibility = Visibility.Hidden;
        }

        private void btnBF4Editor_Click(object sender, RoutedEventArgs e)
        {
            App.MainEditorWindow = new BF4Editor(this);
            App.MainEditorWindow.Show();
            this.Visibility = Visibility.Hidden;
        }

        private void lstProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstProfiles.SelectedItem != null)
            {
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var t = a.GetTypes().FirstOrDefault(x => x.Name.Contains(((Profile)lstProfiles.SelectedItem).EditorScreen, StringComparison.OrdinalIgnoreCase));
                    if (t != null)
                    {
                        App.MainEditorWindow = (Window)Activator.CreateInstance(t, this);
                        App.MainEditorWindow.Show();
                        lstProfiles.SelectedItem = null;
                        this.Visibility = Visibility.Hidden;
                        return;
                    }
                }
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
    }
}
