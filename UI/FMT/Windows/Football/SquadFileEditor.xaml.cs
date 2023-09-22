using AvalonDock.Layout;
using FifaLibrary;
using FMT.Controls.Controls;
using FMT.Controls.Pages;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace FMT.Windows.Football
{
    /// <summary>
    /// Interaction logic for SquadFileEditor.xaml
    /// </summary>
    public partial class SquadFileEditor : MetroWindow
    {
        public SquadFileEditor()
        {
            InitializeComponent();
        }

        private void btnOpenDbFile_Click(object sender, RoutedEventArgs e)
        {
            if (CareerFile.Current != null)
            {
                CareerFile.Current.Dispose();
                CareerFile.Current = null;
            }

            FileInfo fiSquadFile = null;
            FileInfo fiMetaFile = null;

            OpenFileDialog openFileDialogSquadFile = new OpenFileDialog();
            openFileDialogSquadFile.Filter = "";
            if (openFileDialogSquadFile.ShowDialog().Value)
            {
                fiSquadFile = new FileInfo(openFileDialogSquadFile.FileName);
            }

            OpenFileDialog openFileDialogMetaFile = new OpenFileDialog();
            openFileDialogMetaFile.Filter = "*.xml|*.xml";
            if (openFileDialogMetaFile.ShowDialog().Value)
            {
                fiMetaFile = new FileInfo(openFileDialogMetaFile.FileName);
            }


            if (!fiSquadFile.Exists)
                return;

            if (!fiMetaFile.Exists)
                return;

          
            MemoryStream msSquadFile = new MemoryStream();
            using (var fsSquadFile = new FileStream(fiSquadFile.FullName, FileMode.Open))
            {
                fsSquadFile.CopyTo(msSquadFile);
            }

            MemoryStream msMetaFile = new MemoryStream();
            using (var fsMetaFile = new FileStream(fiMetaFile.FullName, FileMode.Open))
            {
                fsMetaFile.CopyTo(msMetaFile);
            }

            try
            {

                CareerFile careerFile = new CareerFile(msSquadFile, new FileStream(fiMetaFile.FullName, FileMode.Open), "");

                lbTables.ItemsSource = null;
                lbTables.ItemsSource = careerFile.Databases[0].GetTables().ToList().OrderBy(x => x.ToString());
            }
            catch { }

        }

        private void btnSaveDbFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "";
                if (saveFileDialog.ShowDialog() == true)
                {

                }
            }
            catch (Exception ex)
            {
            }
        }

        private void lbTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FifaTable fifaTable = e.AddedItems[0] as FifaTable;

        }
    }
}
