using FrostySdk.Managers;
using MahApps.Metro.Controls;
using System;
using System.Text;
using System.Windows;

namespace FMT.Windows
{
    /// <summary>
    /// Interaction logic for LocaleINIEditor.xaml
    /// </summary>
    public partial class LocaleINIEditor : MetroWindow
    {
        public LocaleINIEditor()
        {
            InitializeComponent();
            var d = Data;

            this.DataContext = null;
            this.DataContext = this;
        }

        private string data;
        public string Data
        {
            get
            {
                if (data == null)
                    data = Encoding.UTF8.GetString(AssetManager.Instance.LocaleINIMod.OriginalData);

                if (AssetManager.Instance.LocaleINIMod.UserData != null && AssetManager.Instance.LocaleINIMod.UserData.Length > 0)
                    data = Encoding.UTF8.GetString(AssetManager.Instance.LocaleINIMod.UserData);

                return data;
            }
            set
            {
                data = value;
                AssetManager.Instance.LocaleINIMod.UserData = Encoding.UTF8.GetBytes(value);
            }
        }

        protected override void OnContentRendered(EventArgs e)
        {

            base.OnContentRendered(e);

        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            //AssetManager.Instance.LocaleINIMod = new FrostySdk.Frostbite.IO.LocaleINIMod();
            AssetManager.Instance.LocaleINIMod.Reset();
            this.DataContext = null;
            this.DataContext = this;
        }
    }
}
