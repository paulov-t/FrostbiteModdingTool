using FrostySdk.Managers;
using System;
using System.Windows;

namespace FMT.Controls.Windows
{
    /// <summary>
    /// Interaction logic for DuplicateItem.xaml
    /// </summary>
    public partial class DuplicateItem : Window
    {
        public AssetEntry EntryToDuplicate { get; set; }

        public bool IsLegacy { get; set; }

        public string NewEntryPath { get; set; }


        public DuplicateItem()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        public static Random ResRidRandomizer = new Random();

        private void btnDuplicate_Click(object sender, RoutedEventArgs e)
        {
            AssetManager.Instance.DuplicateEntry(EntryToDuplicate, NewEntryPath);

            DialogResult = true;
            this.Close();
        }


    }
}
