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
using System.Windows.Media.Animation;
using v2k4FIFAModdingCL;

namespace FMT.Pages.Common
{
    /// <summary>
    /// Interaction logic for CacheManagerControl.xaml
    /// </summary>
    public partial class CacheManagerControl : System.Windows.Controls.UserControl, ILogger
    {
        private CacheManager buildCache = new CacheManager();
        public bool AutoRebuild { get; set; } = true;
        public bool AutoClose { get; set; } = true;

        public CacheManagerControl()
        {
            InitializeComponent();

            //Loaded += CacheManagerControl_Loaded;
            IsVisibleChanged += CacheManagerControl_IsVisibleChanged;
            txtOuputMessage.Text = string.Empty;
            txtOutputSubMessage.Text = string.Empty;
        }

        private async void CacheManagerControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                if (AutoRebuild)
                    await Rebuild();

                GCHelpers.ClearGarbage(true);

                if (AutoClose)
                    await Dispatcher.InvokeAsync(() => { this.Visibility = Visibility.Collapsed; });
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


        public async Task Rebuild(CancellationToken cancellationToken = default(CancellationToken), bool forceRebuild = false)
        {
            await Dispatcher.InvokeAsync(() => { btnRebuild.Visibility = Visibility.Collapsed; });

            if (forceRebuild || CacheManager.DoesCacheNeedsRebuilding())
            {
                Stopwatch sw = Stopwatch.StartNew();
                await Task.Delay(1000);

                await Dispatcher.InvokeAsync(() => { txtOuputMessage.Text = "Building Cache. Please wait 1-15 minutes to complete!"; });
                await Dispatcher.InvokeAsync(() => { txtOutputSubMessage.Text = string.Empty; });

                if (FileSystem.Instance == null)
                    new FileSystem(GameInstanceSingleton.Instance.GAMERootPath);
                // -----------------------------------------
                //
                await buildCache.LoadDataAsync(GameInstanceSingleton.Instance.GAMEVERSION, FileSystem.Instance.BasePath, this, true, false);

                AssetManager.Instance.FullReset();
                AssetManager.Instance.Dispose();
                AssetManager.Instance = null;

                if (FileSystem.Instance == null)
                    new FileSystem(GameInstanceSingleton.Instance.GAMERootPath);

                await buildCache.LoadDataAsync(GameInstanceSingleton.Instance.GAMEVERSION, FileSystem.Instance.BasePath, this, false, true);

                await Task.Delay(2000);
                await Dispatcher.InvokeAsync(() => { txtOuputMessage.Text = $"Completed Cache Build in {sw.Elapsed}"; });
                await Dispatcher.InvokeAsync(() => { txtOutputSubMessage.Text = string.Empty; });
                sw.Stop();
                sw = null;

            }

            await Dispatcher.InvokeAsync(() => { btnRebuild.Visibility = Visibility.Visible; });

            if (AutoClose)
                await Dispatcher.InvokeAsync(() => { this.Visibility = Visibility.Collapsed; });
        }

        public void Dispose()
        {
        }



        public string LogText = string.Empty;

        public void Log(string text, params object[] vars)
        {
            _ = LogAsync(text);
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
            _ = LogAsync("[WARNING] " + text);
        }

        public void LogError(string text, params object[] vars)
        {
            Debug.WriteLine("[ERROR] " + text);
            _ = LogAsync("[ERROR] " + text);
        }

        private async void btnRebuild_Click(object sender, RoutedEventArgs e)
        {
            await Rebuild(forceRebuild: true);
        }
    }
}
