using System.Windows;
using v2k4FIFAModding.Frosty;

namespace FrostbiteModdingUI.Windows
{
    /// <summary>
    /// Interaction logic for ModDetailsWindow.xaml
    /// </summary>
    public partial class ModDetailsWindow : Window
    {
        public ModDetailsWindow()
        {
            InitializeComponent();
            this.DataContext = ProjectManagement.Instance.Project.ModSettings;
        }

        private async void SaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() => { 
                this.IsEnabled = false;
            });
            await ProjectManagement.Instance.Project.SaveAsync(null, true);
            this.Close();
        }
    }
}
