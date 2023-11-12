using AvalonDock.Layout;
using FifaLibrary;
using FMT.Controls.Controls;
using FMT.Controls.Pages;
using FrostbiteSdk;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (CareerFile.Current != null)
            {
                CareerFile.Current.Dispose();
                CareerFile.Current = null;
            }
        }

        private void btnOpenDbFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CareerFile.Current != null)
                {
                    CareerFile.Current.Dispose();
                    CareerFile.Current = null;
                }

                FileInfo fiSquadFile = null;
                FileInfo fiMetaFile = null;

                OpenFileDialog openFileDialogSquadFile = new OpenFileDialog();
                openFileDialogSquadFile.Filter = "Squads Files|Squads*|Career Files|Career*";
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

                    var careerFile = new CareerFile(msSquadFile, new FileStream(fiMetaFile.FullName, FileMode.Open), "");

                    lbTables.ItemsSource = null;
                    lbTables.ItemsSource = CurrentCareerFile.Databases[0].GetTables().ToList().OrderBy(x => x.ToString());
                }
                catch { }

            }
            catch (Exception)
            {

            }
        }

        private void btnSaveDbFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "";
                if (saveFileDialog.ShowDialog() == true)
                {
                    CurrentCareerFile.SaveEa(saveFileDialog.FileName);
                }
            }
            catch (Exception)
            {
            }
        }

        private CareerFile CurrentCareerFile { get { return CareerFile.Current; } }
        private FifaTable CurrentViewedTable { get; set; }

        private void lbTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentViewedTable = e.AddedItems[0] as FifaTable;
            dgTable.ItemsSource = null;
            var dt = CurrentViewedTable.ConvertToDataTable();
            var dv = dt.AsDataView();
            if (dv.Table.Columns.Contains("PlayerID"))
            {
                dv.Sort = "PlayerID ASC";
            }
            dv.AllowNew = false;
            dv.AllowDelete = false;
            dv.AllowEdit = true;
            dgTable.ItemsSource = dv;
        }

        private void txtColumnFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var textboxSearch = e.Source as TextBox;
                var dv = dgTable.ItemsSource as DataView;
                var validFilter = textboxSearch.Text.Split(' ').Length > 1 && (textboxSearch.Text.Contains("=") || textboxSearch.Text.Contains("<") || textboxSearch.Text.Contains(">"));
                if (validFilter && dv.Table.Columns.Contains(textboxSearch.Text.Split(' ')[0]))
                    dv.RowFilter = textboxSearch.Text;
                else
                    dv.RowFilter = string.Empty;


            }
            catch 
            { 
            }
        }

        private void dgTable_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var column = e.Column as DataGridBoundColumn;
                if (column != null)
                {
                    var dataGridRow = e.Row;
                    var bindingPath = (column.Binding as Binding).Path.Path;
                    int rowIndex = e.Row.GetIndex();
                    var el = e.EditingElement as TextBox;
                    // rowIndex has the row index
                    // bindingPath has the column's binding
                    // el.Text has the new, user-entered value
                    var dv = dgTable.ItemsSource as DataView;
                    CurrentViewedTable.ConvertFromDataTable(dv.Table);
                }
            }
        }
    }
}
