using FIFAModdingUI.Windows.Profile;
using FMT.FileTools;
using FrostySdk;
using FrostySdk.FrostbiteSdk.Managers;
using FrostySdk.Managers;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

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

            Directory.CreateDirectory(CachedEXEStringDirectoryPath);

            this.DataContext = null;
            this.DataContext = this;
        }

        #region Properties

        private string data;
        public string Data
        {
            get
            {
                if (data == null && AssetManager.Instance.LocaleINIMod.OriginalData != null)
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

        public static string[] EXEStrings { get; set; }
        public static string CachedEXEStringPath { get; } = $"{CachedEXEStringDirectoryPath}EXEStringCache.json";
        public static string CachedEXEStringDirectoryPath { get; } = "_GameCaches\\" + ProfileManager.Instance.Name + "\\";

        #endregion

        protected override async void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);


            //GenerateStrings();
        }

        private async Task GenerateStrings()
        {
            List<string> list = new();
            if (RegistryManager.FoundExe)
            {
                var exeBytes = await File.ReadAllBytesAsync(RegistryManager.GamePathEXE);
                using var nr = new NativeReader(exeBytes);
                while(nr.Position < nr.Length)
                {
                    var nts = nr.ReadNullTerminatedString();
                    if (string.IsNullOrEmpty(nts))
                        continue;

                    if (nts.Contains("\\u"))
                        continue;
                    
                    if(nts.StartsWith("`"))
                        continue;

                    if (nts.Length < 3)
                        continue;

                    if (!Regex.IsMatch(nts, "([a-zA-Z])\\w+"))
                        continue;

                    list.Add(nts);
                }
            }

            if(list.Count > 0)
            {
                EXEStrings = list.ToArray();
            }
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
