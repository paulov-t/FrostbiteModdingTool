using FMT.Pages.Common;
using MahApps.Metro.Controls;

namespace FMT.Windows
{
    /// <summary>
    /// Interaction logic for InitfsEditor.xaml
    /// </summary>
    public partial class InitfsEditor : MetroWindow
    {
        public InitfsEditor()
        {
            InitializeComponent();
            this.Content = new BrowserOfInitfs();
        }
    }
}
