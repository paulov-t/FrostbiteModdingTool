using FMT.FileTools;
using FMT.Logging;
using FrostbiteSdk;
using FrostySdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static FrostySdk.ProfileManager;

//namespace FIFAModdingUI
namespace FMT
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Window MainEditorWindow;

        public static string ApplicationDirectory
        {
            get
            {
                return System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\";
            }
        }

        public static string ProductVersion
        {
            get
            {
                //var assembly = Assembly.GetExecutingAssembly();
                //if (assembly != null && AppContext.BaseDirectory != null)
                //{
                //    return FileVersionInfo.GetVersionInfo(AppContext.BaseDirectory + assembly.ManifestModule.Name).ProductVersion;
                //}
                //return string.Empty;

                return Assembly.GetEntryAssembly().GetName().Version.ToString();
            }
        }

        private List<Profile> profiles;

        public List<Profile> ProfilesWithEditor
        {
            get
            {

                if (profiles == null || !profiles.Any())
                    profiles = ProfileManager.EditorProfiles.ToList();
                return profiles;

            }
            set { profiles = value; }
        }

        public static string[] StartupArgs { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            StartupArgs = e.Args;
            
            FileLogger.WriteLine("FMT:OnStartup");

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);

            // --------------------------------------------------------------
            // Run the Powershell DLL
            //UnblockAllDLL();

            // --------------------------------------------------------------
            // Discord Startup
            _ = StartDiscordRPC();

            // --------------------------------------------------------------
            // Application Insights
            //StartApplicationInsights();

            // --------------------------------------------------------------
            // Language Settings
            LoadLanguageFile();

            base.OnStartup(e);

            FileLogger.WriteLine("FMT:OnStartup:Complete");

        }

        private async Task StartDiscordRPC()
        {
            //DiscordRpcClient = new DiscordRPC.DiscordRpcClient("836520037208686652");
            //DiscordRpcClient.Initialize();
            //var presence = new DiscordRPC.RichPresence();
            //presence.State = "In Main Menu";
            //DiscordRpcClient.SetPresence(presence);
            //DiscordRpcClient.Invoke();

            //await Task.Delay(1000);
            await new DiscordInterop().StartDiscordClient("V." + ProductVersion);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            foreach (var file in new DirectoryInfo(AppContext.BaseDirectory).GetFiles())
            {
                if (file.Name.Contains("temp_") && file.Name.Contains(".DDS"))
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {

                    }
                }

                if (file.Name.Contains("autosave", StringComparison.OrdinalIgnoreCase))
                {
                    if (file.CreationTime < DateTime.Now.AddDays(-2))
                    {
                        file.Delete();
                    }
                }
            }
        }

        private void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;

            FileLogger.WriteLine($"{e}");

            if (File.Exists("ErrorLogging.txt"))
                File.Delete("ErrorLogging.txt");

            using (StreamWriter stream = new StreamWriter("ErrorLogging.txt"))
            {
                stream.WriteLine(e.ToString());
            }

            Trace.WriteLine(e.ToString());
            Console.WriteLine(e.ToString());
            Debug.WriteLine(e.ToString());

            MessageBoxResult result = MessageBox.Show(e.ToString());
        }

        public static void LoadLanguageFile(string newLanguage = null)
        {
            var englishCulture = new CultureInfo("en-US");

            try
            {
                if (string.IsNullOrEmpty(newLanguage))
                {
                    newLanguage = Thread.CurrentThread.CurrentCulture.ToString().Substring(0, 2);

                    // Fix Issue: 34
                    // Force English Culture number format of dot for floats / currancy
                    //var testCulture = new CultureInfo("es-ES");
                    Thread.CurrentThread.CurrentCulture = englishCulture;
                    Thread.CurrentThread.CurrentUICulture = englishCulture;
                }

                // prefix to the relative Uri for resource (xaml file)
                string _prefix = String.Concat(typeof(App).Namespace, ";component/");

                // clear all ResourceDictionaries
                var currentLanguage = Application.Current.Resources.MergedDictionaries.FirstOrDefault(x => x.Source != null && x.Source.ToString().Contains("Languages"));
                if (currentLanguage != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(currentLanguage);
                }
                // get correct file
                string filename = "";
                switch (newLanguage)
                {
                    case "pt":
                        filename = "Resources\\Languages\\Portugese.xaml";
                        break;
                    case "de":
                        filename = "Resources\\Languages\\German.xaml";
                        break;
                    case "en":
                        filename = "Resources\\Languages\\English.xaml";
                        Thread.CurrentThread.CurrentCulture = englishCulture;
                        Thread.CurrentThread.CurrentUICulture = englishCulture;
                        break;
                    case "es":
                        filename = "Resources\\Languages\\Spanish.xaml";
                        break;
                    default:
                        filename = "Resources\\Languages\\English.xaml";
                        Thread.CurrentThread.CurrentCulture = englishCulture;
                        Thread.CurrentThread.CurrentUICulture = englishCulture;
                        break;
                }

                // add ResourceDictionary
                Application.Current.Resources.MergedDictionaries.Add
                (
                 //new ResourceDictionary { Source = new Uri(String.Concat(_prefix + filename), UriKind.Relative) }
                 new ResourceDictionary { Source = new Uri(String.Concat(filename), UriKind.Relative) }
                );

               

            }
            catch (Exception ex)
            {
                FileLogger.WriteLine(ex.ToString());
                //throw ex;
            }
        }
    }
}
