using FMT.Pages.Common;
using System.Windows;

namespace FMT.Windows
{
    /// <summary>
    /// Interaction logic for LTUEditor.xaml
    /// </summary>
    public partial class LTUEditor : Window
    {
        public LTUEditor()
        {
            InitializeComponent();
            this.Content = new BrowserOfLTU();
        }
    }
}
