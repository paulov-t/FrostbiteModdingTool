using MahApps.Metro.Controls;
using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using static FrostySdk.ProfileManager;

namespace FMT.Windows
{
    /// <summary>
    /// Interaction logic for GameVsEditorVsTools.xaml
    /// </summary>
    public partial class GameVsEditorVsTools : MetroWindow
    {
        Profile SelectedProfile { get; set; }

        public GameVsEditorVsTools(Window owner, Profile selectedProfile)
        {
            InitializeComponent();
            Owner = owner;
            SelectedProfile = selectedProfile;
            btnLoadGameModLauncher.IsEnabled = SelectedProfile.CanLaunchMods;
            btnLoadGameEditor.IsEnabled = SelectedProfile.CanEdit;
            btnLoadGameTools.IsEnabled =
                SelectedProfile.Tools.Internal != null && SelectedProfile.Tools.External != null
                &&
                SelectedProfile.Tools.Internal.Count + SelectedProfile.Tools.External.Count > 0;
        }

        private void btnLoadGameModLauncher_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            LaunchWindow launchWindow = new LaunchWindow(Owner);
            launchWindow.Show();
            this.Close();

        }

        private void btnLoadGameEditor_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = a.GetTypes().FirstOrDefault(x => x.Name.Contains(SelectedProfile.EditorScreen, StringComparison.OrdinalIgnoreCase));
                if (t != null)
                {
                    App.MainEditorWindow = (Window)Activator.CreateInstance(t, Owner);
                    App.MainEditorWindow.Show();
                    this.Close();
                    return;
                }
            }
        }

        private void btnLoadGameTools_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            GameToolsWindow toolsWindow = new GameToolsWindow(Owner);
            toolsWindow.Show();
            this.Close();
        }
    }
}
