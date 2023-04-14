using FrostbiteModdingUI.Models;
using FrostySdk;
using FrostySdk.Managers;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using v2k4FIFAModding.Frosty;

namespace FMT.Windows
{
    /// <summary>
    /// Interaction logic for GameToolsWindow.xaml
    /// </summary>
    public partial class GameToolsWindow : MetroWindow
    {
        public GameToolsWindow(Window ownerWindow)
        {
            Owner = ownerWindow;
            InitializeComponent();

            lstInternalSelected.ItemsSource = new ObservableCollection<ProfileManager.Tools.InternalTool>(ProfileManager.LoadedProfile.Tools.Internal);
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                if (Owner != null && Owner.Visibility == Visibility.Hidden)
                    Owner.Visibility = Visibility.Visible;
            }
            catch
            { }

            base.OnClosed(e);
        }

        private void lstInternalSelected_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = a.GetTypes().FirstOrDefault(x => x.Name.Contains(lstInternalSelected.SelectedItem.ToString(), StringComparison.OrdinalIgnoreCase));
                if (t != null)
                {
                    App.MainEditorWindow = (Window)Activator.CreateInstance(t, Owner);
                    App.MainEditorWindow.Show();
                    this.Close();
                    return;
                }
            }
        }

        private void lstExternal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
