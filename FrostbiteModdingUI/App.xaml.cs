using FMT.FileTools;
using FrostbiteSdk;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

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


        protected override void OnStartup(StartupEventArgs e)
        {
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

        //private async void StartApplicationInsights()
        //{
        //    try
        //    {
        //        // Create a TelemetryConfiguration instance.
        //        TelemetryConfiguration config = TelemetryConfiguration.CreateDefault();
        //        config.InstrumentationKey = "92c621ff-61c0-43a2-a2d6-e539b359f053";
        //        //QuickPulseTelemetryProcessor quickPulseProcessor = null;
        //        //config.DefaultTelemetrySink.TelemetryProcessorChainBuilder
        //        //    .Use((next) =>
        //        //    {
        //        //        quickPulseProcessor = new QuickPulseTelemetryProcessor(next);
        //        //        return quickPulseProcessor;
        //        //    })
        //        //    .Build();

        //        //var quickPulseModule = new QuickPulseTelemetryModule();

        //        // Secure the control channel.
        //        // This is optional, but recommended.
        //        //quickPulseModule.AuthenticationApiKey = "YOUR-API-KEY-HERE";
        //        //quickPulseModule.Initialize(config);
        //        //quickPulseModule.RegisterTelemetryProcessor(quickPulseProcessor);

        //        // Create a TelemetryClient instance. It is important
        //        // to use the same TelemetryConfiguration here as the one
        //        // used to setup Live Metrics.
        //        //AppInsightClient = new TelemetryClient(config);
        //    }
        //    catch (Exception)
        //    {

        //    }
        //    await Task.Delay(100);
        //}

        //private async void UnblockAllDLL()
        //{
        //    var loc = AppContext.BaseDirectory;
        //    var psCommmand = $"dir \"{loc}\" -Recurse|Unblock-File";
        //    var psCommandBytes = System.Text.Encoding.Unicode.GetBytes(psCommmand);
        //    var psCommandBase64 = Convert.ToBase64String(psCommandBytes);

        //    var startInfo = new ProcessStartInfo()
        //    {
        //        FileName = "powershell.exe",
        //        Arguments = $"-NoProfile -ExecutionPolicy unrestricted -WindowStyle hidden -EncodedCommand {psCommandBase64}",
        //        UseShellExecute = true
        //    };
        //    startInfo.Verb = "runAs";
        //    Process.Start(startInfo);

        //    await Task.Delay(9000);

        //}

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

            FileLogger.WriteLine($"{e.Message}");

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
            try
            {
                if (string.IsNullOrEmpty(newLanguage))
                    newLanguage = Thread.CurrentThread.CurrentCulture.ToString().Substring(0, 2);

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
                        break;
                    case "es":
                        filename = "Resources\\Languages\\Spanish.xaml";
                        break;
                    default:
                        filename = "Resources\\Languages\\English.xaml";
                        break;
                }

                // add ResourceDictionary
                Application.Current.Resources.MergedDictionaries.Add
                (
                 //new ResourceDictionary { Source = new Uri(String.Concat(_prefix + filename), UriKind.Relative) }
                 new ResourceDictionary { Source = new Uri(String.Concat(filename), UriKind.Relative) }
                );

                // Fix Issue: 34
                // Force English Culture number format of dot for floats / currancy
                var englishCulture = new CultureInfo("en");
                Thread.CurrentThread.CurrentCulture.NumberFormat = englishCulture.NumberFormat;

            }
            catch (Exception)
            {
                //throw ex;
            }
        }
    }
}
