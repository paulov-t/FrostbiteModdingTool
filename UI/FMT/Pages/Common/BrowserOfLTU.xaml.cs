using FrostySdk;
using System.Linq;
using System.Windows.Controls;

namespace FMT.Pages.Common
{
    /// <summary>
    /// Interaction logic for BrowserOfInitfs.xaml
    /// </summary>
    public partial class BrowserOfLTU : UserControl
    {
        public BrowserOfLTU()
        {
            InitializeComponent();
            Load();
        }

        public void Load()
        {

            RefreshList();
        }

        private void RefreshList()
        {
            ltuBrowser.AllAssetEntries = FileSystem.Instance.LiveTuningUpdate.LiveTuningUpdateEntries.Select(x => x.Value);
        }

    }
}
