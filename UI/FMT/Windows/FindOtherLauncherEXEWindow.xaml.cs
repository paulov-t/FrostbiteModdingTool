using FrostySdk;
using Microsoft.Win32;
using ModdingSupport;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using v2k4FIFAModdingCL;

namespace FrostbiteModdingUI.Windows
{
    /// <summary>
    /// Interaction logic for FindOtherLauncherEXEWindow.xaml
    /// </summary>
    public partial class FindOtherLauncherEXEWindow : Window
    {
        public FindOtherLauncherEXEWindow()
        {
            InitializeComponent();
        }

        public bool InjectLegacyModSupport { get; set; }
        public bool InjectLiveEditorSupport { get; set; }

        public static string OtherToolPath { get; set; }

        private void btnFindOtherTool_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "EXE files (*.exe)|*.exe";
            var ok = openFileDialog.ShowDialog();
            if (ok.HasValue && ok.Value)
            {
                OtherToolPath = openFileDialog.FileName;
                txtToolLocation.Text = OtherToolPath;
                btnLaunchOtherTool.IsEnabled = true;
            }
        }
        private async void btnLaunchOtherTool_Click(object sender, RoutedEventArgs e)
        {
            ModExecutor frostyModExecutor = new ModExecutor();
            _ = frostyModExecutor.ExecuteProcess(OtherToolPath, "");

            if (InjectLegacyModSupport)
            {
                var runningLocation = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                LogAsync("Legacy Injection - Tool running location is " + runningLocation);

                string legacyModSupportFile = null;
                if (GameInstanceSingleton.Instance.GAMEVERSION == "FIFA20")
                {
                    LogAsync("Legacy Injection - FIFA 20 found. Using FIFA20Legacy.DLL.");
                    legacyModSupportFile = runningLocation + @"\FIFA20Legacy.dll";
                }
                else if (ProfileManager.IsFIFA21DataVersion())// GameInstanceSingleton.Instance.GAMEVERSION == "FIFA21")
                {
                    LogAsync("Legacy Injection - FIFA 21 found. Using FIFA.DLL.");
                    legacyModSupportFile = runningLocation + @"\FIFA.dll";
                }

                if (!File.Exists(legacyModSupportFile))
                {
                    LogAsync($"Legacy Injection - Unable to find Legacy Injection DLL {legacyModSupportFile}");
                }
                else
                {
                    var legmodsupportdllpath = @GameInstanceSingleton.Instance.GAMERootPath + @"v2k4LegacyModSupport.dll";

                    LogAsync("Copying " + legacyModSupportFile + " to " + legmodsupportdllpath);
                    await Task.Delay(500);
                    File.Copy(legacyModSupportFile, legmodsupportdllpath, true);
                    await Task.Delay(500);

                    try
                    {
                        LogAsync("Injecting Live Legacy Mod Support");
                        await Task.Delay(500);
                        bool InjectionSuccess = await GameInstanceSingleton.InjectDLLAsync(legmodsupportdllpath);
                        if (InjectionSuccess)
                            LogAsync("Injected Live Legacy Mod Support");
                        else
                            LogAsync("Launcher could not inject Live Legacy File Support");

                    }
                    catch (Exception InjectDLLException)
                    {
                        LogAsync("Launcher could not inject Live Legacy File Support");
                        LogAsync(InjectDLLException.ToString());
                    }
                }
            }



        }


        public async void LogAsync(string in_text)
        {
            var txt = string.Empty;
            Dispatcher.Invoke(() =>
            {
                txt = txtLog.Text;
            });

            var text = await Task.Run(() =>
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.Append(txt);
                stringBuilder.AppendLine(in_text);

                return stringBuilder.ToString();
            });

            await Dispatcher.InvokeAsync(() =>
            {
                txtLog.Text = text;
                txtLog.ScrollToEnd();
            });

        }


    }
}
