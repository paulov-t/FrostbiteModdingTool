using FrostySdk.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FrostbiteModdingUI.Pages.Common.EBX
{
    /// <summary>
    /// Interaction logic for ListControl.xaml
    /// </summary>
    public partial class ListControl : UserControl
    {
        List<object> ContextDataList;
        EbxAsset ParentAsset;


        public ListControl()
        {
            InitializeComponent();
        }

        public ListControl(List<object> inList, EbxAsset inAsset)
        {
            ContextDataList = inList;
            ParentAsset = inAsset;
            DataContext = ContextDataList;

            InitializeComponent();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
