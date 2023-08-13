using FIFAModdingUI.Pages.Common;
using FMT.FileTools;
using FrostySdk.Managers;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FrostbiteModdingUI.Windows
{
    /// <summary>
    /// Interaction logic for MeshSkeletonSelector.xaml
    /// </summary>
    public partial class MeshSkeletonSelector : Window
    {
        public EbxAssetEntry AssetEntry { get; set; }

        public Browser ParentBrowser { get; set; }

        public MeshSkeletonSelector()
        {
            InitializeComponent();

            assetListView.ItemsSource = null;
            assetListView.ItemsSource = AssetManager.Instance
                                   .EnumerateEbx()
                                   .Where(x => x.Type == "SkeletonAsset")
                                   .OrderBy(x => x.Path).Select(x => (IAssetEntry)x).ToList();

        }



        private void assetListView_Selected(object sender, RoutedEventArgs e)
        {
            ListView listView = sender as ListView;

        }

        private void assetListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView listView = sender as ListView;
            AssetEntry = listView.SelectedItem as EbxAssetEntry;
            DialogResult = true;
            this.Close();
        }
    }
}
