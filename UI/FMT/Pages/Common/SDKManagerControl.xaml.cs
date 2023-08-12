using FrostySdk.Interfaces;
using SdkGenerator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FMT.Pages.Common
{
    /// <summary>
    /// Interaction logic for SDKManagerControl.xaml
    /// </summary>
    public partial class SDKManagerControl : UserControl, ILogger
    {
        public SDKManagerControl()
        {
            InitializeComponent();

            txtOuputMessage.Text= string.Empty;
            txtOutputSubMessage.Text= string.Empty;


        }

        BuildSDK SDKBuilder;

        private async void btnCreateSDK_Click(object sender, RoutedEventArgs e)
        {
            SDKBuilder = new BuildSDK(this);
            SDKBuilder.TaskChanged += SDKBuilder_TaskChanged;
            await SDKBuilder.Build();
        }

        private void SDKBuilder_TaskChanged(object sender, FrostyEditor.Windows.SdkUpdateTask e)
        {
            Dispatcher.Invoke(() => { 
            
            
            
            
            });

            switch(e.Stage)
            {
                case FrostyEditor.Windows.SdkUpdateTask.SdkUpdateTaskStage.Process:
                    break;
                case FrostyEditor.Windows.SdkUpdateTask.SdkUpdateTaskStage.TypeInfo:
                    break;
                case FrostyEditor.Windows.SdkUpdateTask.SdkUpdateTaskStage.TypeDump:
                    break;
                case FrostyEditor.Windows.SdkUpdateTask.SdkUpdateTaskStage.CrossReference:
                    break;
                case FrostyEditor.Windows.SdkUpdateTask.SdkUpdateTaskStage.CompileSdk:
                    break;
            }
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
    }
}
