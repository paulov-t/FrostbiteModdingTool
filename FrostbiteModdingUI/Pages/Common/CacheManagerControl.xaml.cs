using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Interfaces;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using v2k4FIFAModdingCL;

namespace FMT.Pages.Common
{
    /// <summary>
    /// Interaction logic for CacheManagerControl.xaml
    /// </summary>
    public partial class CacheManagerControl : System.Windows.Controls.UserControl, ILogger
    {
        private CacheManager buildCache = new CacheManager();
        public bool AutoRebuild = true;

        public CacheManagerControl()
        {
            InitializeComponent();

            //Loaded += CacheManagerControl_Loaded;
            IsVisibleChanged += CacheManagerControl_IsVisibleChanged;
        }

        private async void CacheManagerControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                if (AutoRebuild)
                    await Rebuild();

                GCHelpers.ClearGarbage(true);
            }
        }

        //private async void CacheManagerControl_Loaded(object sender, RoutedEventArgs e)
        //{
        //    //    if (Visibility == Visibility.Visible)
        //    //    {
        //    //        if (AutoRebuild)
        //    //            await Rebuild();
        //    //    }
        //}


        public async Task Rebuild(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (CacheManager.DoesCacheNeedsRebuilding())
            {
                await Task.Delay(1000);

                await Dispatcher.InvokeAsync(() => { btnRebuild.IsEnabled = false; });
                await Dispatcher.InvokeAsync(() => { txtOuputMessage.Text = "Building Cache. Please wait 3-15 minutes to complete!"; });

                if (FileSystem.Instance == null)
                    new FileSystem(GameInstanceSingleton.Instance.GAMERootPath);
                // -----------------------------------------
                //
                await buildCache.LoadDataAsync(GameInstanceSingleton.Instance.GAMEVERSION, FileSystem.Instance.BasePath, this, true, false);

                AssetManager.Instance.FullReset();
                AssetManager.Instance.Dispose();
                AssetManager.Instance = null;

                await buildCache.LoadDataAsync(GameInstanceSingleton.Instance.GAMEVERSION, GameInstanceSingleton.Instance.GAMERootPath, this, false, true);

                await Task.Delay(2000);

                await Dispatcher.InvokeAsync(() => { btnRebuild.IsEnabled = true; });

            }

            await Dispatcher.InvokeAsync(() => { this.Visibility = Visibility.Collapsed; });
        }

        public void Dispose()
        {
        }



        public string LogText = string.Empty;

        public void Log(string text, params object[] vars)
        {
            //if (LastMessage == text)
            //    return;

            //LastMessage = text;

            //Dispatcher.Invoke(() => { txtOutputSubMessage.Content = text; });
            //txtOutputSubMessage.Content = text;
            LogAsync(text);
        }

        private string LastMessage = string.Empty;

        public async Task LogAsync(string text)
        {
            if (LastMessage == text)
                return;

            LastMessage = text;

            await Dispatcher.InvokeAsync(() => { txtOutputSubMessage.Text = text; });

        }

        public void LogWarning(string text, params object[] vars)
        {
            Debug.WriteLine("[WARNING] " + text);
            LogAsync("[WARNING] " + text);
        }

        public void LogError(string text, params object[] vars)
        {
            Debug.WriteLine("[ERROR] " + text);
            LogAsync("[ERROR] " + text);
        }

        private async void btnRebuild_Click(object sender, RoutedEventArgs e)
        {
            await Rebuild();
        }
    }
}
